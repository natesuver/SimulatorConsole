#Appendix E: The powershell script used to track table read/write usage

Param 
(
    [string] $servername = $(throw "You must supply a database server name to store candidate procedures"),
    [string] $databasename = $(throw "You must supply a database name to store candidate procedures"),
    [string] $stat_tracking_db = $(throw "You must supply a statistics database name"),
    [int] $polling_interval = $(throw "You must supply a polling interval (in seconds)")
) 
Add-PSSnapin SqlServerCmdletSnapin100
Add-PSSnapin SqlServerProviderSnapin100
$sql_template =  @'
use {1}
DECLARE @databaseId INT
SELECT @databaseId = DB_ID('{1}')
DECLARE @CurrentWriteTime DATETIME = DATEADD(SECOND,(-{2}),GETDATE())
-- For each table, find the last Write date that is greater than right now -{2} seconds.
-- If we find a hit, get the total Write count, and then get the total Write count for the prior Writing for that same table which is the max(date) less than the read date.
-- subtract the second number from the first number, and store that as the delta number for that run for that table.

DECLARE @LastWrites TABLE (LastWriteDate DATETIME, TableName NVARCHAR(50), TotalWriteCount INT)
DECLARE @PriorWrites TABLE (LastWriteDate DATETIME, TableName NVARCHAR(50), TotalWriteCount INT)

--search for all table Writes that have occurred in the last {2} seconds.
INSERT INTO @LastWrites
(
    LastWriteDate,
    TableName,
    TotalWriteCount
)
SELECT 
MAX(s.last_user_update),
object_name(s.object_id),
SUM(ISNULL(user_updates,0))
FROM {1}.sys.dm_db_index_usage_stats AS s
INNER JOIN {1}.sys.indexes AS i ON 
    s.object_id = i.object_id AND
    i.index_id = s.index_id
INNER JOIN (Select DISTINCT table_name from {0}.dbo.dependent_tables) AS dt ON 
	dt.table_name = object_name(s.object_id)
WHERE 
s.database_id = @databaseId
AND ISNULL(user_updates,0) > 0
AND s.last_user_update >= @CurrentWriteTime
GROUP BY object_name(s.object_id)


INSERT INTO {0}.dbo.table_usage_stats
(
    TableName,
    LastActionTime,
    TotalRowsAffected,
    DeltaRowsAffected
)
Select
TableName,
LastWriteDate,
TotalWriteCount,
0
from @LastWrites
where TotalRowsAffected > 0
'@ -f $stat_tracking_db, $databasename, $polling_interval

while(1)
{
    $time_start = Get-Date -format "HHmmssffff"
    Invoke-Sqlcmd -ServerInstance $servername -Database $stat_tracking_db -Query $sql_template 
    $time_end = Get-Date -format "HHmmssffff"
    $duration = [int]$time_end - [int]$time_start
    $now = Get-Date
    Write-Output "Invoked table usage metrics for $servername -> $databasename -> duration: $duration ms -> at: $now, sleeping for $polling_interval seconds"
    start-sleep -seconds $polling_interval
}
Write-Output "Processing track-table-usage is complete!"

#Sample Usage
#. ./track-table-usage.ps1 -servername "<your-server-name>" -databasename "<your-database-name>" -stat_tracking_db "<your-statistics-database>" -polling_interval 10