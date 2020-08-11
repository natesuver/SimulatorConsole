using System;
using ServiceStack.DataAnnotations;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching.Tables
{
    public class T_AssessmentXRef : BaseCacheEntity
    {
        [Index]
        [Unique]
        public int asxref_ID { get; set; }
        [Index]
        public int asxref_ASDEF_ID { get; set; }
        [Index]
        public int asxref_ASCFG_ID { get; set; }
        public bool asxref_Editable { get; set; }
        

        public override void Merge(ScheduleInfo scheduleDomainObject)
        {
            throw new NotImplementedException();
        }
    }
}
