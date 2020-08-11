using ServiceStack.DataAnnotations;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching.Tables
{
    public class T_Payers : BaseCacheEntity
    {
        [Index]
        [Unique]
        public long pay_ID { get; set; }
        public string pay_Name { get; set; }
        public override void Merge(ScheduleInfo scheduleDomainObject)
        {
            
        }
    }


}