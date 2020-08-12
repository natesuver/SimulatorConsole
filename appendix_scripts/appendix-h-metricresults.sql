--Appendix H: Sql Script to pivot output test results and standard deviation, experiment 2
DECLARE @test_group_name NVARCHAR(25) = '<enter test group name>'
PRINT 'Execution Times'
SELECT * FROM (
SELECT 
test_name,
stresslevel,
AVG(average_exec_time) AS result
FROM testruns 
WHERE test_group_name = @test_group_name
GROUP BY test_name, stresslevel
) t
PIVOT (
	AVG(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table

PRINT 'Execution Times Standard Deviation'
SELECT * FROM (
SELECT 
test_name,
stresslevel,
STDEVP(average_exec_time) AS result
FROM testruns 
WHERE test_group_name = @test_group_name
GROUP BY test_name, stresslevel
) t
PIVOT (
	max(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table


PRINT 'Reads For Procedure'
SELECT * FROM (
SELECT 
test_name,
stresslevel,
AVG(logical_reads) AS result
FROM testruns 
WHERE test_group_name = @test_group_name
GROUP BY test_name, stresslevel
) t
PIVOT (
	AVG(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table

PRINT 'Reads Standard Deviation'
DECLARE @ReadResult TABLE (test_name NVARCHAR(50), stresslevel INT, result DECIMAL(18,5))
;WITH result_avg(test_name, stresslevel,result)
AS (
SELECT
test_name,
stresslevel,
(logical_reads)  AS result
FROM testruns 
WHERE test_group_name = @test_group_name
)
INSERT INTO @ReadResult
SELECT 
test_name,
stresslevel,
STDEVP(result)  AS result
FROM result_avg
GROUP BY test_name,stresslevel

SELECT * FROM (
SELECT 
test_name,
stresslevel,
MAX(result) AS result
FROM @ReadResult 
GROUP BY test_name, stresslevel
) t
PIVOT (
	AVG(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table


PRINT 'CPU Time For Procedure'
SELECT * FROM (
SELECT 
test_name,
stresslevel,
AVG((worker_time/1000)) AS result
FROM testruns 
WHERE test_group_name = @test_group_name
GROUP BY test_name, stresslevel
) t
PIVOT (
	AVG(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table


PRINT 'CPU Time Standard Deviation'
DECLARE @CPUResult TABLE (test_name NVARCHAR(50), stresslevel INT, result DECIMAL(18,5))
;WITH result_avg(test_name, stresslevel,result)
AS (
SELECT
test_name,
stresslevel,
((worker_time/1000))  AS result
FROM testruns 
WHERE test_group_name = @test_group_name
)
INSERT INTO @CPUResult
SELECT 
test_name,
stresslevel,
STDEVP(result) AS result
FROM result_avg
GROUP BY test_name,stresslevel

SELECT * FROM (
SELECT 
test_name,
stresslevel,
MAX(result) AS result
FROM @CPUResult 
GROUP BY test_name, stresslevel
) t
PIVOT (
	AVG(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table

PRINT 'Total Request Time for Entire Database'
SELECT * FROM (
SELECT 
test_name,
stresslevel,
AVG(total_request_time/1000) AS result
FROM testruns 
WHERE test_group_name = @test_group_name
GROUP BY test_name, stresslevel
) t
PIVOT (
	AVG(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table

PRINT 'Total Request Time Standard Deviation'
DECLARE @TotalRequesTimeResult TABLE (test_name NVARCHAR(50), stresslevel INT, result DECIMAL(18,5))
;WITH result_avg(test_name, stresslevel,result)
AS (
SELECT
test_name,
stresslevel,
((total_request_time/1000))  AS result
FROM testruns 
WHERE test_group_name = @test_group_name
)
INSERT INTO @TotalRequesTimeResult
SELECT 
test_name,
stresslevel,
STDEVP(result) AS result
FROM result_avg
GROUP BY test_name,stresslevel

SELECT * FROM (
SELECT 
test_name,
stresslevel,
MAX(result) AS result
FROM @TotalRequesTimeResult 
GROUP BY test_name, stresslevel
) t
PIVOT (
	AVG(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table


PRINT 'Total Reads For Entire Database'
SELECT * FROM (
SELECT 
test_name,
stresslevel,
AVG(total_logical_reads) AS result
FROM testruns 
WHERE test_group_name = @test_group_name
GROUP BY test_name, stresslevel
) t
PIVOT (
	AVG(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table

PRINT 'Total Reads Standard Deviation'
DECLARE @TotalReadResult TABLE (test_name NVARCHAR(50), stresslevel INT, result DECIMAL(18,5))
;WITH result_avg(test_name, stresslevel,result)
AS (
SELECT
test_name,
stresslevel,
(total_logical_reads)  AS result
FROM testruns 
WHERE test_group_name = @test_group_name
)
INSERT INTO @TotalReadResult
SELECT 
test_name,
stresslevel,
STDEVP(result) AS result
FROM result_avg
GROUP BY test_name,stresslevel

SELECT * FROM (
SELECT 
test_name,
stresslevel,
MAX(result) AS result
FROM @TotalReadResult 
GROUP BY test_name, stresslevel
) t
PIVOT (
	AVG(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table


PRINT 'CPU Time For Entire Database'
SELECT * FROM (
SELECT 
test_name,
stresslevel,
AVG((total_worker_time/1000)) AS result
FROM testruns 
WHERE test_group_name = @test_group_name
GROUP BY test_name, stresslevel
) t
PIVOT (
	AVG(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table

PRINT 'CPU Time, System, Standard Deviation'
DECLARE @TotalCPUResult TABLE (test_name NVARCHAR(50), stresslevel INT, result DECIMAL(18,5))
;WITH result_avg(test_name, stresslevel,result)
AS (
SELECT
test_name,
stresslevel,
((total_worker_time/1000))  AS result
FROM testruns 
WHERE test_group_name = @test_group_name
)
INSERT INTO @TotalCPUResult
SELECT 
test_name,
stresslevel,
STDEVP(result) AS result 
FROM result_avg
GROUP BY test_name,stresslevel

SELECT * FROM (
SELECT 
test_name,
stresslevel,
MAX(result) AS result
FROM @TotalCPUResult 
GROUP BY test_name, stresslevel
) t
PIVOT (
	AVG(result)
	FOR test_name IN (
	[AssessmentBaseTest],
	[AssessmentResponseCacheTest],
	[AssessmentMiddleTierTest],	
	[AssessmentMaterializedViewTest])
) AS pivot_table
