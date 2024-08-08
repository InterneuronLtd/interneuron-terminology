$fileDir = Split-Path -Parent $MyInvocation.MyCommand.Path
cd $fileDir
java '-Dtalend.component.manager.m2.repository=%cd%/../lib' '-Xms256M' '-Xmx4096M' -cp '.;../lib/routines.jar;../lib/log4j-slf4j-impl-2.12.1.jar;../lib/log4j-api-2.12.1.jar;../lib/log4j-core-2.12.1.jar;../lib/jakarta-oro-2.0.8.jar;../lib/postgresql-42.2.9.jar;../lib/talendcsv.jar;../lib/crypto-utils.jar;../lib/talend_file_enhanced_20070724.jar;../lib/slf4j-api-1.7.25.jar;../lib/dom4j-2.1.1.jar;snomed_import_0_1.jar;' local_project.snomed_import_0_1.snomed_import  --context=Default $args