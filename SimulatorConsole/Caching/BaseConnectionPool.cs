using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using ServiceStack.OrmLite;
using Stratis.Soneto.Web2.BL.Caches;

namespace SimulatorConsole.Caching
{
    //Based on https://www.codeproject.com/Articles/710384/Creating-a-Custom-Database-Connection-Pool, with changes to support multi-tenancy.
    public abstract class BaseConnectionPool
    {
        protected Dictionary<string, ConnectionDetail> connections;
        public const int POOL_SIZE = 100;
        public const int MAX_IDLE_TIME = 10;
        protected OrmLiteConnectionFactory ormFactory;
        protected IOrmLiteDialectProvider ormProvider;

        public BaseConnectionPool()
        {
            connections = new Dictionary<string, ConnectionDetail>();
        }

        public IDbConnection GetConnection(IDbContext context,string tableName, out int identifier)
        {
            if (!connections.ContainsKey(context.Tenant))
            {
                connections.Add(context.Tenant,new ConnectionDetail());
            }

            ConnectionDetail connDetail;
            connections.TryGetValue(context.Tenant, out connDetail);
            if (connDetail == null) throw new Exception("connection not found");

            for (int i = 0; i < POOL_SIZE; i++)
            {
                if (Interlocked.CompareExchange(ref connDetail.Locks[i], 1, 0) == 0)
                {
                    if (connDetail.Dates[i] != DateTime.MinValue && (DateTime.Now - connDetail.Dates[i]).TotalMinutes > MAX_IDLE_TIME)
                    {
                        connDetail.Connections[i].Dispose();
                        connDetail.Connections[i] = null;
                    }

                    if (connDetail.Connections[i] == null)
                    {
                        IDbConnection conn = CreateConnection(context);
                        connDetail.Connections[i] = conn;
                        conn.Open();
                    }

                    connDetail.Dates[i] = DateTime.Now;
                    identifier = i;
                    return connDetail.Connections[i];
                }
            }

            throw new Exception("No free connections");
        }

        protected abstract IDbConnection CreateConnection(IDbContext context);
      
        public void FreeConnection(IDbContext context, int identifier)
        {
            ConnectionDetail connDetail;
            connections.TryGetValue(context.Tenant, out connDetail);
            if (connDetail == null) throw new Exception("connection not found");
            if (identifier < 0 || identifier >= POOL_SIZE)
                return;

            Interlocked.Exchange(ref connDetail.Locks[identifier], 0);
        }
    }

    public class ConnectionDetail
    {
        protected const int POOL_SIZE = 100;
        public IDbConnection[] Connections;
        public int[] Locks;
        public DateTime[] Dates;

        public ConnectionDetail()
        {
            Connections = new IDbConnection[BaseConnectionPool.POOL_SIZE];
            Locks = new int[BaseConnectionPool.POOL_SIZE];
            Dates = new DateTime[BaseConnectionPool.POOL_SIZE];
        }
    }
}
