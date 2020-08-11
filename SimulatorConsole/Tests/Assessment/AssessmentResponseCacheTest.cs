using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using Stratis.Soneto.BL.Common;
using Stratis.Soneto.BL.Security;
using Stratis.Soneto.BL.Shared.Scheduling;
using Stratis.Soneto.DA.Operations;
using Stratis.Soneto.Info.Operations;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Tests.Assessment
{
    public class AssessmentResponseCacheTest : AssessmentBaseTest, IResponseCache
    {
        internal static IDatabase redis_db;
        internal static ConnectionMultiplexer redis_connection;
        internal int cache_hits = 0;

        public override void Initialize(string[] args)
        {
            redis_connection = ConnectionMultiplexer.Connect("localhost,allowAdmin=true");
            redis_db = redis_connection.GetDatabase();
        }

        public override object ExecuteTest(long[] ids)
        {
            var hashKey = string.Join(".", ids);
            var result = redis_db.StringGet(hashKey);
            if (result.HasValue)
            {
                Console.WriteLine($"Cache Hit on {hashKey}");
                cache_hits++;
                var deserialized = JsonConvert.DeserializeObject<AssessmentConfigurationInfo>(result);

                return deserialized;
            }

            try
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

                var serialized = JsonConvert.SerializeObject(info);
                redis_db.StringSet(hashKey, serialized);
                return info;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public int CacheHits()
        {
            return cache_hits;
        }


        public override bool UsesResponseCache()
        {
            return true;
        }


        public override string TestName()
        {
            return "AssessmentResponseCacheTest";
        }
    }
}
