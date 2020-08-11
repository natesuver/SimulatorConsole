using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ServiceStack.OrmLite;
using SimulatorConsole.Caching;
using StackExchange.Redis;
using Stratis.Soneto.Web2.BL.Caches;
using Stratis.Soneto.Web2.BL.Scheduling;
using SimulatorConsole.Properties;
using SimulatorConsole.Tests;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole
{
    public class Simulator
    {
        private string DbName;
        private string ConnectionString;
        private string MasterDbConnectString;
        private string TrackingConnectString;
        private string BackupLocation;
        private int SecondsPerTest;
        private int ExecutionsPerMinute;
        private int CacheExpirationInterval;
        private string rootPath;
        private int redisPort;
        private bool RunRestore;
        private bool RunStress;
        private string TestGroupName;
        public static string outputFile;
        private Stopwatch stressStopWatch;
        private DateTime testRunEndTime;
        private DateTime testRunStartTime;
        public delegate void TestFunction(ScheduleBl bl, IDbContext context, long[] schedules);

        public static ConcurrentBag<double> tracking;
        public static List<double> serializeTimes;
        private IProcedureTest testDefinition;

        public void StartTest(int ReplayMultiplier, int execsPerMinute, int maxRequests)
        {
            RunTest(ReplayMultiplier, execsPerMinute, maxRequests);
        }

        public void Initialize(IProcedureTest testDefinition, string testGroupName)
        {
            DbName = ConfigurationManager.AppSettings["DatabaseName"];
            BackupLocation = ConfigurationManager.AppSettings["BackupLocation"];
            TestGroupName = testGroupName;
            rootPath = ConfigurationManager.AppSettings["RootPath"];
            redisPort = Convert.ToInt32(ConfigurationManager.AppSettings["RedisPort"]);
            CacheExpirationInterval = Convert.ToInt32(ConfigurationManager.AppSettings["CacheExpirationInterval"]);
            SecondsPerTest = Convert.ToInt32(ConfigurationManager.AppSettings["SecondsPerTest"]);

            ConnectionString = ConfigurationManager.ConnectionStrings["TargetDatabase"].ToString();
            MasterDbConnectString = ConfigurationManager.ConnectionStrings["MasterDatabase"].ToString();
            TrackingConnectString = ConfigurationManager.ConnectionStrings["TrackingDatabase"].ToString();
            RunRestore = Convert.ToBoolean(ConfigurationManager.AppSettings["RunRestore"]);
            RunStress = Convert.ToBoolean(ConfigurationManager.AppSettings["RunStress"]);

            tracking = new ConcurrentBag<double>();
            serializeTimes = new List<double>();
            this.testDefinition = testDefinition;
        }

        private void RunTest(int stressMultiplier, int execsPerMinute, int maxRequests)
        {
            ExecutionsPerMinute = execsPerMinute;
            var expirationIntervalMS = 1000 * 60 * CacheExpirationInterval;
            var context = new DbContext
            {
                BaseCachePath = rootPath, ServerDB = new SqlConnection(ConnectionString), Tenant = "local",DatabaseName = DbName
            };
            testDefinition.Context = context;
            
            var executeTest = true;

            var testStatusFile = $"{rootPath}scheduleMetrics_runtype_{testDefinition.TestName()}_Status.txt";
            outputFile = $"{rootPath}scheduleMetrics_runtype_{testDefinition.TestName()}_group.txt";
            RebuildFile(outputFile);
            RebuildFile(testStatusFile);

            if (RunRestore)
            {
                WriteStatus(testStatusFile, $"Restoring Database {DbName} on {DateTime.Now}");
                RestoreTestDatabase(testDefinition);
            }
            else
            {
                ApplyScripts(testDefinition);
            }
                
            if (testDefinition.UsesMiddleTierCache())
            {
                var args = new List<String> {rootPath, expirationIntervalMS.ToString()};
                testDefinition.Initialize(args.ToArray());
            }
            if (testDefinition.UsesResponseCache())
            {
                testDefinition.Initialize(null);
            }
            
            Requests requests;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                requests = testDefinition.GetRequests(conn, ExecutionsPerMinute, stressMultiplier);
            }
            Stopwatch stopWatch = new Stopwatch();
            
            ExecutionsPerMinute *= stressMultiplier;
         
            Process stressProcess = null;
            stopWatch.Start();
            testRunStartTime = DateTime.Now;
            if (RunStress)
            {
                stressStopWatch = new Stopwatch();
                stressStopWatch.Start();
                stressProcess = StartStress(stressMultiplier);
                Task.Run(() =>
                { //start metric collection after 7 seconds.
                    Thread.Sleep(7000);
                    ResetDatabaseCaches();
                    Console.WriteLine("Beginning Metric Collection...");
                });
                WriteStatus(testStatusFile, $"Starting Stress with {stressMultiplier}x multiplier");
            }
            while (executeTest)
            {
                WriteStatus(testStatusFile, $"Starting test");
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    ResetRedisCache();
                   
                    foreach (var request in requests)
                    {
                        var sleepMS = Convert.ToInt32(((decimal)60 / (decimal)ExecutionsPerMinute) * 1000);
                        System.Threading.Thread.Sleep(sleepMS); // pause the next request so that only ExecutionsPerMinute requests are made.
                        if (tracking.Count <= maxRequests)
                        {
                            BeginSingleExecution(testDefinition, request.items.ToArray());
                        }
                        
                        if (stressProcess.HasExited) //stopWatch.ElapsedMilliseconds >= 1000 * SecondsPerTest
                        {
                            testRunEndTime = DateTime.Now;
                            stressStopWatch.Stop();
                            recordTrackingMetric(testDefinition, testRunStartTime, stressMultiplier);
                            executeTest=false;
                            stopWatch.Restart();
                            testRunStartTime = DateTime.Now;
                            break;
                        }
                    }

                }
            }

            if (serializeTimes.Count > 0)
            {
                WriteStatus(testStatusFile, $"Average Serialize Time: {serializeTimes.Average()}ms");
            }

        }



        private async Task ResetDatabaseCaches()
        {
            await Cleanup("DBCC FREEPROCCACHE");
            await Cleanup("DBCC DROPCLEANBUFFERS");
            Console.WriteLine("DATABASE PROCEDURE CACHES PURGED!!");
        }
        private async Task Cleanup(string sql)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd =
                    new SqlCommand(sql,conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }


        //addTrackingMetric
        private void recordTrackingMetric(IProcedureTest test, DateTime startTime, int stressMultiplier)
        {
            var trackingItems = new List<Double>();
            trackingItems.AddRange(tracking);
            var avg = Convert.ToDecimal(trackingItems.Average());
            System.IO.File.AppendAllText(outputFile, String.Join(Environment.NewLine, tracking));
            System.IO.File.AppendAllText(outputFile, $"Average: {avg}");
            recordTrackingMetric(test, stressMultiplier, startTime, avg);
        }
        private void recordTrackingMetric(IProcedureTest test, int stressMultiplier,DateTime startTime, decimal average_exec_time)
        {
            int testrun_id;
            using (SqlConnection conn = new SqlConnection(TrackingConnectString))
            {
                var duration = stressStopWatch.ElapsedMilliseconds;
                int cacheHits = 0;
                if (test is IResponseCache)
                {
                    cacheHits = ((IResponseCache) test).CacheHits();
                }
                conn.Open();
                var metricAll = getMetrics(test.Context, String.Empty);
                var metricSingleProcedure = getMetrics(test.Context, test.StoredProcedureName());
                using (var cmd =
                    new SqlCommand($"INSERT INTO testruns (test_group_name,starttime,endtime,average_exec_time,test_name,duration,stresslevel, cache_hits, physical_reads,logical_reads,worker_time,total_physical_reads,total_logical_reads,total_worker_time, total_request_time,procedure_request_count,test_duration_ms,total_requests_processed,total_elapsed_time) VALUES ('{TestGroupName}','{startTime.ToString("G")}',GETDATE(),{average_exec_time},'{test.TestName()}',{SecondsPerTest},{stressMultiplier},{cacheHits},{metricSingleProcedure.PhysicalReads},{metricSingleProcedure.LogicalReads},{metricSingleProcedure.CpuTime},{metricAll.PhysicalReads},{metricAll.LogicalReads},{metricAll.CpuTime},{metricAll.ElapsedTime},{tracking.Count},{duration},{metricAll.TotalExecutionCount},{metricAll.TotalElapsedTime})",
                        conn))
                {
                    cmd.ExecuteNonQuery();
                }
                using (var cmd =
                    new SqlCommand($"Select MAX(id) from testruns",conn))
                {
                    testrun_id = (int) cmd.ExecuteScalar();
                }
                using (var cmd =
                    new SqlCommand(String.Format(Properties.Resources.MetricDetailSql,DbName, testrun_id), conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private Metric getMetrics(IDbContext context, string procedureName)
        {
            var result = new Metric();
            using (SqlConnection conn = new SqlConnection(TrackingConnectString))
            {
                conn.Open();
                
                string sql;
                if (String.IsNullOrEmpty(procedureName))
                {
                    sql = String.Format(Properties.Resources.MetricSqlAllDatabase, DbName);
                }
                else
                {
                    sql = String.Format(Properties.Resources.MetricSql, DbName, procedureName);
                }
                using (var cmd =
                    new SqlCommand(sql,conn))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        var hasData = rdr.Read();
                        if (!hasData) return result;
                        if (rdr["TotalLogicalReads"] != null)
                            result.LogicalReads = Convert.ToDecimal(rdr["TotalLogicalReads"]);
                        if (rdr["TotalElapsedTime"] != null)
                            result.ElapsedTime = Convert.ToDecimal(rdr["TotalElapsedTime"]);
                        if (rdr["TotalPhysicalReads"] != null)
                            result.PhysicalReads = Convert.ToDecimal(rdr["TotalPhysicalReads"]);
                        if (rdr["TotalCpuTime"] != null)
                            result.CpuTime = Convert.ToDecimal(rdr["TotalCpuTime"]);
                        if (rdr["ElapsedTime"] != null)
                            result.TotalElapsedTime = Convert.ToDecimal(rdr["ElapsedTime"]);
                        if (rdr["ExecutionCount"] != null)
                            result.TotalExecutionCount = Convert.ToInt32(rdr["ExecutionCount"]);

                    }
                }
               
                return result;
            }
        }
        private void RestoreTestDatabase(IProcedureTest test)
        {
            using (SqlConnection conn = new SqlConnection(MasterDbConnectString))
            {
                conn.Open();
                using (var cmd =
                    new SqlCommand(String.Format(Resources.Kill_Process_Sql, DbName),
                        conn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.ExecuteNonQuery();
                }
                using (var cmd =
                    new SqlCommand(String.Format(Resources.Set_Single_User_Sql, DbName),
                        conn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.ExecuteNonQuery();
                }

              
                using (var cmd =
                    new SqlCommand(String.Format(Resources.Restore_Sql, DbName, BackupLocation),
                        conn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.ExecuteNonQuery();
                }
               

                ApplyScripts(test);
            }


        }
        /// <summary>
        /// Apply any script modifications after backup occurs
        /// </summary>
        /// <param name="conn"></param>
        private void ApplyScripts(IProcedureTest test)
        {
            var scriptFolder = System.IO.Path.Combine(ConfigurationManager.AppSettings.Get("ScriptPath"), test.TestName());
            if (!System.IO.Directory.Exists(scriptFolder)) return;
            foreach (var file in System.IO.Directory.GetFiles(scriptFolder))
            {
                var sqlContent = System.IO.File.ReadAllText(file);
                string[] statements = sqlContent.Split(new[] { "GO" + Environment.NewLine }, StringSplitOptions.None);
                foreach (string statement in statements)
                {
                    //Console.WriteLine($"Executing Script {file}");
                    ExecuteScript(file, statement);
                }

            }
        }

        private void ExecuteScript(string file, string sqlContent)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd =
                    new SqlCommand(String.Format(sqlContent, DbName, BackupLocation),
                        conn))
                {
                    cmd.CommandTimeout = 600;
                    Console.WriteLine($"Executing staging script {file}");
                    cmd.ExecuteNonQuery();
                }

            }
        }

        private static Process StartStress(int replayMultiplier)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(ConfigurationManager.AppSettings["StressBatchFileLocation"]);
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.Arguments = replayMultiplier.ToString();
            startInfo.Arguments += " " + ConfigurationManager.AppSettings["ServerName"];
            startInfo.Arguments += " " + ConfigurationManager.AppSettings["DatabaseName"];
            startInfo.Arguments += " " + ConfigurationManager.AppSettings["RMLFilePattern"];
            startInfo.Arguments += " " + ConfigurationManager.AppSettings["OStressOutputPath"];
            return Process.Start(startInfo);
            

        }

        private static void KillProcessAndChildren(int pid)
        { //Taken from https://social.msdn.microsoft.com/Forums/vstudio/en-US/d60f0793-cc92-48fb-b867-dd113dabcd5c/how-to-find-the-child-processes-associated-with-a-pid?forum=csharpgeneral
            ManagementObjectSearcher processSearcher = new System.Management.ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }

            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }
        }

      

        private void addTrackingMetric(double duration)
        {
            tracking.Add(duration);
        }

        private void ResetRedisCache()
        {
            var conn= ConnectionMultiplexer.Connect("localhost,allowAdmin=true");
            if (conn == null) return;
            var server = conn.GetServer($"localhost:{redisPort}");
            server.FlushAllDatabases();
        }
        private void WriteStatus(string filename, string status)
        {
            var now = DateTime.Now.AddHours(-1).ToString("G");
            System.IO.File.AppendAllText(filename, $"{now} CST (server time) - {status}" + Environment.NewLine);
            Console.WriteLine(status);
        }
        private void RebuildFile(string outputFile)
        {
            if (System.IO.File.Exists(outputFile)) System.IO.File.Delete(outputFile);
            System.IO.File.Create(outputFile).Close();
        }

        private void BeginSingleExecution(IProcedureTest test, long[] ids)
        {
            var watch = new System.Diagnostics.Stopwatch();
            try
            {
                Task.Run(() => Run(test,ids, watch));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception getting record: " + ex.Message);
            }
        }

        private void Run(IProcedureTest test, long[] ids, Stopwatch watch)
        {
            watch.Start();
           
            var output = test.ExecuteTest(ids);
           
            StopAndReport(watch);
        }

        private void StopAndReport(Stopwatch watch)
        {
            watch.Stop();
            TimeSpan ts = watch.Elapsed;
            addTrackingMetric(ts.TotalMilliseconds);
            Console.WriteLine("Completed Execution in  " + ts.TotalMilliseconds + " ms");
        }
    }
}
