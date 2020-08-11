using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using SimulatorConsole.Caching;
using Stratis.Soneto.BL.Common;
using Stratis.Soneto.BL.Security;
using Stratis.Soneto.Web2.BL.Caches;
using Stratis.Soneto.Web2.BL.Scheduling;

namespace SimulatorConsole.Tests
{
    public class ScheduleBaselineTest : IProcedureTest
    {
        public virtual void Initialize(string[] args)
        {
            //throw new NotImplementedException();
        }

        public virtual Requests GetRequests(SqlConnection conn, int executionsPerMinute, int stressMultiplier)
        {
            var allRequests = new Requests();
            var percentDups = .2;
            
            var requestCount = Convert.ToInt32(executionsPerMinute * (1- percentDups))* stressMultiplier;
            var duplicateCount = Convert.ToInt32(requestCount * percentDups);
            var requests = BuildRequestList(conn, requestCount);
            allRequests.AddRange(requests.GetRange(0, duplicateCount));
            allRequests.AddRange(requests);
            
            return allRequests;
        }

        private static Requests BuildRequestList(SqlConnection conn, int requestCount)
        {
            var requests = new Requests();
            using (var cmd =
                new SqlCommand(String.Format(Properties.Resources.ClientScheduleGroupSql, requestCount), conn)
            ) 
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        requests.Add(new Request(Convert.ToInt32(rdr[0]), null));
                    }
                }
            }

            foreach (var req in requests)
            {
                List<Int64> currentScheduleList = new List<Int64>();
                using (var cmd =
                    new SqlCommand($"Select top 3 sch_ID from T_Schedules where sch_ClientID = {req.id}", conn)) //maxRequests
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            currentScheduleList.Add(Convert.ToInt64(rdr[0]));
                        }
                    }
                }

                req.items = currentScheduleList;
            }

            return requests;
        }

        public virtual object ExecuteTest(long[] ids)
        {
            try
            {
                var bl = new ScheduleBl();
                var lanConn = new LANConnectionInfo(Context.DatabaseName, "sbsdba", Context.ServerDB.ConnectionString) { IsPublic = true };
                var container = new DatabaseBlContainer(lanConn);

                return bl.GetOccurrences(container, ids);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public virtual bool UsesResponseCache()
        {
            return false;
        }

        public virtual bool UsesMiddleTierCache()
        {
            return false;
        }

        public virtual string TestName()
        {
            return "BaselineScheduleDetailTest";
        }
        public virtual string StoredProcedureName()
        {
            return "usp_Schedules_GetByID";
        }
        public IDbContext Context { get; set; }
    }
}
