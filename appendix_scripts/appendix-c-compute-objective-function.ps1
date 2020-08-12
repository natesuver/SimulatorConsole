#Appendix C: Powershell script used to compute system impact

Param 
(
	[string] $servername = $(throw "You must supply a database server name"),
	[string] $databasename = $(throw "You must supply a database name"),
	[string] $stat_tracking_db = $(throw "You must supply a statistics database name"),
	[decimal] $Cpu_Weight = 1,
	[decimal] $Read_Weight = 1,
	[decimal] $WaitTime_Weight = 1
) 

Add-PSSnapin SqlServerCmdletSnapin100
Add-PSSnapin SqlServerProviderSnapin100

$template = @' 
DECLARE @cpu_weight DECIMAL={2}, @read_weight DECIMAL={3}, @wait_time_weight DECIMAL={4}
DECLARE @Raw TABLE (schema_name NVARCHAR(20), procedure_name NVARCHAR(500), total_reads DECIMAL(18,0), total_worker_time DECIMAL(18,0), total_elapsed_time DECIMAL(18,0), execution_count DECIMAL(18,0), mean_normal_c DECIMAL(10,6), mean_normal_p DECIMAL(10,6), mean_normal_w DECIMAL(10,6), system_impact_score DECIMAL(10,6))
;with agg_cte (schema_name, procedure_name, total_reads, total_worker_time, total_elapsed_time, execution_count, total_minutes)
AS
(
	select 
	schema_name,
	procedure_name,
	CONVERT(DECIMAL,SUM(ISNULL(total_logical_reads,0))),
	CONVERT(DECIMAL,SUM(ISNULL(total_worker_time,0))),
	CONVERT(DECIMAL,SUM(ISNULL(total_elapsed_time,0))),
	CONVERT(DECIMAL,SUM(ISNULL(execution_count,0))),
	CONVERT(DECIMAL,SUM(DATEDIFF(minute,cache_creation_date,last_execution_date)))
	FROM {0}.dbo.procedure_stats stats
    WHERE

    CONVERT(decimal,execution_count) > 0 and 
    CONVERT(decimal,total_logical_reads) > 0 AND
    CONVERT(decimal,total_worker_time) > 0 AND
    CONVERT(decimal,total_elapsed_time) > 0 AND
    DATEDIFF(minute,cache_creation_date,last_execution_date) > 0 AND
    exists (Select 1 from {0}.dbo.procedure_stats s where s.schema_name = stats.schema_name and s.procedure_name = stats.procedure_name and stats.ran_on_peak = 1)	
	GROUP BY schema_name, procedure_name
), avg_cte  (schema_name, procedure_name, total_reads, total_worker_time, total_elapsed_time, execution_count)
AS (
	SELECT
    schema_name, 
	procedure_name,
	AVG(total_reads / execution_count) OVER(PARTITION BY schema_name, procedure_name ),
	AVG(total_worker_time / execution_count) OVER(PARTITION BY  schema_name, procedure_name ),
	AVG(total_elapsed_time / execution_count) OVER(PARTITION BY  schema_name, procedure_name ),
	AVG(execution_count / total_minutes) OVER(PARTITION BY  schema_name, procedure_name )
	FROM agg_cte
)

INSERT INTO @Raw
Select schema_name,procedure_name, ROUND(total_reads,2), ROUND(total_worker_time,2), ROUND(total_elapsed_time,2), ROUND(execution_count,2),0,0,0,0  
 from avg_cte

   
DECLARE @min_cpu DECIMAL, @max_cpu DECIMAL, @min_logical_reads DECIMAL, @max_logical_reads DECIMAL, @min_wait_time DECIMAL, @max_wait_time DECIMAL
SELECT
@min_cpu = MIN(total_worker_time),
@max_cpu = MAX(total_worker_time),
@min_logical_reads = MIN(total_reads),
@max_logical_reads = MAX(total_reads),
@min_wait_time = MIN(total_elapsed_time*execution_count),
@max_wait_time = MAX(total_elapsed_time*execution_count)
FROM @Raw


UPDATE @Raw SET 
mean_normal_c = @cpu_weight*((total_worker_time-@min_cpu)/(@max_cpu-@min_cpu)),
mean_normal_p = @read_weight*((total_logical_reads-@min_logical_reads)/(@max_logical_reads-@min_logical_reads)),
mean_normal_w = @wait_time_weight*(((total_elapsed_time*execution_count)-@min_wait_time)/(@max_wait_time-@min_wait_time))
UPDATE @Raw SET system_impact_score = mean_normal_c+mean_normal_p+mean_normal_w

DELETE from {0}.dbo.candidates where stage = 2
INSERT INTO {0}.dbo.candidates (schema_name, procedure_name,stage, system_impact_score)
SELECT schema_name, procedure_name,2, system_impact_score FROM @Raw ORDER BY system_impact_score desc

INSERT INTO {0}.dbo.dependent_tables
SELECT DISTINCT s.name, procs.name, tabs.name,0
FROM {1}.sys.sql_dependencies depends 
INNER JOIN {1}.sys.procedures procs ON 
	procs.object_id = depends.object_id
INNER JOIN {1}.sys.tables     tabs ON 
	tabs.object_id = depends.referenced_major_id
INNER JOIN {1}.sys.schemas s ON
	s.schema_id = procs.schema_id
INNER JOIN {0}.dbo.candidates on 
	candidates.schema_name = s.name AND
	candidates.procedure_name = procs.name AND
	candidates.stage = 2
        
'@ -f $stat_tracking_db, $databasename, $Cpu_Weight, $Read_Weight, $WaitTime_Weight

$time_start = Get-Date -format "HHmmssffff"
Invoke-Sqlcmd -ServerInstance $servername -Database $stat_tracking_db -Query $template
$time_end = Get-Date -format "HHmmssffff"
$duration = [int]$time_end - [int]$time_start
$now = Get-Date
Write-Output "Captured Objective Function Metrics for $servername -> $stat_tracking_db -> duration: $duration ms -> at: $now"

#Sample Usage
#. ./phase1-compute-objective-function.ps1 -servername "<your-server>" -stat_tracking_db "<your-statistics-database>"