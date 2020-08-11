using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using StackExchange.Redis;
using Stratis.Soneto.BL.Common;
using Stratis.Soneto.BL.Security;
using Stratis.Soneto.Info.Scheduling;
using Stratis.Soneto.Web2.BL.Caches;

using Stratis.Soneto.Web2.BL.Scheduling;

namespace SimulatorConsole.Tests
{
    public class ScheduleWithResponseCache : ScheduleBaselineTest, IResponseCache
    {
        internal static IDatabase redis_db;
        internal static ConnectionMultiplexer redis_connection;
        internal int cache_hits=0;
        public override void Initialize(string[] args)
        {
            redis_connection = ConnectionMultiplexer.Connect("localhost,allowAdmin=true");
            redis_db = redis_connection.GetDatabase();
        }
        public override bool UsesResponseCache()
        {
            return true;
        }

        public override string TestName()
        {
            return "ScheduleDetailResponseCacheTest";
        }

        public override object ExecuteTest(long[] ids)
        {
            var hashKey = string.Join(".", ids);
            var result = redis_db.StringGet(hashKey);
            if (result.HasValue)
            {
                Console.WriteLine($"Cache Hit on {hashKey}");
                cache_hits++;
                var deserialized = JsonConvert.DeserializeObject<ScheduleInfo>(result);

                return deserialized;
            }

            try
            {
                var bl = new ScheduleBl();
                var lanConn = new LANConnectionInfo(Context.DatabaseName, "sbsdba", Context.ServerDB.ConnectionString) { IsPublic = true };
                var container = new DatabaseBlContainer(lanConn);

                var output= bl.GetOccurrences(container, ids);
                if (output != null)
                {
                    var serialized = JsonConvert.SerializeObject(output);
                    redis_db.StringSet(hashKey, serialized);
                }

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        int IResponseCache.CacheHits()
        {
            return cache_hits;
        }
    }
}
