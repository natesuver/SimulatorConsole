using ServiceStack.DataAnnotations;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching.Tables
{
    public class T_ClientPhones : BaseCacheEntity
    {
        [Index]
        [Unique]
        public long clph_ID { get; set; }
        [Index]
        public long clph_ClientID { get; set; }
        public int clph_Sequence { get; set; }

        public string clph_Phone { get; set; }
        public override void Merge(ScheduleInfo scheduleDomainObject)
        {
            scheduleDomainObject.ScheduleMain.ClientPhone = clph_Phone;
        }

    }

    
}