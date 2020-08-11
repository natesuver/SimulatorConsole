using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching
{
    public abstract class BaseCacheEntity
    {
        public abstract void Merge(ScheduleInfo scheduleDomainObject);
    }
}
