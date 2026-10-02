using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zentro.Services;

namespace Zentro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CamBienController : Controller
    {
        private readonly NhaMayDataService nhaMayData;

        public CamBienController(NhaMayDataService nhaMayData)
        {
            this.nhaMayData = nhaMayData;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Giám sát cảm biến";
            ViewData["Menu"] = "cambien";
            return View(nhaMayData.GetSensors());
        }
    }
}
