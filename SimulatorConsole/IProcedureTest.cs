using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimulatorConsole.Caching;
using Stratis.Soneto.Web2.BL.Caches;

namespace SimulatorConsole
{
    public interface IProcedureTest
    {
        void Initialize(string[] args);
        Requests GetRequests(SqlConnection conn, int maxRequests, int stressMultiplier);
        object ExecuteTest(long[] ids);
        bool UsesResponseCache();
        bool UsesMiddleTierCache();
        string TestName();
        string StoredProcedureName();
        IDbContext Context { get; set; }
    }
}
