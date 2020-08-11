using System;
using ServiceStack.DataAnnotations;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching.Tables
{
    public class T_ScheduleTemplates : BaseCacheEntity
    {
        [Index]
        [Unique]
        public long scht_ID { get; set; }

        public override void Merge(ScheduleInfo scheduleDomainObject)
        {
            throw new NotImplementedException();
        }
    }

   
}