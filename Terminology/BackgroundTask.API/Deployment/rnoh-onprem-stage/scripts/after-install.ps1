Write-Host "Copying Files to ss1-cpweb-01";
#Copy-Item -Path C:\inetpub\buildfile\* -Destination \\st1-cpweb-01\c$\inetpub\wwwroot\TerminologyAPI\ -Recurse;


#Copy-Item -Path C:\inetpub\buildfile\* -Destination \\st1-cpweb-02\c$\inetpub\wwwroot\TerminologyAPI\ -Recurse;


#Write-Host "Removing files from buildfiles";
#Get-ChildItem C:\inetpub\buildfile\ -Include *.* -Recurse | ForEach  { $_.Delete()};
#Get-ChildItem C:\inetpub\buildfile\ | ForEach   { $_.Delete()};