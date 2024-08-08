Write-Host "Removing old files from ss1-cpweb-01";
#Remove-Item –path "\\st1-cpweb-01\c$\inetpub\wwwroot\codedeploytest\" -Recurse;
#Get-ChildItem \\st1-cpweb-01\c$\inetpub\wwwroot\TerminologyAPI\ -Include *.* -Recurse | ForEach  { $_.Delete()};
#Get-ChildItem \\st1-cpweb-01\c$\inetpub\wwwroot\TerminologyAPI\ | ForEach   { $_.Delete()};


#Remove-Item –path "\\st1-cpweb-01\c$\inetpub\wwwroot\codedeploytest\" -Recurse;
#Get-ChildItem \\st1-cpweb-02\c$\inetpub\wwwroot\TerminologyAPI\ -Include *.* -Recurse | ForEach  { $_.Delete()};
#Get-ChildItem \\st1-cpweb-02\c$\inetpub\wwwroot\TerminologyAPI\ | ForEach   { $_.Delete()};
