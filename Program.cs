using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Zentro.Data;
using Zentro.Hubs;
using Zentro.Models;
using Zentro.Services;

namespace Zentro
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            // Tránh lỗi port đã bị chiếm (thường do instance cũ chưa tắt)
            builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://127.0.0.1:5180");

            // ===============================
            // ĐĂNG KÝ MVC
            // ===============================
            builder.Services.AddControllersWithViews();
            builder.Services.AddSignalR();
            var connectionString = builder.Configuration.GetConnectionString("Production")
                ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:Production.");
            var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString)
            {
                Database = "webnoibo",
                AllowPublicKeyRetrieval = true
            };
            builder.Services.AddPooledDbContextFactory<ProductionDbContext>(options =>
                options.UseMySql(connectionStringBuilder.ConnectionString, new MySqlServerVersion(new Version(8, 0, 0))));
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.Name = "Zentro.PendingLogin";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.IdleTimeout = TimeSpan.FromMinutes(5);
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.Cookie.Name = "Zentro.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                });

            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // ===============================
            // ĐĂNG KÝ MODEL
            // ===============================
            builder.Services.AddSingleton<BangTaiModel>();
            builder.Services.AddSingleton<BangTaiLine2Model>();

            // ===============================
            // ĐĂNG KÝ SERVICES
            // ===============================
            builder.Services.AddSingleton<BangTaiServices>();
            builder.Services.AddSingleton<BangTaiLine2Services>();
            builder.Services.AddSingleton<CamBienServices>();
            builder.Services.AddSingleton<PhanLoaiServices>();
            builder.Services.AddSingleton<PhanLoaiLine2Services>();
            builder.Services.AddSingleton<NhaMayDataService>();
            builder.Services.AddSingleton<ProductionDatabaseService>();
            builder.Services.AddSingleton<LoginProtectionService>();
            builder.Services.AddSingleton<DatabaseBootstrapService>();
            builder.Services.AddSingleton<MachineService>();
            builder.Services.AddSingleton<PartService>();
            builder.Services.AddSingleton<MachiningService>();
            builder.Services.AddSingleton<UserAccountService>();
            builder.Services.AddSingleton<UserActionLogService>();
            builder.Services.AddSingleton<AuthenticationSettingsService>();
            builder.Services.AddSingleton<PlcRuntimeState>();
            builder.Services.Configure<PlcOptions>(builder.Configuration.GetSection("Plc"));
            builder.Services.AddHostedService<PlcModbusService>();
            builder.Services.AddScoped<DashboardService>();

            var app = builder.Build();

            app.Services.GetRequiredService<DatabaseBootstrapService>().EnsureSchemaAndSeed();

            app.Lifetime.ApplicationStarted.Register(() =>
            {
                using var scope = app.Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<BangTaiServices>().StartIfConfigured();
                scope.ServiceProvider.GetRequiredService<BangTaiLine2Services>().StartIfConfigured();
            });

            // ===============================
            // CẤU HÌNH XỬ LÝ LỖI
            // ===============================
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // ===============================
            // STATIC FILES
            // CSS, JavaScript, hình ảnh...
            // ===============================
            app.UseStaticFiles();

            // ===============================
            // ROUTING
            // ===============================
            app.UseRouting();
            app.UseSession();

            // ===============================
            // AUTHORIZATION
            // ===============================
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<DashboardHub>("/hubs/dashboard");

            // ===============================
            // ROUTE MẶC ĐỊNH
            // ===============================
            app.MapControllerRoute(
                name: "conveyorControl",
                pattern: "BangTai/DieuKhien",
                defaults: new { controller = "BangTai", action = "DieuKhien" });

            app.MapControllerRoute(
                name: "sorting",
                pattern: "BangTai/PhanLoaiPage",
                defaults: new { controller = "BangTai", action = "DieuKhien", page = "PhanLoai" });

            app.MapControllerRoute(
                name: "classification",
                pattern: "BangTai/PhanLoai",
                defaults: new { controller = "BangTai", action = "Start", returnPage = "PhanLoai" });

            app.MapControllerRoute(
                name: "moveBack",
                pattern: "BangTai/Lui",
                defaults: new { controller = "BangTai", action = "Start", returnPage = "Index", command = "LUI" });

            app.MapControllerRoute(
                name: "moveForward",
                pattern: "BangTai/Tien",
                defaults: new { controller = "BangTai", action = "Start", returnPage = "Index", command = "TIEN" });

            app.MapControllerRoute(
                name: "modeAuto",
                pattern: "BangTai/ModeAuto",
                defaults: new { controller = "BangTai", action = "Start", command = "AUTO_LINE" });

            app.MapControllerRoute(
                name: "modeManual",
                pattern: "BangTai/ModeManual",
                defaults: new { controller = "BangTai", action = "Start", command = "MANUAL_LINE" });

            app.MapControllerRoute(
                name: "startLine",
                pattern: "BangTai/StartLine",
                defaults: new { controller = "BangTai", action = "Start", command = "START_LINE" });

            app.MapControllerRoute(
                name: "stopLine",
                pattern: "BangTai/StopLine",
                defaults: new { controller = "BangTai", action = "Start", command = "STOP_LINE" });

            app.MapControllerRoute(
                name: "resetLine",
                pattern: "BangTai/ResetLine",
                defaults: new { controller = "BangTai", action = "Start", command = "RESET_LINE" });

            app.MapControllerRoute(
                name: "emergencyLine",
                pattern: "BangTai/EmergencyLine",
                defaults: new { controller = "BangTai", action = "Start", command = "E_STOP_LINE" });

            app.MapControllerRoute(
                name: "faultLine",
                pattern: "BangTai/FaultLine",
                defaults: new { controller = "BangTai", action = "Start", command = "FAULT_LINE" });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );

            // ===============================
            // CHẠY ỨNG DỤNG
            // ===============================
            app.Run();
        }
    }
}
