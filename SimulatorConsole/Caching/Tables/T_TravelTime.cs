using System;
using ServiceStack.DataAnnotations;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching.Tables
{
    public class T_TravelTime : BaseCacheEntity
    {
        [Index]
        public long tt_ToScheduleID { get; set; }
        public long? tt_FromScheduleID { get; set; }
        public long tt_Duration { get; set; }
        public byte tt_Payable { get; set; }
        public DateTime tt_CreatedDate { get; set; }
        public string tt_CreatedUser { get; set; }
        public DateTime tt_ModifiedDate { get; set; }
        public string tt_ModifiedUser { get; set; }
        public bool tt_AutoCalculate { get; set; }
        public decimal tt_PayRateOverrideAmt { get; set; }
        public DateTime? tt_PayRateOverrideModifiedDate { get; set; }
        public string tt_PayRateOverrideModifiedUser { get; set; }
        public override void Merge(ScheduleInfo scheduleDomainObject)
        {
            var tt = scheduleDomainObject.TravelTime;
            tt.tt_ToScheduleID = tt_ToScheduleID;
            tt.tt_FromScheduleID = tt_FromScheduleID;
            tt.tt_Duration = tt_Duration;
            tt.tt_Payable = tt_Payable;
            tt.tt_CreatedDate = tt_CreatedDate;
            tt.tt_CreatedUser = tt_CreatedUser;
            tt.tt_ModifiedDate = tt_ModifiedDate;
            tt.tt_ModifiedUser = tt_ModifiedUser;
            tt.tt_AutoCalculate = tt_AutoCalculate;
            tt.tt_PayRateOverrideAmt = tt_PayRateOverrideAmt;
            tt.tt_PayRateOverrideModifiedUser = tt_PayRateOverrideModifiedUser;
            tt.tt_PayRateOverrideModifiedDate = tt_PayRateOverrideModifiedDate;
        }
    }

   
}