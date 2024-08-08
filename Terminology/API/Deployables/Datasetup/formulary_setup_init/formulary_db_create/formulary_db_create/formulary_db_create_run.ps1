$fileDir = Split-Path -Parent $MyInvocation.MyCommand.Path
cd $fileDir
java '-Dtalend.component.manager.m2.repository=%cd%/../lib' '-Xms256M' '-Xmx1024M' -cp '.;../lib/routines.jar;../lib/log4j-slf4j-impl-2.12.1.jar;../lib/log4j-api-2.12.1.jar;../lib/log4j-core-2.12.1.jar;../lib/talendcsv.jar;../lib/crypto-utils.jar;../lib/slf4j-api-1.7.25.jar;../lib/dom4j-2.1.1.jar;formulary_db_create_0_1.jar;' local_project.formulary_db_create_0_1.formulary_db_create  --context=Default $args