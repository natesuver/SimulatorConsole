using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using ServiceStack.OrmLite;
using Stratis.Soneto.Web2.BL.Caches;

namespace SimulatorConsole.Caching
{
    public class BaseSqliteCache<T> : IDisposable
    {
        static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        protected static System.Timers.Timer timer;
        protected IDbContext _context;
        protected bool _cacheAvailable;
        protected OrmLiteConnectionFactory ormFactory;
        private Expression<Func<T, bool>> _filteringExpression;

        public BaseSqliteCache(IDbContext context, int expiresIn, string databaseLocation, Expression<Func<T, bool>> filteringExpression) // ":memory:"
        {

            _context = context;
            _filteringExpression = filteringExpression;
            if (expiresIn != 0)
            {
                _cacheAvailable = true;
                var iConn = 0;
                try
                {
                    var connLite = CachePools.SqlitePool.GetConnection(context, typeof(T).ToString(), out iConn);
                    connLite.CreateTableIfNotExists<T>();
                }
                finally
                {
                    CachePools.SqlitePool.FreeConnection(context,iConn);
                }
                PopulateCache();
                timer = new System.Timers.Timer { Interval = expiresIn };
                timer.Elapsed += OnTimedEvent;
                timer.AutoReset = true;
                timer.Enabled = true;
            }
            else
            {
                _cacheAvailable = false;
            }
           
           
        }

        public IDbConnection GetConnection(out int connectionId, out bool cached)
        {
            var iConn = 0;
            IDbConnection conn;
            if (_cacheAvailable)
            {
                conn = CachePools.SqlitePool.GetConnection(_context, typeof(T).ToString(), out iConn);
                cached = true;
            }
            else
            {
                conn = CachePools.SqlServerPool.GetConnection(_context, typeof(T).ToString(), out iConn);
                cached = false;
            }
            connectionId = iConn;
            return conn;
        }

        public void FreeConnection(bool cached, int identifier)
        {
            if (cached)
            {
                CachePools.SqlitePool.FreeConnection(_context, identifier);
            }
            else
            {
                CachePools.SqlServerPool.FreeConnection(_context, identifier);
            }
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            PopulateCache();
        }
        public BaseSqliteCache<T> PopulateCache()
        {
            Console.WriteLine("Repopulating Cache...");
            _semaphoreSlim.Wait();
            _cacheAvailable = false;
            var iConnLite = 0;
            var iConnSqlServer = 0;
            try
            {
                var connLite = CachePools.SqlitePool.GetConnection(_context, typeof(T).ToString(), out iConnLite);
                var connSS = CachePools.SqlServerPool.GetConnection(_context, typeof(T).ToString(), out iConnSqlServer);
                var items = _filteringExpression != null ? connSS.Select<T>(_filteringExpression) : connSS.Select<T>();
                connLite.DeleteAll<T>();
                connLite.InsertAll<T>(items);
                _cacheAvailable = true;
                _semaphoreSlim.Release();

            }
            finally
            {
                CachePools.SqlitePool.FreeConnection(_context, iConnLite);
                CachePools.SqlServerPool.FreeConnection(_context, iConnSqlServer);
            }

            
            return this;
        }
        public void Dispose()
       {
           timer?.Dispose();
       }
    }
}
