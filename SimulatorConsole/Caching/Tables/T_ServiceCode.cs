using System;
using ServiceStack.DataAnnotations;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching.Tables
{
    public class T_ServiceCode : BaseCacheEntity
    {
        [Index]
        [Unique]
        public long svcc_ID { get; set; }
        public byte svcc_RevenueCode { get; set; }
        public byte svcc_Pay { get; set; }
        public int? svcc_OverrideID { get; set; }
        public int? svcc_BranchID { get; set; }
        public bool svcc_IsExpense { get; set; }

        public override void Merge(ScheduleInfo scheduleDomainObject)
        {
            throw new NotImplementedException();
        }
    }

   
}