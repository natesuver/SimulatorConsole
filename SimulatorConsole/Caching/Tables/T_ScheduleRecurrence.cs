using ServiceStack.DataAnnotations;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching.Tables
{
    public class T_ScheduleRecurrence : BaseCacheEntity
    {
        [Index]
        [Unique]
        public long schrec_ID { get; set; }
        public long schrec_RootAppointment { get; set; }
        public int schrec_PatternFrequency { get; set; }
        public int schrec_PatternInterval { get; set; }
        public override void Merge(ScheduleInfo scheduleDomainObject)
        {
            scheduleDomainObject.ScheduleMain.AllowMulitTemplateRec =
                (schrec_PatternFrequency == 1 && schrec_PatternInterval == 1);
        }
    }

   
}
