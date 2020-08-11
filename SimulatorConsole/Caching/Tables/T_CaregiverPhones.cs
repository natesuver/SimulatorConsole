using ServiceStack.DataAnnotations;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching.Tables
{
    public class T_CaregiverPhones : BaseCacheEntity
    {
        [Index]
        [Unique]
        public long cph_ID { get; set; }
        [Index]
        public long cph_CaregiverID { get; set; }
        public string cph_Phone { get; set; }
        public int cph_Sequence { get; set; }

        public override void Merge(ScheduleInfo scheduleDomainObject)
        {
            scheduleDomainObject.ScheduleMain.CaregiverPhone = cph_Phone;
        }

    }
}