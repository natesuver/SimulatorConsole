using System;
using ServiceStack.DataAnnotations;
using Stratis.Soneto.Info.Scheduling;

namespace SimulatorConsole.Caching.Tables
{
    public class T_PayerServices : BaseCacheEntity
    {
        [Unique]
        public int paysvc_ID { get; set; }
        [Index]
        public int paysvc_ServiceCodeID { get; set; }
        public DateTime paysvc_EffectiveTo { get; set; }
        [Index]
        public long paysvc_PayerID { get; set; }
        public byte paysvc_RevenueCode { get; set; }
        public byte paysvc_PayCode { get; set; }
        public DateTime paysvc_EffectiveFrom { get; set; }
        public override void Merge(ScheduleInfo scheduleDomainObject)
        {
           
        }
    }

   
}