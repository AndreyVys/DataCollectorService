set current_dir=%~dp0
sqlcmd -S localhost\SQLExpress -U sa -P sa -i %current_dir%CreateDB.sql
sc create DataCollectorService binPath= "%current_dir%DataCollectorService.exe" start= "auto" displayname="Служба сохранения данных брикетной линии"
sc start DataCollectorService
pause