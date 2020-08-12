//Appendix G: Sample IProcedureTest implementation
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Stratis.Soneto.Web2.BL.Caches;

namespace SimulatorConsole.Tests.Assessment
{
    public class AssessmentBaseTest : IProcedureTest
    {
        public virtual void Initialize(string[] args)
        {
            
        }

        public virtual Requests GetRequests(SqlConnection conn, int executionsPerMinute, int stressMultiplier)
        {
            var allRequests = new Requests();
            var percentDups = .8;
            var requests = BuildRequestList(conn, 0);
            for (var i = 1; i <= 5; i++)
            {
                allRequests.AddRange(requests);
            }
            return allRequests;
        }
        public virtual Requests BuildRequestList(SqlConnection conn, int maxRequests)
        {
            var requests = new Requests();
            using (var cmd = new SqlCommand($"Select top 20 ascfg_ID from T_AssessmentConfiguration order by ascfg_ID desc", conn))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var id = Convert.ToInt32(rdr[0]);
                        var lst = new List<Int64> {id};
                        requests.Add(new Request(id, lst));
                    }
                }
            }

            return requests;
        }

        public virtual object ExecuteTest(long[] ids)
        {
            var info = new AssessmentConfigurationInfo();
            using (var conn = new SqlConnection(Context.ServerDB.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("usp_AssessmentGetDefinitionIds", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("@ascfg_ID", SqlDbType.Int);
                    cmd.Parameters["@ascfg_ID"].Value = Convert.ToInt32(ids[0]);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        AssessmentDABase.SetDefinitionIds(rdr, info);
                    }
                }
            }

            return info;
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
            return "AssessmentBaseTest";
        }

        public virtual string StoredProcedureName()
        {
            return "usp_AssessmentGetDefinitionIds";
        }

        public IDbContext Context { get; set; }
    }
}
