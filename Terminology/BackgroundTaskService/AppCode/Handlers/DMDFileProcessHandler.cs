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
﻿using Interneuron.Common.Extensions;
using Interneuron.Terminology.BackgroundTaskService.AppCode.Extensions;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Interneuron.Terminology.BackgroundTaskService.AppCode.Handlers
{
    public class DMDFileProcessHandler
    {
        private IConfiguration _configuration;

        public DMDFileProcessHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (bool isSuccess, string? processText, string? processError) ProcessDMDFile(string uploadDir = null, string dmdFileName = null, string dmdBonusFileName = null, string bnfFileName = null, string bnfLkpFileName = null, string syncMode = "auto")
        {
            if (uploadDir == null || (dmdFileName.IsEmpty() && dmdBonusFileName.IsEmpty() && bnfFileName.IsEmpty()))
                return (false, "", "Invalid parameters");

            var dmdVersion = GetDMDVersion(dmdFileName);

            if (dmdFileName.IsNotEmpty())
                UnzipFile(uploadDir, dmdFileName);

            if (dmdBonusFileName.IsNotEmpty())
                UnzipFile(uploadDir, dmdBonusFileName);

            if (bnfFileName.IsNotEmpty())
                UnzipFile(uploadDir, bnfFileName);

            if (bnfLkpFileName.IsNotEmpty())
                UnzipFile(uploadDir, bnfLkpFileName);


            var dmdFilePath = dmdFileName.IsEmpty() ? "" : $@"{Path.Combine(uploadDir, Path.GetFileNameWithoutExtension(dmdFileName))}";
            var dmdBonusFilePath = dmdBonusFileName.IsEmpty() ? "" : $@"{Path.Combine(uploadDir, Path.GetFileNameWithoutExtension(dmdBonusFileName))}";
            var bnfFilePath = bnfFileName.IsEmpty() ? "" : $@"{Path.Combine(uploadDir, Path.GetFileNameWithoutExtension(bnfFileName))}";
            var bnfLkpFilePath = bnfLkpFileName.IsEmpty() ? "" : $@"{Path.Combine(uploadDir, Path.GetFileNameWithoutExtension(bnfLkpFileName))}";

            dmdFilePath = dmdFilePath.Replace(@"\", "/");
            dmdBonusFilePath = dmdBonusFilePath.Replace(@"\", "/");
            bnfFilePath = bnfFilePath.Replace(@"\", "/");
            bnfLkpFilePath = bnfLkpFilePath.Replace(@"\", "/");

            var dmdDb = _configuration["MMCSyncDMDDBConfig:dmdDb"];
            var dmdServer = _configuration["MMCSyncDMDDBConfig:dmdServer"];
            var dmdPort = _configuration["MMCSyncDMDDBConfig:dmdPort"]; ;
            var dmdSchema = _configuration["MMCSyncDMDDBConfig:dmdSchema"];
            var dmdUId = _configuration["MMCSyncDMDDBConfig:dmdUId"];
            var dmdPassword = _configuration["MMCSyncDMDDBConfig:dmdPassword"];
            var dmdStgDb = _configuration["MMCSyncDMDDBConfig:dmdStgDb"];
            var dmdStgServer = _configuration["MMCSyncDMDDBConfig:dmdStgServer"];
            var dmdStgPort = _configuration["MMCSyncDMDDBConfig:dmdStgPort"];
            var dmdStgSchema = _configuration["MMCSyncDMDDBConfig:dmdStgSchema"];
            var dmdStgUId = _configuration["MMCSyncDMDDBConfig:dmdStgUId"];
            var dmdStgPassword = _configuration["MMCSyncDMDDBConfig:dmdStgPassword"];

            var (hasValidAccessiblePath, batchFileDir) = GetETLDMDDeltaProcessorFile();// Path.Combine(Directory.GetCurrentDirectory(), @"ETLJobs\DMDDeltaProcessor\dmd_delta_processor\dmd_delta_processor", "dmd_delta_processor_run.bat");

            if(!hasValidAccessiblePath)
                return (false, String.Empty, "Unable to find the path for DMD File Processing Talend Jobs");

            var batArgs = $"--context_param dmd_version=\"{dmdVersion}\" --context_param dmd_db_additionalparams=  --context_param dmd_db_host=\"{dmdServer}\" --context_param dmd_db_name=\"{dmdDb}\" --context_param dmd_db_password=\"{dmdPassword}\" --context_param dmd_db_port={dmdPort} --context_param dmd_db_psql_path=  --context_param dmd_db_pwd_string=\"{dmdPassword}\" --context_param dmd_db_schema=\"{dmdSchema}\" --context_param dmd_db_script_path= --context_param dmd_db_user=\"{dmdUId}\" --context_param dmd_file_path=\"{dmdFilePath}\" --context_param dmd_db_stg_additionalparams=  --context_param dmd_db_stg_host=\"{dmdStgServer}\" --context_param dmd_db_stg_name=\"{dmdStgDb}\" --context_param dmd_db_stg_password=\"{dmdStgPassword}\" --context_param dmd_db_stg_port={dmdStgPort} --context_param dmd_db_stg_pwd_string=\"{dmdStgPassword}\" --context_param dmd_db_stg_schema=\"{dmdStgSchema}\" --context_param dmd_db_script_path= --context_param dmd_db_stg_user=\"{dmdStgUId}\" --context_param dmd_bnf_file_path=\"{bnfFilePath}\" --context_param dmd_bonus_file_path=\"{dmdBonusFilePath}\" --context_param dmd_bnf_lkp_file_path=\"{bnfLkpFilePath}\"";


            //No need to execute the batch file
            //batchFileDir = Path.Combine(Directory.GetCurrentDirectory(), @"ETLJobs\DMDDeltaProcessor\dmd_delta_process.bat");
            //var command = $"dmd_delta_processor_run.bat {batArgs}";

            var psi = new ProcessStartInfo(batchFileDir)
            //var psi = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                //WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), @"ETLJobs\DMDDeltaProcessor\dmd_delta_processor\dmd_delta_processor"),
                Arguments = batArgs,//dmdFilePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = false,
                //CreateNoWindow = true
            };

            var batchProcess = new Process();

            var consoleOutput = new StringBuilder();
            var errorOutput = new StringBuilder();

            ////Debuggging only or can be for realtime later
            batchProcess.OutputDataReceived += (s, d) =>
            {
                consoleOutput.AppendLine(d?.Data);
                Console.WriteLine(d?.Data);
            };

            batchProcess.ErrorDataReceived += (s, d) =>
            {
                errorOutput.AppendLine(d?.Data);
                Console.WriteLine(d?.Data);
            };

            batchProcess.StartInfo = psi;
            batchProcess.Start();

            batchProcess.BeginOutputReadLine();
            batchProcess.BeginErrorReadLine();

            var isSuccess = false;

            batchProcess.WaitForExit();

            if (batchProcess.HasExited)
            {
                var exitCode = batchProcess.ExitCode;

                isSuccess = (exitCode == 0);//0 success

                //if (string.Compare(syncMode, "auto", true) == 0 && exitCode == 0)
                //{
                //    status = await ImportDeltasToFormulary();
                //}
            }

            batchProcess.Close();

            return (isSuccess, consoleOutput.ToString(), errorOutput.ToString());
        }

        //private async Task<int> ImportDeltasToFormulary()
        //{
        //    string token = HttpContext.Session.GetString("access_token");

        //    var response = await TerminologyAPIService.ImportDeltas(token);

        //    if (response == null || response.StatusCode == DataService.APIModel.StatusCode.Fail)
        //    {
        //        _toastNotification.AddErrorToastMessage("Error Importing DMD data to MMC System.");
        //        return 0;
        //    }

        //    return 1;
        //}

        private (bool hasValidUploadableFolder, string folderPath) GetETLDMDDeltaProcessorFile()
        {
            var filePathsAsStr = _configuration["TerminologyBackgroundTaskConfig:ETLJobsPaths"];
            if (filePathsAsStr.IsEmpty()) return (false, null);
            var uploadableDirs = filePathsAsStr.Split("|");
            foreach (var dir in uploadableDirs)
            {
                try
                {
                    var deltaProcessorPath = _configuration["TerminologyBackgroundTaskConfig:DMDDeltaProcessorFilePathWithinETLJobsPath"] ?? "DMDDeltaProcessor/dmd_delta_processor/dmd_delta_processor";

                    deltaProcessorPath = deltaProcessorPath.StartsWith("/") ? deltaProcessorPath.TrimStart('/') : deltaProcessorPath;
                    deltaProcessorPath = deltaProcessorPath.EndsWith("/") ? deltaProcessorPath.TrimEnd('/') : deltaProcessorPath;

                    var deltaProcessorDir = $"{dir}/{deltaProcessorPath}";
                    File.Create(Path.Combine(deltaProcessorDir, $"etljobs_temp_{new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds()}.txt")).Close();
                    //System.IO.File.Delete(tempfilepath + "temp.txt");
                    return (true, Path.Combine(deltaProcessorDir, "dmd_delta_processor_run.bat"));
                }
                catch { }
            }

            return (false, null);
        }

        private void UnzipFile(string uploadDir, string fileName)
        {
            if (!CommonUtil.DoesFileExists(uploadDir, fileName)) return;
            if (uploadDir.IsEmpty() || fileName.IsEmpty()) return;

            var pathWithFileName = Path.Combine(uploadDir, fileName);
            uploadDir = Path.Combine(uploadDir, Path.GetFileNameWithoutExtension(fileName));
            ZipFile.ExtractToDirectory(pathWithFileName, uploadDir, true);
        }

        private string GetDMDVersion(string fileName)
        {
            var allVers = Regex.Split(fileName, @"[^0-9\.]+");

            if (allVers.IsCollectionValid())
            {
                var versionNo = allVers.FirstOrDefault(rec => rec.IsNotEmpty());
                return versionNo;
            }

            return "";
        }
    }
}