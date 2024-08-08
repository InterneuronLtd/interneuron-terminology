@echo off 
:: Parameter Details:
:: --context_param snomed_int_file_path="<Path to downloaded SNOMED CT files Int version>/Full" 
:: E.g. C:/Projects/Interneuron/TalendPlayground/uk_sct2cl_30.0.0_20200805000001/SnomedCT_InternationalRF2_PRODUCTION_20190731T120000Z/Full

:: --context_param snomed_db_script_path="<Absolute path>/snomedct_setup_init/scripts" 
:: --context_param snomed_db_host=<DB Server> 
:: --context_param snomed_db_port=<Db port> 
:: --context_param snomed_db_user=<Db User Id> 
:: --context_param snomed_db_password=<Db User Password> 
:: --context_param snomed_db_pwd_string=<Db User Password>  
:: --context_param snomed_db_schema=terminology 
:: --context_param snomed_db_name=<Db Name> 
:: --context_param snomed_db_additionalparams= 
:: --context_param snomed_clinical_ext_file_path=<E.g. sct2_Concept_Full_INT_<"This Content">.txt> 
:: --context_param snomed_drug_ext_file_path uk drug extension file path

:: E.g.
:: C:/Projects/Interneuron/TalendPlayground/uk_sct2cl_30.0.0_20200805000001/SnomedCT_UKClinicalRF2_PRODUCTION_20200805T000001Z/Full

:: --context_param snomed_db_psql_path=<Path to psql bin - Empty if set as environment variable>


echo "Started creating required tables"
echo "%~dp0"
cd "%~dp0/snomed_db_create/snomed_db_create"
call "snomed_db_create_run.bat" --context_param snomed_clinical_ext_file_path="C:/MMC_Deployment/Deployment_Demo_07122020/SnomedFiles" --context_param snomed_drug_ext_file_path="C:/MMC_Deployment/Deployment_Demo_07122020/SnomedFiles" --context_param snomed_db_script_path="C:/Projects/Interneuron/POCs/Apps/interneuron-synapse/Terminology/API/Deployables/Datasetup/snomedct_setup_init/scripts" --context_param snomed_db_host=interneuron-ne-db-test.postgres.database.azure.com --context_param snomed_db_port=5432 --context_param snomed_db_user=nedbtestadmin@interneuron-ne-db-test --context_param snomed_db_password=N3ur0n!nedbtest --context_param snomed_db_pwd_string=N3ur0n!nedbtest --context_param snomed_db_schema=terminology --context_param snomed_db_name=mmc_demo --context_param snomed_db_additionalparams= --context_param snomed_db_psql_path=

echo "Finished creating required tables with data"


echo "Started importing data from files"
echo "%~dp0"
cd "%~dp0/snomed_import/snomed_import"
call "snomed_import_run.bat" --context_param snomed_clinical_ext_file_path="C:/MMC_Deployment/Deployment_Demo_07122020/SnomedFiles" --context_param snomed_drug_ext_file_path="C:/MMC_Deployment/Deployment_Demo_07122020/SnomedFiles" --context_param snomed_db_script_path="C:/Projects/Interneuron/POCs/Apps/interneuron-synapse/Terminology/API/Deployables/Datasetup/snomedct_setup_init/scripts" --context_param snomed_db_host=interneuron-ne-db-test.postgres.database.azure.com --context_param snomed_db_port=5432 --context_param snomed_db_user=nedbtestadmin@interneuron-ne-db-test --context_param snomed_db_password=N3ur0n!nedbtest --context_param snomed_db_pwd_string=N3ur0n!nedbtest --context_param snomed_db_schema=terminology --context_param snomed_db_name=mmc_demo --context_param snomed_db_additionalparams= --context_param snomed_db_psql_path=

echo "Finished importing data from files"

echo "Started creating materialized views and UDFs"
echo "%~dp0"
cd "%~dp0/snomed_db_views_udfs/snomed_db_views_udfs"
call "snomed_db_views_udfs_run.bat" --context_param snomed_clinical_ext_file_path="C:/MMC_Deployment/Deployment_Demo_07122020/SnomedFiles" --context_param snomed_drug_ext_file_path="C:/MMC_Deployment/Deployment_Demo_07122020/SnomedFiles" --context_param snomed_db_script_path="C:/Projects/Interneuron/POCs/Apps/interneuron-synapse/Terminology/API/Deployables/Datasetup/snomedct_setup_init/scripts" --context_param snomed_db_host=interneuron-ne-db-test.postgres.database.azure.com --context_param snomed_db_port=5432 --context_param snomed_db_user=nedbtestadmin@interneuron-ne-db-test --context_param snomed_db_password=N3ur0n!nedbtest --context_param snomed_db_pwd_string=N3ur0n!nedbtest --context_param snomed_db_schema=terminology --context_param snomed_db_name=mmc_demo --context_param snomed_db_additionalparams= --context_param snomed_db_psql_path=

if %ERRORLEVEL% GEQ 1 (
echo ERROR
EXIT /B 1
)

echo "Finished creating materialized views and UDFs"

