using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SimulatorConsole.Caching;
using Stratis.Soneto.BL.Operations;
using Stratis.Soneto.BL.Security;
using Stratis.Soneto.DA.Security;
using Stratis.Soneto.Web2.BL.Caches;

namespace SimulatorConsole.Tests
{
    public class AssessmentAdmissionTest : IProcedureTest
    {
        public void Initialize(string[] args)
        {
            throw new NotImplementedException();
        }
        public Requests GetRequests(SqlConnection conn, int maxRequests, int stressMultiplier)
        {
            var requests = new Requests();
            using (var cmd =
                new SqlCommand(String.Format("Select adm_ID from T_Admissions where exists (Select 1 from T_Assessment where asmt_AdmissionID = adm_ID) ORDER BY adm_ID DESC"), conn)) 
            {
                using (var rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (rdr.Read())
                    {
                        List<long> admList = new List<long>();
                        admList.Add(Convert.ToInt64(rdr[0]));
                        requests.Add(new Request(Convert.ToInt32(rdr[0]), admList));
                    }
                }
            }
            return requests;
        }

        public object ExecuteTest(long[] ids)
        {
            try
            {
                var lanConn = new LANConnectionInfo(Context.DatabaseName, "sbsdba", Context.ServerDB.ConnectionString) { IsPublic = true };
                var assessbl = new AssessmentBL(lanConn);
                SecurityContext.OverrideDaSecurity();
                return assessbl.GetAdmissionAssessments(ids[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public bool UsesResponseCache()
        {
            return false;
        }

        public bool UsesMiddleTierCache()
        {
            return false;
        }

        public string TestName()
        {
            return "AdmissionAssessmentTestMaterializedViewTest";
        }
        public string StoredProcedureName()
        {
            return "usp_Assessment_Admission_Get";
        }
        public IDbContext Context { get; set; }
    }
}
