Echo Off
SET SPLANGEXT=cs

Echo Backing up previous version of generated code ... 
IF NOT EXIST .\PreviousVersionGeneratedCode MkDir .\PreviousVersionGeneratedCode
IF EXIST Status.%SPLANGEXT% xcopy /Y/V Status.%SPLANGEXT% .\PreviousVersionGeneratedCode

Echo Generating code ...
SPMetal /web:http://sps-dev-01/status /code:Status.%SPLANGEXT% /user:dolbynet\dmttest-rs1 /password:dolby1234!