using System.Data;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;
using Stratis.Soneto.Web2.BL.Caches;

namespace SimulatorConsole.Caching
{
    public class SqlServerPool : BaseConnectionPool
    {
        public SqlServerPool()
        {
            OrmLiteConfig.DialectProvider = new SqlServer2008OrmLiteDialectProvider();
            ormFactory = new OrmLiteConnectionFactory();
        }
        protected override IDbConnection CreateConnection(IDbContext context)
        {
            var key = $"{context.Tenant}.sqlserver";
            OrmLiteConfig.DialectProvider = new SqlServer2008OrmLiteDialectProvider();
            if (!OrmLiteConnectionFactory.NamedConnections.ContainsKey(key))
            {
                ormFactory.RegisterConnection(key,
                    new OrmLiteConnectionFactory(context.ServerDB.ConnectionString, SqlServerDialect.Provider));
            }

            return ormFactory.OpenDbConnection(key);

        }
    }
}
