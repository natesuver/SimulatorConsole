namespace SimulatorConsole.Caching
{
    public sealed class CachePools
    {
        private static SqlitePool sqlitePool = null;
        private static SqlServerPool sqlServerPool = null;
        private static readonly object padlock = new object();
        CachePools()
        {
        }

        public static SqlitePool SqlitePool
        {
            get
            {
                lock (padlock)
                {
                    return sqlitePool ?? (sqlitePool = new SqlitePool());
                }
            }
        }
        public static SqlServerPool SqlServerPool
        {
            get
            {
                lock (padlock)
                {
                    return sqlServerPool ?? (sqlServerPool = new SqlServerPool());
                }
            }
        }
    }
}
