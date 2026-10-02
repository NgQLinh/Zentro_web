using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zentro.Data;
using Zentro.Models;

namespace Zentro.Services
{
    public class UserAccountService
    {
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;
        private readonly PasswordHasher<UserRecord> passwordHasher = new();

        public UserAccountService(IDbContextFactory<ProductionDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public List<UserRecord> GetAll()
        {
            using var db = contextFactory.CreateDbContext();
            return db.Users.AsNoTracking().OrderBy(item => item.Username).ToList();
        }

        public UserRecord? GetById(long id)
        {
            using var db = contextFactory.CreateDbContext();
            return db.Users.AsNoTracking().FirstOrDefault(item => item.Id == id);
        }

        public UserRecord? GetByUsername(string username)
        {
            using var db = contextFactory.CreateDbContext();
            return db.Users.AsNoTracking().FirstOrDefault(item => item.Username == username);
        }

        public UserRecord? ValidateCredentials(string username, string password)
        {
            var user = GetByUsername(username);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return result == PasswordVerificationResult.Failed ? null : user;
        }

        public List<string> GetAssignedMachines(long userId)
        {
            using var db = contextFactory.CreateDbContext();
            return db.UserMachineAssignments.AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => item.MaMay)
                .OrderBy(item => item)
                .ToList();
        }

        public void Create(string username, string password, string role, IEnumerable<string> machineCodes)
        {
            using var db = contextFactory.CreateDbContext();
            if (db.Users.Any(item => item.Username == username))
            {
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");
            }

            var user = new UserRecord
            {
                Username = username,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            db.Users.Add(user);
            db.SaveChanges();
            SetAssignments(db, user.Id, machineCodes);
            db.SaveChanges();
        }

        public void Update(long id, string role, bool isActive, string? newPassword, IEnumerable<string> machineCodes)
        {
            using var db = contextFactory.CreateDbContext();
            var user = db.Users.First(item => item.Id == id);
            user.Role = role;
            user.IsActive = isActive;
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
            }

            SetAssignments(db, id, machineCodes);
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = contextFactory.CreateDbContext();
            if (db.MachiningRecords.Any(item => item.UserId == id))
            {
                throw new InvalidOperationException("Không thể xóa tài khoản đã có lịch sử gia công. Hãy khóa tài khoản thay thế.");
            }

            var user = db.Users.FirstOrDefault(item => item.Id == id);
            if (user == null)
            {
                return;
            }

            db.UserMachineAssignments.RemoveRange(db.UserMachineAssignments.Where(item => item.UserId == id));
            db.Users.Remove(user);
            db.SaveChanges();
        }

        private static void SetAssignments(ProductionDbContext db, long userId, IEnumerable<string> machineCodes)
        {
            var existing = db.UserMachineAssignments.Where(item => item.UserId == userId).ToList();
            db.UserMachineAssignments.RemoveRange(existing);
            foreach (var maMay in machineCodes.Distinct())
            {
                db.UserMachineAssignments.Add(new UserMachineAssignmentRecord { UserId = userId, MaMay = maMay });
            }
        }
    }
}
