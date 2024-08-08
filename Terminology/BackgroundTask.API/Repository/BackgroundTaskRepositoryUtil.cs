 //Interneuron synapse

//Copyright(C) 2024 Interneuron Limited

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

//See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
﻿using Dapper;
using Interneuron.Common.Extensions;
using Interneuron.Infrastructure.CustomExceptions;
using NodaTime;
using Npgsql;
using NpgsqlTypes;

namespace Interneuron.Terminology.BackgroundTask.API.Repository
{
    public class BackgroundTaskRepositoryUtil
    {
        private IConfiguration _configuration;
        private ILogger<BackgroundTaskRepositoryUtil> _logger;

        public BackgroundTaskRepositoryUtil(IConfiguration configuration, ILogger<BackgroundTaskRepositoryUtil> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> CreateBackgroundTask(Models.BackgroundTask task)
        {
            if (task == null) return false;

            var connString = _configuration["TerminologyBackgroundTaskConfig:Connectionstring"];

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            var stmt = @"INSERT INTO terminology.background_task
(task_id, name, detail, status_code, status, _createdby, _createddate, _createdtimestamp,_updatedby, _updateddate, _updatedtimestamp, correlation_task_id)
values(@task_id, @name, @detail, @status_code, @status, @createdby, @createddate, @createdtimestamp, @updatedby, @updateddate, @updatedtimestamp, @correlation_task_id);";

            //INSERT INTO notification.messagelog
            //            (message_log_id, root_message_id, name, message_type, message, sender, delivery_status, notifier_message, _createdby, _createddate, _createdtimestamp, _updatedby, _updateddate, _updatedtimestamp)
            //            ";

            await using (var cmd = new NpgsqlCommand(stmt, conn))
            {
                cmd.Parameters.AddWithValue("task_id", task.TaskId);
                cmd.Parameters.AddWithValue("name", task.Name);
                cmd.Parameters.AddWithValue("detail", task.Detail);
                cmd.Parameters.AddWithValue("status_code", task.StatusCd);
                cmd.Parameters.AddWithValue("status", task.Status.IsEmpty() ? DBNull.Value : task.Status);
                cmd.Parameters.AddWithValue("createdby", task.Createdby.IsEmpty() ? DBNull.Value : task.Createdby);
                cmd.Parameters.AddWithValue("createddate", DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local));
                cmd.Parameters.AddWithValue("createdtimestamp", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updatedby", task.Updatedby.IsEmpty() ? DBNull.Value : task.Updatedby);
                cmd.Parameters.AddWithValue("updateddate", DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local));
                cmd.Parameters.AddWithValue("updatedtimestamp", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("correlation_task_id", task.CorrelationTaskId.IsEmpty() ? DBNull.Value : task.CorrelationTaskId);

                var insertedRows = await cmd.ExecuteNonQueryAsync();
                return insertedRows > 0;
            }
        }

        public async Task<List<Models.BackgroundTask>?> GetTasksByNameAndStatus(string taskName, short? statusCode)
        {
            var connString = _configuration["TerminologyBackgroundTaskConfig:Connectionstring"];

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            string commandText = $"SELECT task_id as taskId, correlation_task_id as correlationtaskid, name, detail, status_code as statuscd, status, _createdby as createdby, _createddate as createddate, _createdtimestamp as createdtimestamp,_updatedby as updatedby, _updateddate as updateddate, _updatedtimestamp as updatedtimestamp FROM terminology.background_task WHERE name = @Name";

            var queryArgs = new DynamicParameters();
            queryArgs.AddDynamicParams(new { Name = taskName });

            if (statusCode.HasValue)
            {
                commandText = $"{commandText} and status_code = @StatusCd";
                queryArgs.AddDynamicParams(new { StatusCd = statusCode.Value });
            }

            commandText = $"{commandText} order by serial_num desc;";

            var tasks = await conn.QueryAsync<Models.BackgroundTask>(commandText, queryArgs);
            return tasks?.ToList();
        }

        internal async Task<List<Models.BackgroundTask>> GetTaskByNamesWithStatusAndUpdateStatus(List<string> taskNames, short? statusCode, short statusCdToUpdate, string statusToUpdate, string? detailToUpdate)
        {
            var connString = _configuration["TerminologyBackgroundTaskConfig:Connectionstring"];

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            using (var tran = await conn.BeginTransactionAsync())
            {
                try
                {
                    string commandText = $"SELECT task_id as taskId, correlation_task_id as correlationtaskid, name, detail, status_code as statuscd, status, _createdby as createdby, _createddate as createddate, _createdtimestamp as createdtimestamp,_updatedby as updatedby, _updateddate as updateddate, _updatedtimestamp as updatedtimestamp FROM terminology.background_task WHERE name = Any(@Names)";

                    var queryArgs = new DynamicParameters();
                    queryArgs.AddDynamicParams(new { Names = taskNames });

                    if (statusCode.HasValue)
                    {
                        commandText = $"{commandText} and status_code = @StatusCd";
                        queryArgs.AddDynamicParams(new { StatusCd = statusCode.Value });
                    }
                    commandText = $"{commandText} order by serial_num desc;";

                    var tasks = await conn.QueryAsync<Models.BackgroundTask>(commandText, queryArgs);
                    var tasksAsList = tasks.ToList();

                    if (tasksAsList.IsCollectionValid())
                    {
                        await UpdateTaskStatusWithDetailInSameTran(tasksAsList, statusCdToUpdate, statusToUpdate, detailToUpdate, conn);
                    }

                    await tran.CommitAsync();
                    return tasksAsList;
                }
                catch (Exception ex)
                {
                    await tran.RollbackAsync();
                    _logger.LogError(ex, string.Join(", ", taskNames));
                    throw;
                }
            }
                
        }

        private async Task UpdateTaskStatusWithDetailInSameTran(List<Models.BackgroundTask> tasks,  short statusCdToUpdate, string statusToUpdate, string? detailToUpdate, NpgsqlConnection conn)
        {
            var detail = "";

            if (detailToUpdate.IsNotEmpty())
                detail = "detail = @detail,";

            foreach (var task in tasks)
            {
                var stmt = @"UPDATE terminology.background_task 
                                SET status = @status,
                                status_code = @statusCd," +
                                detail +
                                @"_updatedby = @updatedby,
                                _updateddate = @updateddate,
                                _updatedtimestamp = @updatedtimestamp
                                WHERE task_id = @id;";

                await using (var cmd = new NpgsqlCommand(stmt, conn))
                {
                    cmd.Parameters.AddWithValue("id", task.TaskId);
                    cmd.Parameters.AddWithValue("status", statusToUpdate.IsEmpty() ? DBNull.Value : statusToUpdate);
                    cmd.Parameters.AddWithValue("statusCd", statusCdToUpdate);
                    if (detailToUpdate.IsNotEmpty())
                        cmd.Parameters.AddWithValue("detail", detailToUpdate);
                    cmd.Parameters.AddWithValue("updatedby", task.Updatedby.IsEmpty() ? DBNull.Value : task.Updatedby);
                    cmd.Parameters.AddWithValue("updateddate", DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local));
                    cmd.Parameters.AddWithValue("updatedtimestamp", DateTime.UtcNow);

                    var updatedRows = await cmd.ExecuteNonQueryAsync();
                    //return updatedRows > 0;
                }
            }
        }

        public async Task<List<Models.BackgroundTask>?> GetTaskByNamesAndStatus(List<string> taskNames, short? statusCode)
        {
            var connString = _configuration["TerminologyBackgroundTaskConfig:Connectionstring"];

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            string commandText = $"SELECT task_id as taskId, correlation_task_id as correlationtaskid, name, detail, status_code as statuscd, status, _createdby as createdby, _createddate as createddate, _createdtimestamp as createdtimestamp,_updatedby as updatedby, _updateddate as updateddate, _updatedtimestamp as updatedtimestamp  FROM terminology.background_task WHERE name = Any(@Names)";

            var queryArgs = new DynamicParameters();
            queryArgs.AddDynamicParams(new { Names = taskNames });

            if (statusCode.HasValue)
            {
                commandText = $"{commandText} and status_code = @StatusCd";
                queryArgs.AddDynamicParams(new { StatusCd = statusCode.Value });
            }
            commandText = $"{commandText} order by serial_num desc;";

            var tasks = await conn.QueryAsync<Models.BackgroundTask>(commandText, queryArgs);
            return tasks?.ToList();
        }

        public async Task<Models.BackgroundTask?> GetTaskByTaskId(string taskId)
        {
            var connString = _configuration["TerminologyBackgroundTaskConfig:Connectionstring"];

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            string commandText = $"SELECT task_id as taskId, correlation_task_id as correlationtaskid, name, detail, status_code as statuscd, status, _createdby as createdby, _createddate as createddate, _createdtimestamp as createdtimestamp,_updatedby as updatedby, _updateddate as updateddate, _updatedtimestamp as updatedtimestamp  FROM terminology.background_task WHERE task_id = @TaskId";

            var queryArgs = new DynamicParameters();
            queryArgs.AddDynamicParams(new { TaskId = taskId });

            commandText = $"{commandText} order by serial_num desc;";

            var task = await conn.QueryFirstOrDefaultAsync<Models.BackgroundTask>(commandText, queryArgs);
            return task;
        }

        public async Task<List<Models.BackgroundTask>?> GetTasksByCorrelationTaskId(string correlattionTaskId)
        {
            var connString = _configuration["TerminologyBackgroundTaskConfig:Connectionstring"];

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            string commandText = $"SELECT task_id as taskId, correlation_task_id as correlationtaskid, name, detail, status_code as statuscd, status, _createdby as createdby, _createddate as createddate, _createdtimestamp as createdtimestamp,_updatedby as updatedby, _updateddate as updateddate, _updatedtimestamp as updatedtimestamp  FROM terminology.background_task WHERE correlation_task_id = @CorrelattionTaskId";

            var queryArgs = new DynamicParameters();
            queryArgs.AddDynamicParams(new { CorrelattionTaskId = correlattionTaskId });

            commandText = $"{commandText} order by serial_num desc;";

            var tasks = await conn.QueryAsync<Models.BackgroundTask>(commandText, queryArgs);
            return tasks?.ToList();
        }

        public async Task<bool> UpdateTask(Models.BackgroundTask task)
        {
            var connString = _configuration["TerminologyBackgroundTaskConfig:Connectionstring"];

            await using var conn = new NpgsqlConnection(connString);

            await conn.OpenAsync();

            var stmt = @"UPDATE terminology.background_task 
                        SET status = @status,
                        status_code = @statusCd,
                        detail = @detail,
                        name = @name,
                        _updatedby = @updatedby,
                        _updateddate = @updateddate,
                        _updatedtimestamp = @updatedtimestamp
                WHERE task_id = @id;";
            await using (var cmd = new NpgsqlCommand(stmt, conn))
            {
                cmd.Parameters.AddWithValue("id", task.TaskId);
                cmd.Parameters.AddWithValue("status", task.Status.IsEmpty() ? DBNull.Value : task.Status);
                cmd.Parameters.AddWithValue("statusCd", task.StatusCd);
                cmd.Parameters.AddWithValue("detail", task.Detail.IsEmpty() ? DBNull.Value : task.Detail);
                cmd.Parameters.AddWithValue("name", task.Name.IsEmpty() ? DBNull.Value : task.Name);
                cmd.Parameters.AddWithValue("updatedby", task.Updatedby.IsEmpty() ? DBNull.Value : task.Updatedby);
                cmd.Parameters.AddWithValue("updateddate", DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local));
                cmd.Parameters.AddWithValue("updatedtimestamp", DateTime.UtcNow);

                var updatedRows = await cmd.ExecuteNonQueryAsync();
                return updatedRows > 0;
            }
        }

        public async Task<bool> UpdateTaskStatusWithDetail(Models.BackgroundTask task)
        {
            var connString = _configuration["TerminologyBackgroundTaskConfig:Connectionstring"];

            await using var conn = new NpgsqlConnection(connString);

            await conn.OpenAsync();

            var stmt = @"UPDATE terminology.background_task 
                        SET status = @status,
                        status_code = @statusCd,
                        detail = @detail,
                        _updatedby = @updatedby,
                        _updateddate = @updateddate,
                        _updatedtimestamp = @updatedtimestamp
                WHERE task_id = @id;";
            await using (var cmd = new NpgsqlCommand(stmt, conn))
            {
                cmd.Parameters.AddWithValue("id", task.TaskId);
                cmd.Parameters.AddWithValue("status", task.Status.IsEmpty() ? DBNull.Value : task.Status);
                cmd.Parameters.AddWithValue("statusCd", task.StatusCd);
                cmd.Parameters.AddWithValue("detail", task.Detail.IsEmpty() ? DBNull.Value : task.Detail);
                cmd.Parameters.AddWithValue("updatedby", task.Updatedby.IsEmpty() ? DBNull.Value : task.Updatedby);
                cmd.Parameters.AddWithValue("updateddate", DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local));
                cmd.Parameters.AddWithValue("updatedtimestamp", DateTime.UtcNow);

                var updatedRows = await cmd.ExecuteNonQueryAsync();
                return updatedRows > 0;
            }
        }

        private static NpgsqlParameter safeNpgsqlParameter<T>(string parameterName, T? data) where T : struct =>
    data switch
    {
        decimal => new NpgsqlParameter() { ParameterName = parameterName, NpgsqlDbType = NpgsqlDbType.Numeric, Value = data },
        Enum => new NpgsqlParameter() { ParameterName = parameterName, Value = data },
        null => new NpgsqlParameter() { ParameterName = parameterName, Value = DBNull.Value },
        LocalDate => new NpgsqlParameter()
        {
            ParameterName = parameterName,
            NpgsqlDbType = NpgsqlDbType.Date,
            Value = data
        },
        _ => throw new InterneuronDBException()
    };

        //public async Task<NotificationMessagelog> GetMessagelogByMessageLogId(string messageLogId)
        //{
        //    //var connString = _configuration["WebNotifierConfig:Connectionstring"];

        //    await using var conn = new NpgsqlConnection(connString);
        //    await conn.OpenAsync();

        //    string commandText = $"SELECT message_log_id as messagelogid,name,message_type as messagetype,message,sender,delivery_status as deliverystatus,notifier_message as notifiermessage FROM notification.messagelog WHERE message_log_id = @id";

        //    var queryArgs = new { Id = messageLogId };
        //    var messagelog = await conn.QueryFirstOrDefaultAsync<NotificationMessagelog>(commandText, queryArgs);
        //    return messagelog;
        //}

        //public async Task<NotificationMessagelog> GetMessagelogByMessageCorrelationIdAndMsgType(string messageLogId, string messageType)
        //{
        //    //var connString = _configuration["WebNotifierConfig:Connectionstring"];

        //    await using var conn = new NpgsqlConnection(connString);
        //    await conn.OpenAsync();

        //    string commandText = $"SELECT message_log_id as messagelogid,name,message_type as messagetype,message,sender,delivery_status as deliverystatus,notifier_message as notifiermessage FROM notification.messagelog WHERE message_log_id = @Id and message_type = @MessageType";

        //    var queryArgs = new { Id = messageLogId, MessageType = messageType };
        //    var messagelog = await conn.QueryFirstOrDefaultAsync<NotificationMessagelog>(commandText, queryArgs);
        //    return messagelog;
        //}

        //public async Task<bool> UpdateMessagelogStatus(NotificationMessagelogEntry messageLog)
        //{
        //    //var connString = _configuration["WebNotifierConfig:Connectionstring"];// "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";

        //    await using var conn = new NpgsqlConnection(_connectionString);
        //    await conn.OpenAsync();

        //    var stmt = @"UPDATE notification.messagelog 
        //                SET delivery_status = @status,
        //                message = @message,
        //                notifier_message = @notifier_message,
        //                _updatedby = @updatedby,
        //                _updateddate = @updateddate,
        //                _updatedtimestamp = @updatedtimestamp
        //        WHERE message_log_id = @id;";
        //    await using (var cmd = new NpgsqlCommand(stmt, conn))
        //    {
        //        cmd.Parameters.AddWithValue("status", messageLog.DeliveryStatus);
        //        cmd.Parameters.AddWithValue("id", messageLog.MessageLogId);
        //        cmd.Parameters.AddWithValue("notifier_message", messageLog.NotifierMessage.IsEmpty() ? DBNull.Value : messageLog.NotifierMessage);
        //        cmd.Parameters.AddWithValue("message", messageLog.Message.IsEmpty() ? DBNull.Value : messageLog.Message);
        //        cmd.Parameters.AddWithValue("updatedby", messageLog.Updatedby.IsEmpty() ? DBNull.Value: messageLog.Updatedby);
        //        cmd.Parameters.AddWithValue("updateddate", DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local));
        //        cmd.Parameters.AddWithValue("updatedtimestamp", DateTime.UtcNow);

        //        var updatedRows = await cmd.ExecuteNonQueryAsync();
        //        return updatedRows > 0;
        //    }
        //}

    }
}