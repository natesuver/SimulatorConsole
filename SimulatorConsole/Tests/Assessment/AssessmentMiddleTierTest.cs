using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite.Legacy;
using SimulatorConsole.Caching;
using SimulatorConsole.Caching.Tables;
using Stratis.Soneto.DA.Operations;
using Stratis.Soneto.Info.Operations;
using Stratis.Soneto.Web2.BL.Caches;

namespace SimulatorConsole.Tests.Assessment
{
    public class AssessmentMiddleTierTest: AssessmentBaseTest
    {
        private BaseSqliteCache<T_AssessmentDefinition> T_AssessmentDefinitionCache;
        private BaseSqliteCache<T_AssessmentXRef> T_AssessmentXRefCache;
        private IDbConnection T_AssessmentXRefconn;
        private IDbConnection T_AssessmentDefinitionConn;
        public override void Initialize(string[] args)
        {
            var rootPath = args[0];
            var expirationInterval = Convert.ToInt32(args[1]);
            var dbPath = $"{rootPath}cache.db"; //;":memory:"
            var cacheList = new InMemoryCache();
            cacheList.AddToCache("AssessmentDefinitionCache", new BaseSqliteCache<T_AssessmentDefinition>(Context, expirationInterval, dbPath,null));
            cacheList.AddToCache("AssessmentXRefCache", new BaseSqliteCache<T_AssessmentXRef>(Context, expirationInterval, dbPath, null));
            T_AssessmentDefinitionCache = (BaseSqliteCache<T_AssessmentDefinition>)new InMemoryCache().GetCacheData("AssessmentDefinitionCache");
            T_AssessmentXRefCache = (BaseSqliteCache<T_AssessmentXRef>)new InMemoryCache().GetCacheData("AssessmentXRefCache");
            var iConn1 = 0; bool cached1 = false;
            var iConn2 = 0; bool cached2 = false;
            T_AssessmentXRefconn = T_AssessmentXRefCache.GetConnection(out iConn1, out cached1);
            T_AssessmentDefinitionConn = T_AssessmentDefinitionCache.GetConnection(out iConn2, out cached2);
        }


        public override object ExecuteTest(long[] ids)
        {
            var info = new AssessmentConfigurationInfo
            {
                AssessmentDefinition = new AssessmentDefinitionInfoCollection()
            };
            var asxrefData = T_AssessmentXRefconn.Select<T_AssessmentXRef>(r=>r.Where(a=>a.asxref_ASCFG_ID==ids[0]));
            var asdefIds = (from asxref in asxrefData select asxref.asxref_ASDEF_ID).ToList();
            var asdefData = T_AssessmentDefinitionConn.Select<T_AssessmentDefinition>(a => a.Where(x => asdefIds.Contains(x.asdef_ID)));
            foreach (var asxref in asxrefData)
            {
               var asdefDataItem = asdefData.FirstOrDefault(a => a.asdef_ID == asxref.asxref_ASDEF_ID);
               var asdef = new AssessmentDefinitionInfo();
               asdef.asdef_ID = asdefDataItem.asdef_ID;
               asdef.asdef_Description = asdefDataItem.asdef_Description;
               asdef.TS = asdefDataItem.asdef_TS;
               asdef.CreatedDate = asdefDataItem.asdef_CreatedDate;
               asdef.ModifiedDate = asdefDataItem.asdef_ModifiedDate;
               asdef.ModifiedUser = asdefDataItem.asdef_ModifiedUser;
               asdef.CreatedUser = asdefDataItem.asdef_CreatedUser;
               asdef.asxref_Editable = asxref.asxref_Editable;
               info.AssessmentDefinition.Add(asdef);
            }
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
                        Console.WriteLine("Executed stub usp_AssessmentGetDefinitionIds");//AssessmentDABase.SetDefinitionIds(rdr, info);
                    }
                }
            }
            return info;
        }

        public override bool UsesMiddleTierCache()
        {
            return true;
        }

        public override string TestName()
        {
            return "AssessmentMiddleTierTest";
        }
    }
}
