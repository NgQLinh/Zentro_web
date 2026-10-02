using Microsoft.EntityFrameworkCore;
using Zentro.Data;
using Zentro.Models;

namespace Zentro.Services
{
    public class AuthenticationSettingsService
    {
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;

        public AuthenticationSettingsService(IDbContextFactory<ProductionDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public string GetPin(string role)
        {
            using var db = contextFactory.CreateDbContext();
            var settings = db.AuthenticationSettings.AsNoTracking().Single(item => item.Id == 1);
            return role == "Admin" ? settings.AdminPin : settings.UserPin;
        }

        public PinSettingsViewModel GetSettings()
        {
            return new PinSettingsViewModel();
        }

        public void Save(PinSettingsViewModel model)
        {
            using var db = contextFactory.CreateDbContext();
            var settings = db.AuthenticationSettings.Single(item => item.Id == 1);
            if (!string.IsNullOrWhiteSpace(model.AdminPin))
            {
                settings.AdminPin = model.AdminPin;
            }

            if (!string.IsNullOrWhiteSpace(model.UserPin))
            {
                settings.UserPin = model.UserPin;
            }

            db.SaveChanges();
        }
    }
}
