using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching.Tables
{
    public class T_AssessmentDefinition: BaseCacheEntity
    {
        [Index]
        [Unique]
        public int asdef_ID { get; set; }
        public string asdef_Description { get; set; }
        public DateTime asdef_ModifiedDate { get; set; }
        public int asdef_TS { get; set; }
        public string asdef_ModifiedUser { get; set; }
        public DateTime asdef_CreatedDate { get; set; }
        public string asdef_CreatedUser { get; set; }

        
        public override void Merge(ScheduleInfo scheduleDomainObject)
        {
            throw new NotImplementedException();
        }
    }
}
