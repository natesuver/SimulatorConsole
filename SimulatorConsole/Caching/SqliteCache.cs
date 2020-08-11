using System;
using ServiceStack.OrmLite;
using Stratis.Soneto.Web2.BL.Caches;

namespace SimulatorConsole.Caching
{
    public class SqliteCache : BaseWebCache
    {
      

        public override void InitializeCache()
        {
            var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
            var db = dbFactory.Open();     //Open ADO.NET DB Connection
        }

        public override void Clear()
        {
            throw new NotImplementedException();
        }

        public override void AddToCache(string key, object value)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFromCache(string key)
        {
            throw new NotImplementedException();
        }

        public override object GetCacheData(string key)
        {
            throw new NotImplementedException();
        }

        public override bool IsCacheInitialized()
        {
            throw new NotImplementedException();
        }

        public override bool CacheItemExists(string key)
        {
            throw new NotImplementedException();
        }
    }
}
