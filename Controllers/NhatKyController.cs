using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zentro.Models;
using Zentro.Services;

namespace Zentro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class NhatKyController : Controller
    {
        private readonly BangTaiModel heThong;
        private readonly ProductionDatabaseService database;
        private readonly UserActionLogService actionLogService;

        public NhatKyController(BangTaiModel heThong, ProductionDatabaseService database, UserActionLogService actionLogService)
        {
            this.heThong = heThong;
            this.database = database;
            this.actionLogService = actionLogService;
        }

        public IActionResult Index(DateTime? fromDate, DateTime? toDate, int? month, int? year, string? username = null, string? action = null, string? maMay = null, int page = 1)
        {
            ViewData["Title"] = "Nhật ký vận hành";
            ViewData["Menu"] = "nhatky";
            page = Math.Max(1, page);
            const int pageSize = 20;
            var requestedPage = page;
            var result = database.GetEvents(fromDate, toDate, month, year, page, pageSize);
            var totalPages = Math.Max(1, (int)Math.Ceiling(result.TotalItems / (double)pageSize));
            page = Math.Min(page, totalPages);
            if (page != requestedPage)
            {
                result = database.GetEvents(fromDate, toDate, month, year, page, pageSize);
            }
            var items = result.Items;
            var totalItems = result.TotalItems;

            if (totalItems == 0 && !fromDate.HasValue && !toDate.HasValue && !month.HasValue && !year.HasValue)
            {
                items = heThong.EventLogs
                    .OrderByDescending(item => item.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                totalItems = heThong.EventLogs.Count;
            }

            return View(new OperationLogViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Month = month,
                Year = year,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                Items = items
                ,ActionItems = actionLogService.Get(username, action, maMay, fromDate, toDate)
            });
        }
    }
}
