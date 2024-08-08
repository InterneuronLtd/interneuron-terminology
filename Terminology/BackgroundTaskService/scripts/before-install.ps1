IISReset /STOP
ECHO "Stopping Windows service for InterneuronTerminologyBackgroundServiceDMDBrowser if exists"
sc.exe stop "InterneuronTerminologyBackgroundServiceDMDBrowser"
timeout /t 20 /nobreak
ECHO "Deleting Windows service for InterneuronTerminologyBackgroundServiceDMDBrowser if exists"
sc.exe delete "InterneuronTerminologyBackgroundServiceDMDBrowser"
timeout /t 20 /nobreak