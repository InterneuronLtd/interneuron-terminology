@echo off
echo Run As Administrator to create service

echo Current Directory %~dp0

set exefilepath="%~dp0"

if [%1] == [] (
	set exefilepath="%~dp0"
	
) else (
	set exefilepath="[%1]"
)

::REFERENCES
::https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/sc-create
:: https://stackoverflow.com/questions/58307558/how-can-i-get-my-net-core-3-single-file-app-to-find-the-appsettings-json-file
:: https://stackoverflow.com/questions/49156169/restsharp-and-ignoring-errors-in-ssl-certificate

ECHO "Stopping Windows service for InterneuronTerminologyBackgroundServiceDMDBrowser if exists"
sc.exe stop "InterneuronTerminologyBackgroundServiceDMDBrowser"
if %ERRORLEVEL% == 0 (timeout /t 10 /nobreak)

ECHO "Deleting Windows service for InterneuronTerminologyBackgroundServiceDMDBrowser if exists"
sc.exe delete "InterneuronTerminologyBackgroundServiceDMDBrowser"
if %ERRORLEVEL% == 0 (timeout /t 10 /nobreak)

ECHO "Creating Windows service for InterneuronTerminologyBackgroundServiceDMDBrowser"
:: contentRoot is where appsettings.json file is
cd %exefilepath%
cd ..
echo Path of exe file "%cd%\Interneuron.Terminology.BackgroundTaskService.exe"
sc.exe create "InterneuronTerminologyBackgroundServiceDMDBrowser" displayname= InterneuronTerminologyBackgroundServiceDMDBrowser type= own error= normal start= auto obj= LocalSystem binpath="%cd%\Interneuron.Terminology.BackgroundTaskService.exe"

sc.exe start InterneuronTerminologyBackgroundServiceDMDBrowser
if %ERRORLEVEL% == 0 (ECHO "Successfully created Windows service for InterneuronTerminologyBackgroundServiceDMDBrowser in 'Auto' mode") else (ECHO "Failed creating Windows service for InterneuronTerminologyBackgroundServiceDMDBrowser in 'Auto' mode")

sc.exe description InterneuronTerminologyBackgroundServiceDMDBrowser "Background service for Terminology Import."
:: pause