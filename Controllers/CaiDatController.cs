using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zentro.Models;
using Zentro.Services;

namespace Zentro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CaiDatController : Controller
    {
        private readonly NhaMayDataService nhaMayData;
        private readonly BangTaiModel heThong;

        public CaiDatController(NhaMayDataService nhaMayData, BangTaiModel heThong)
        {
            this.nhaMayData = nhaMayData;
            this.heThong = heThong;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Cài đặt hệ thống";
            ViewData["Menu"] = "caidat";
            ViewData["Config2"] = nhaMayData.GetConfig("BT02");
            return View(nhaMayData.GetConfig());
        }

        [HttpPost]
        public IActionResult Index(SystemConfig config)
        {
            nhaMayData.SaveConfig(config);
            heThong.BufferMax = BangTaiModel.DefaultBufferMax;
            ViewData["Title"] = "Cài đặt hệ thống";
            ViewData["Menu"] = "caidat";
            ViewData["Saved"] = true;
            ViewData["Config2"] = nhaMayData.GetConfig("BT02");
            return View(nhaMayData.GetConfig());
        }

        [HttpPost]
        public IActionResult SaveConveyor2(SystemConfig config)
        {
            nhaMayData.SaveConfig(config, "BT02");
            ViewData["Title"] = "Cài đặt hệ thống";
            ViewData["Menu"] = "caidat";
            ViewData["Saved2"] = true;
            ViewData["Config2"] = nhaMayData.GetConfig("BT02");
            return View("Index", nhaMayData.GetConfig());
        }
    }
}
