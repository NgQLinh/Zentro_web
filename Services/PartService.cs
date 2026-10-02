using Microsoft.EntityFrameworkCore;
using Zentro.Data;
using Zentro.Models;

namespace Zentro.Services
{
    public class PartService
    {
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;

        public PartService(IDbContextFactory<ProductionDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public List<PartRecord> GetAll(string? search = null, string? maMay = null)
        {
            using var db = contextFactory.CreateDbContext();
            var query = db.Parts.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(item =>
                    item.MaIndex.Contains(search) ||
                    item.MaSp.Contains(search) ||
                    item.TenChiTiet.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(maMay))
            {
                query = query.Where(item => item.MaMay == null || item.MaMay == maMay);
            }

            return query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).ToList();
        }

        public PartRecord? Get(long id)
        {
            using var db = contextFactory.CreateDbContext();
            return db.Parts.AsNoTracking().FirstOrDefault(item => item.Id == id);
        }

        public PartRecord Create(PartRecord part)
        {
            using var db = contextFactory.CreateDbContext();
            part.CreatedAt = DateTime.Now;
            db.Parts.Add(part);
            db.SaveChanges();
            return part;
        }

        public void Update(PartRecord part)
        {
            using var db = contextFactory.CreateDbContext();
            var existing = db.Parts.First(item => item.Id == part.Id);
            existing.MaIndex = part.MaIndex;
            existing.MaSp = part.MaSp;
            existing.TenChiTiet = part.TenChiTiet;
            existing.MaMay = part.MaMay;
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = contextFactory.CreateDbContext();
            if (db.MachiningRecords.Any(item => item.PartId == id && item.TrangThai == "RUNNING"))
            {
                throw new InvalidOperationException("Không thể xóa Part đang được gia công.");
            }

            var existing = db.Parts.FirstOrDefault(item => item.Id == id);
            if (existing == null)
            {
                return;
            }

            db.Parts.Remove(existing);
            db.SaveChanges();
        }
    }
}
