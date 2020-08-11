using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using SimulatorConsole.Caching;
using SimulatorConsole.Caching.Tables;
using Stratis.Soneto.BL.Common;
using Stratis.Soneto.BL.Security;
using Stratis.Soneto.Info.Scheduling;
using Stratis.Soneto.Web2.BL.Caches;
using Stratis.Soneto.Web2.BL.Scheduling;

namespace SimulatorConsole.Tests
{
    public class ScheduleWithMiddleTierCache: ScheduleBaselineTest
    { 

        public override object ExecuteTest(long[] ids)
        {
            try
            {
                var bl = new ScheduleBl();
                var result= bl.ScheduleCacheOptimized.Get(Context,ids);
                MergeCaregiverPhones(Context, result);
                MergeSchedulePayers(Context, result);
                MergeExpensePayers(Context, result);
                MergePayerServices(Context, result);
                MergeTherapyCounts(Context, result);
                MergeRecurrences(Context, result);
                MergeTravelTime(Context, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public override bool UsesMiddleTierCache()
        {
            return true;
        }

        public override string TestName()
        {
            return "ScheduleWithMiddleTierCache";
        }
        public override string StoredProcedureName()
        {
            return "usp_Schedules_GetByID_Optimize_2_Request_1_All";
        }
        public override void Initialize(string[] args)
        {
            var rootPath = args[0];
            var expirationInterval = Convert.ToInt32(args[1]);
            var dbPath = $"{rootPath}cache.db"; //;":memory:"
            var cacheList = new InMemoryCache();
            cacheList.AddToCache("CaregiverPhoneCache", new BaseSqliteCache<T_CaregiverPhones>(Context, expirationInterval, dbPath, (a => a.cph_Sequence == 1)));
            cacheList.AddToCache("ClientPhoneCache", new BaseSqliteCache<T_ClientPhones>(Context, expirationInterval, dbPath, (a => a.clph_Sequence == 1)));
            cacheList.AddToCache("PayersCache", new BaseSqliteCache<T_Payers>(Context, expirationInterval, dbPath, null));
            cacheList.AddToCache("PayerServicesCache", new BaseSqliteCache<T_PayerServices>(Context, expirationInterval, dbPath, null));
            cacheList.AddToCache("ScheduleRecurrenceCache", new BaseSqliteCache<T_ScheduleRecurrence>(Context, expirationInterval, dbPath, null));
            cacheList.AddToCache("ScheduleTemplateCache", new BaseSqliteCache<T_ScheduleTemplates>(Context, expirationInterval, dbPath, null));
            cacheList.AddToCache("ServiceCodeCache", new BaseSqliteCache<T_ServiceCode>(Context, expirationInterval, dbPath, null));
            cacheList.AddToCache("TherapyCountsCache", new BaseSqliteCache<T_TherapyCounts>(Context, expirationInterval, dbPath, null));
            cacheList.AddToCache("TravelTimeCache", new BaseSqliteCache<T_TravelTime>(Context, expirationInterval, dbPath, null));
        }



        public virtual void MergeCaregiverPhones(IDbContext context, ScheduleInfoCollection schedules)
        {
            var ids = (from schedule in schedules select schedule.CaregiverId).ToList();
            if (ids.Count == 0) return;
            var dbCache = (BaseSqliteCache<T_CaregiverPhones>)new InMemoryCache().GetCacheData("CaregiverPhoneCache");
            var iConn = 0; bool cached = false;
            try
            {
                var conn = dbCache.GetConnection(out iConn, out cached);
                var data = conn.Select<T_CaregiverPhones>(a => ids.Contains(a.cph_CaregiverID));
                foreach (var schedule in schedules)
                {
                    var phone = data.FirstOrDefault(a => a.cph_CaregiverID == schedule.CaregiverId);
                    phone?.Merge(schedule);
                }
            }
            finally { dbCache.FreeConnection(cached, iConn); }

        }

        public virtual void MergeClientPhones(IDbContext context, ScheduleInfoCollection schedules)
        {
            var ids = (from schedule in schedules select schedule.ClientId).ToList();
            if (ids.Count == 0) return;
            var dbCache = (BaseSqliteCache<T_ClientPhones>)new InMemoryCache().GetCacheData("ClientPhoneCache");
            var iConn = 0; bool cached = false;

            try
            {
                var conn = dbCache.GetConnection(out iConn, out cached);
                var data = conn.Select<T_ClientPhones>(a => ids.Contains(a.clph_ClientID));
                foreach (var schedule in schedules)
                {
                    var phone = data.FirstOrDefault(a => a.clph_ClientID == schedule.ClientId);
                    phone?.Merge(schedule);
                }
            }
            finally { dbCache.FreeConnection(cached, iConn); }
        }

        public virtual void MergeTherapyCounts(IDbContext context, ScheduleInfoCollection schedules)
        {
            var ids = (from schedule in schedules select schedule.sch_ID).ToList();
            if (ids.Count == 0) return;
            var iConn = 0; bool cached = false;
            var dbCache = (BaseSqliteCache<T_TherapyCounts>)new InMemoryCache().GetCacheData("TherapyCountsCache");
            try
            {
                var conn = dbCache.GetConnection(out iConn, out cached);
                var data = conn.Select<T_TherapyCounts>(a => ids.Contains(a.tcnt_ScheduleID));
                foreach (var schedule in schedules)
                {
                    var item = data.FirstOrDefault(a => a.tcnt_ScheduleID == schedule.sch_ID);
                    item?.Merge(schedule);
                }
            }
            finally { dbCache.FreeConnection(cached, iConn); }



        }

        public virtual void MergeRecurrences(IDbContext context, ScheduleInfoCollection schedules)
        {
            var ids = (from schedule in schedules select schedule.ScheduleMain.sch_RecurrenceId).ToList();
            if (ids.Count == 0) return;
            var dbCache = (BaseSqliteCache<T_ScheduleRecurrence>)new InMemoryCache().GetCacheData("ScheduleRecurrenceCache");
            var iConn = 0; bool cached = false;

            try
            {
                var conn = dbCache.GetConnection(out iConn, out cached);
                var data = conn.Select<T_ScheduleRecurrence>(a => ids.Contains(a.schrec_ID));
                foreach (var schedule in schedules)
                {
                    var item = data.FirstOrDefault(a => a.schrec_ID == schedule.ScheduleMain.sch_RecurrenceId);
                    if (item == null) continue;
                    item?.Merge(schedule);
                    var templateId = conn.Select<T_ScheduleTemplates>(a => item.schrec_RootAppointment == a.scht_ID);
                    schedule.ScheduleMain.RecIsMultTemplate = (templateId != null);
                }
            }
            finally
            {
                dbCache.FreeConnection(cached, iConn);
            }


        }

        public virtual void MergeSchedulePayers(IDbContext context, ScheduleInfoCollection schedules)
        {
            var ids = (from schedule in schedules select schedule.ScheduleMain.PrimaryPayerID).ToList();
            if (ids.Count == 0) return;
            var dbCache = (BaseSqliteCache<T_Payers>)new InMemoryCache().GetCacheData("PayersCache");
            var iConn = 0; bool cached = false;

            try
            {
                var conn = dbCache.GetConnection(out iConn, out cached);
                var data = conn.Select<T_Payers>(a => ids.Contains(a.pay_ID));
                foreach (var schedule in schedules)
                {
                    var item = data.FirstOrDefault(a => a.pay_ID == schedule.ScheduleMain.PrimaryPayerID);
                    if (item != null) schedule.ScheduleMain.PrimaryPayerName = item.pay_Name;
                }
            }
            finally
            {
                dbCache.FreeConnection(cached, iConn);
            }

        }

        public virtual void MergeExpensePayers(IDbContext context, ScheduleInfoCollection schedules)
        {
            var ids = (from schedule in schedules
                       from expense in schedule.ScheduleExpensess
                       select expense.ExpensePrimaryPayerID).ToList();
            if (ids.Count == 0) return;
            var dbCache = (BaseSqliteCache<T_Payers>)new InMemoryCache().GetCacheData("PayersCache");
            var iConn = 0; bool cached = false;

            try
            {
                var conn = dbCache.GetConnection(out iConn, out cached);
                var data = conn.Select<T_Payers>(a => ids.Contains(a.pay_ID));
                foreach (var schedule in schedules)
                {
                    foreach (var expense in schedule.ScheduleExpensess)
                    {
                        var item = data.FirstOrDefault(a => a.pay_ID == expense.ExpensePrimaryPayerID);
                        if (item != null) expense.ExpensePrimaryPayerName = item.pay_Name;
                    }
                }
            }
            finally
            {
                dbCache.FreeConnection(cached, iConn);
            }

        }

        public virtual void MergePayerServices(IDbContext context, ScheduleInfoCollection schedules)
        {
            var dbCache = (BaseSqliteCache<T_PayerServices>)new InMemoryCache().GetCacheData("PayerServicesCache");
            var iConn = 0; bool cached = false;

            try
            {
                var conn = dbCache.GetConnection(out iConn, out cached);
                foreach (var schedule in schedules)
                {
                    var payerServiceData = conn.Select<T_PayerServices>(a => a.paysvc_PayerID == schedule.ScheduleMain.PrimaryPayerID &&
                                                                                             a.paysvc_ServiceCodeID == schedule.ScheduleMain.sch_ServiceCodeID &&
                                                                                             a.paysvc_EffectiveTo >= schedule.StartDate &&
                                                                                             a.paysvc_EffectiveFrom <= schedule.StartDate).FirstOrDefault();


                    if (payerServiceData != null)
                    {
                        var service = GetServiceCodeOverride(context, schedule.ScheduleMain.sch_ServiceCodeID);
                        schedule.ScheduleBill.DefaultRevenueCode = service.svcc_RevenueCode;
                        schedule.SchedulePay.DefaultPayCode = service.svcc_Pay;
                        if (payerServiceData.paysvc_RevenueCode > 0)
                            schedule.ScheduleBill.DefaultRevenueCode = payerServiceData.paysvc_RevenueCode;
                        if (payerServiceData.paysvc_PayCode != 2)
                            schedule.SchedulePay.DefaultPayCode = payerServiceData.paysvc_PayCode;
                    }

                    foreach (var expense in schedule.ScheduleExpensess)
                    {
                        var payerServiceExpenseData = conn.Select<T_PayerServices>(a => a.paysvc_PayerID == schedule.ScheduleMain.PrimaryPayerID &&
                                                                                                          a.paysvc_ServiceCodeID == schedule.ScheduleMain.sch_ServiceCodeID &&
                                                                                                          a.paysvc_EffectiveTo >= schedule.StartDate &&
                                                                                                          a.paysvc_EffectiveFrom <= schedule.StartDate).FirstOrDefault();
                        if (payerServiceExpenseData != null)
                        {
                            var service = GetServiceCodeOverride(context, expense.schexp_expID.Value);
                            expense.DefaultRevenueCode = service.svcc_RevenueCode;
                            expense.DefaultPayCode = service.svcc_Pay;
                            if (payerServiceExpenseData.paysvc_RevenueCode > 0)
                                expense.DefaultRevenueCode = payerServiceExpenseData.paysvc_RevenueCode;
                            if (payerServiceExpenseData.paysvc_PayCode != 2)
                                expense.DefaultPayCode = payerServiceExpenseData.paysvc_PayCode;
                        }
                    }

                }
            }
            finally
            {
                dbCache.FreeConnection(cached, iConn);
            }



        }

        public virtual void MergeTravelTime(IDbContext context, ScheduleInfoCollection schedules)
        {
            var ids = (from schedule in schedules select schedule.sch_ID).ToList(); //obtain a distinct list of all lookup ids
            if (ids.Count == 0) return;
            var dbCache = (BaseSqliteCache<T_TravelTime>)new InMemoryCache().GetCacheData("TravelTimeCache");
            var iConn = 0; bool cached = false;

            try
            {
                var conn = dbCache.GetConnection(out iConn, out cached);
                //Perform a single SELECT operation against the SQLite datastore.  Equivalent to Select * from Table where Col IN (1,2,3,4,5)
                var data = conn.Select<T_TravelTime>(a => ids.Contains(a.tt_ToScheduleID));
                foreach (var schedule in schedules)
                {
                    var phone = data.FirstOrDefault(a => a.tt_ToScheduleID == schedule.sch_ID);
                    phone?.Merge(schedule);
                }
            }
            finally
            {
                dbCache.FreeConnection(cached, iConn);
            }



        }

        protected T_ServiceCode GetServiceCodeOverride(IDbContext context, int id)
        {
            var dbCache = (BaseSqliteCache<T_ServiceCode>)new InMemoryCache().GetCacheData("ServiceCodeCache");
            var iConn = 0; bool cached = false;

            try
            {
                var conn = dbCache.GetConnection(out iConn, out cached);
                var serviceCode = conn.Select<T_ServiceCode>(a => a.svcc_ID == id).FirstOrDefault();
                if (serviceCode?.svcc_OverrideID != null)
                {
                    var serviceCodeOverride = conn.Select<T_ServiceCode>(a => a.svcc_ID == serviceCode.svcc_OverrideID.Value).FirstOrDefault();
                    if (serviceCodeOverride != null) return serviceCodeOverride;
                }

                return serviceCode;
            }
            finally
            {
                dbCache.FreeConnection(cached, iConn);
            }



        }



    }
}
