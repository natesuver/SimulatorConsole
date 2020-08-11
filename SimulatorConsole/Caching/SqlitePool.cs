using System.Data;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using Stratis.Soneto.Web2.BL.Caches;

namespace SimulatorConsole.Caching
{
    public class SqlitePool: BaseConnectionPool
    {
        public SqlitePool()
        {
            OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();
            ormFactory = new OrmLiteConnectionFactory();
        }
        protected override IDbConnection CreateConnection(IDbContext context)
        {
            var key = $"{context.Tenant}.sqlite";
            OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();
            if (!OrmLiteConnectionFactory.NamedConnections.ContainsKey(key))
            {
                ormFactory.RegisterConnection(key,
                    new OrmLiteConnectionFactory(context.GetCacheDatabasePath(), SqliteDialect.Provider));
            }

            return ormFactory.OpenDbConnection(key);

        }
    }
}
