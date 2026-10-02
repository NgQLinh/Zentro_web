using Microsoft.EntityFrameworkCore;
using Zentro.Data;
using Zentro.Models;

namespace Zentro.Services
{
    public class UserActionLogService
    {
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;

        public UserActionLogService(IDbContextFactory<ProductionDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public void Write(long? userId, string username, string role, string action, string? maMay, string? maIndex, string message, bool success, string? error = null)
        {
            using var db = contextFactory.CreateDbContext();
            db.UserActionLogs.Add(new UserActionLogRecord
            {
                UserId = userId,
                Username = username,
                Role = role,
                ThoiDiem = DateTime.Now,
                LoaiThaoTac = action,
                MaMay = maMay,
                MaIndex = maIndex,
                NoiDung = message,
                ThanhCong = success,
                Loi = error
            });
            db.SaveChanges();
        }

        public List<UserActionLogRecord> Get(string? username = null, string? action = null, string? maMay = null, DateTime? from = null, DateTime? to = null, int limit = 500)
        {
            using var db = contextFactory.CreateDbContext();
            var query = db.UserActionLogs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(username)) query = query.Where(item => item.Username == username);
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(item => item.LoaiThaoTac == action);
            if (!string.IsNullOrWhiteSpace(maMay)) query = query.Where(item => item.MaMay == maMay);
            if (from.HasValue) query = query.Where(item => item.ThoiDiem >= from.Value.Date);
            if (to.HasValue) query = query.Where(item => item.ThoiDiem < to.Value.Date.AddDays(1));
            return query.OrderByDescending(item => item.ThoiDiem).Take(limit).ToList();
        }
    }
}
