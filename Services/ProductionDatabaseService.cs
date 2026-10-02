using Microsoft.EntityFrameworkCore;
using Zentro.Data;
using Zentro.Models;

namespace Zentro.Services
{
    public class ProductionDatabaseService
    {
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;

        public ProductionDatabaseService(IDbContextFactory<ProductionDbContext> contextFactory, BangTaiModel heThong, BangTaiLine2Model heThong2)
        {
            this.contextFactory = contextFactory;
            heThong.EventAdded = eventItem => SaveEvent(eventItem, "BT01");
            heThong2.EventAdded = eventItem => SaveEvent(eventItem, "BT02");
        }

        public async Task SaveProductionAsync(ProductionRecord record)
        {
            await using var db = await contextFactory.CreateDbContextAsync();
            db.ProductionRecords.Add(record);
            await db.SaveChangesAsync();
        }

        public void SaveAlarm(AlarmRecord record)
        {
            using var db = contextFactory.CreateDbContext();
            db.AlarmRecords.Add(record);
            db.SaveChanges();
        }

        public void SaveEvent(EventRecord record)
        {
            using var db = contextFactory.CreateDbContext();
            db.EventRecords.Add(record);
            db.SaveChanges();
        }

        private void SaveEvent(EventLogItem eventItem, string deviceCode)
        {
            SaveEvent(new EventRecord
            {
                NgayGio = eventItem.Timestamp,
                NoiDung = eventItem.Message,
                LoaiSuKien = "SYSTEM",
                ThietBi = deviceCode
            });
        }

        public List<ProductionRecord> GetProduction(DateTime from, DateTime to)
        {
            using var db = contextFactory.CreateDbContext();
            return db.ProductionRecords
                .AsNoTracking()
                .Where(item => item.NgayGio >= from && item.NgayGio < to)
                .OrderByDescending(item => item.NgayGio)
                .ToList();
        }

        public List<AlarmRecord> GetAlarms(DateTime from, DateTime to)
        {
            using var db = contextFactory.CreateDbContext();
            return db.AlarmRecords
                .AsNoTracking()
                .Where(item => item.NgayGio >= from && item.NgayGio < to)
                .OrderByDescending(item => item.NgayGio)
                .ToList();
        }

            public ProductionLotRecord? GetLot(string lot)
            {
                using var db = contextFactory.CreateDbContext();
                return db.ProductionLots.AsNoTracking().FirstOrDefault(item => item.Lot == lot);
            }

            public List<EventLogItem> GetEvents(int limit = 100)
            {
                using var db = contextFactory.CreateDbContext();
                return db.EventRecords
                .AsNoTracking()
                .OrderByDescending(item => item.NgayGio)
                .Take(limit)
                .Select(item => new EventLogItem { Timestamp = item.NgayGio, Message = item.NoiDung, Device = item.ThietBi ?? string.Empty, EventType = item.LoaiSuKien ?? string.Empty })
                .ToList();
            }

        public (List<EventLogItem> Items, int TotalItems) GetEvents(DateTime? fromDate, DateTime? toDate, int? month, int? year, int page, int pageSize)
        {
            using var db = contextFactory.CreateDbContext();
            var query = db.EventRecords.AsNoTracking().AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(item => item.NgayGio >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                query = query.Where(item => item.NgayGio < toDate.Value.Date.AddDays(1));
            }
            if (month.HasValue)
            {
                query = query.Where(item => item.NgayGio.Month == month.Value);
            }
            if (year.HasValue)
            {
                query = query.Where(item => item.NgayGio.Year == year.Value);
            }

            var totalItems = query.Count();
            var items = query.OrderByDescending(item => item.NgayGio)
                .ThenByDescending(item => item.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new EventLogItem
                {
                    Timestamp = item.NgayGio,
                    Message = item.NoiDung,
                    Device = item.ThietBi ?? string.Empty,
                    EventType = item.LoaiSuKien ?? string.Empty
                })
                .ToList();

            return (items, totalItems);
        }
    }
}
