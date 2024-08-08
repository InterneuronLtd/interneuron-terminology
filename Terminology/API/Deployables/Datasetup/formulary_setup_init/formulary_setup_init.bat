@echo off 
:: Parameter Details:
:: --context_param formulary_db_script_path="<Absolute path>/formulary_setup_init/scripts"  
:: --context_param formulary_db_host=<Db Server>  
:: --context_param formulary_db_port=<Db Server Port>  
:: --context_param formulary_db_user=<Db User Id>  
:: --context_param formulary_db_password=<Db User Pwd>  
:: --context_param formulary_db_pwd_string=<Db User Pwd>  
:: --context_param formulary_db_schema=local_formulary 
:: --context_param formulary_db_name=<Db Name>  
:: --context_param formulary_db_additionalparams= 
:: --context_param formulary_db_psql_path=<Path to psql bin - Empty if set as environment variable>

echo "Started creating required Tables, Master data, UDFs and Triggers"
echo "%~dp0"
cd "%~dp0/formulary_db_create/formulary_db_create"
call "formulary_db_create_run.bat" --context_param formulary_db_script_path="C:/Projects/Interneuron/POCs/Apps/interneuron-synapse/Terminology/API/Deployables/Datasetup/formulary_setup_init/scripts" --context_param formulary_db_host=interneuron-ne-db-test.postgres.database.azure.com --context_param formulary_db_port=5432 --context_param formulary_db_user=nedbtestadmin@interneuron-ne-db-test --context_param formulary_db_password=N3ur0n!nedbtest --context_param formulary_db_pwd_string=N3ur0n!nedbtest --context_param formulary_db_schema=local_formulary --context_param formulary_db_name=mmc_demo --context_param formulary_db_additionalparams=  --context_param formulary_db_psql_path= 

if %ERRORLEVEL% GEQ 1 (
echo ERROR
EXIT /B 1
)

echo "Finished creating required Tables, Master data, UDFs and Triggers"
