using ServiceStack.DataAnnotations;
using Stratis.Soneto.Info.Common;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching.Tables
{
    public class T_TherapyCounts : BaseCacheEntity
    {
        [Index]
        [Unique]
        public long tcnt_ScheduleID { get; set; }
        public byte tcnt_Warning { get; set; }

        public override void Merge(ScheduleInfo scheduleDomainObject)
        {
            scheduleDomainObject.TherapyWarning = (TherapyWarningType)tcnt_Warning;
        }
    }

  
}