#Appendix A: Powershell script used to setup performance metrics tables

Param 
(
    [string] $servername = $(throw "You must supply a database server name"),
    [string] $databasename = $(throw "You must supply a database name, used for performance metrics")
) 
Add-PSSnapin SqlServerCmdletSnapin100
Add-PSSnapin SqlServerProviderSnapin100
$create_table =  @'
IF NOT EXISTS (Select * from sys.objects where name='candidates') 
BEGIN 
    CREATE TABLE [dbo].[candidates](
        [schema_name] [nvarchar](30) NOT NULL,
        [procedure_name] [nvarchar](250) NOT NULL,
        [system_impact_score] INT NULL,
        [stage] [int] NOT NULL,
    CONSTRAINT [PK_candidates] PRIMARY KEY CLUSTERED 
    (
        [schema_name] ASC,
        [procedure_name] ASC,
        [stage] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]  
END
IF NOT EXISTS (Select * from sys.objects where name='procedure_stats') 
BEGIN 
    CREATE TABLE [dbo].[procedure_stats](
        [schema_name] [nvarchar](30) NOT NULL,
        [procedure_name] [nvarchar](250) NOT NULL,
		[cache_creation_date] datetime NOT NULL,
        [ran_on_peak] [bit] NOT NULL,
		[total_reads] DECIMAL NULL,
		[total_worker_time]  DECIMAL NULL,
		[total_elapsed_time]   DECIMAL NULL,
        [execution_count] DECIMAL NULL,
        [last_execution_date] datetime NOT NULL
    CONSTRAINT [PK_procedure_stats] PRIMARY KEY CLUSTERED 
    (
        [schema_name] ASC,
        [procedure_name] ASC,
        [cache_creation_date] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]  
END
IF NOT EXISTS (Select * from sys.objects where name='dependent_tables') 
BEGIN 
    CREATE TABLE [dbo].[dependent_tables](
        [schema_name] [NVARCHAR](30) NOT NULL,
        [procedure_name] [NVARCHAR](250) NOT NULL,
        [table_name] [NVARCHAR](100) NOT NULL,
        [total_writes] [INT] NULL
    CONSTRAINT [PK_dependent_tables] PRIMARY KEY CLUSTERED 
    (
        [schema_name] ASC,
        [procedure_name] ASC,
        [table_name] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
END
IF NOT EXISTS (Select * from sys.objects where name='table_usage_stats') 
BEGIN 
    CREATE TABLE [dbo].[table_usage_stats](
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [TableName] [nvarchar](50) NOT NULL,
        [ProcessTime] [datetime] NULL,
        [LastActionTime] [datetime] NULL,
        [TotalRowsAffected] [int] NULL,
        [DeltaRowsAffected] [int] NULL,
    CONSTRAINT [PK_table_usage_stats] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY];

    CREATE NONCLUSTERED INDEX [IX_LastActionTime] ON [dbo].[table_usage_stats] 
    (
        [TableName] ASC,
        [LastActionTime] ASC
    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END

IF NOT EXISTS (Select * from sys.objects where name='testruns') 
BEGIN 
    CREATE TABLE [dbo].[testruns](
        [id] [INT] IDENTITY(1,1) NOT NULL,
        [starttime] [DATETIME] NOT NULL,
        [endtime] [DATETIME] NOT NULL,
        [average_exec_time] [DECIMAL](18, 4) NOT NULL,
        [test_name] [VARCHAR](50) NOT NULL,
        [duration] [INT] NOT NULL,
        [stresslevel] [INT] NULL,
        [cache_hits] [INT] NULL,
        [physical_reads] [DECIMAL](18, 4) NULL,
        [logical_reads] [DECIMAL](18, 4) NULL,
        [worker_time] [DECIMAL](18, 4) NULL,
        [notes] [NVARCHAR](MAX) NULL,
        [total_physical_reads] [DECIMAL](18, 4) NULL,
        [total_logical_reads] [DECIMAL](18, 4) NULL,
        [total_worker_time] [DECIMAL](18, 4) NULL,
        [total_request_time] [DECIMAL](18, 4) NULL,
        [procedure_request_count] [INT] NULL,
        [test_group_name] [NVARCHAR](50) NULL,
        [test_duration_ms] [INT] NULL,
        [total_requests_processed] [INT] NULL,
        [total_elapsed_time] [INT] NULL,
    CONSTRAINT [PK_testruns] PRIMARY KEY CLUSTERED 
    (
        [id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
IF NOT EXISTS (Select * from sys.objects where name='testrun_data') 
BEGIN 
    CREATE TABLE [dbo].[testrun_data](
        [id] [INT] IDENTITY(1,1) NOT NULL,
        [testrun_id] [INT] NULL,
        [SqlText] [NVARCHAR](250) NOT NULL,
        [Elapsed_Time] [BIGINT] NOT NULL,
        [Cpu] [BIGINT] NOT NULL,
        [Reads] [BIGINT] NOT NULL,
        [Execution_Count] [BIGINT] NOT NULL,
    CONSTRAINT [PK_testrun_data] PRIMARY KEY CLUSTERED 
    (
        [id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
END

'@  
Invoke-Sqlcmd -ServerInstance $servername -Database $databasename -Query $create_table 


Write-Output "Table Setup Complete!"
#Sample Usage
#.\Setup-Tables.ps1 -servername "<your server>" -databasename "<your performance database>" 