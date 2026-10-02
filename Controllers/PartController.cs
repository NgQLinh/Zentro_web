using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zentro.Models;
using Zentro.Services;

namespace Zentro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PartController : Controller
    {
        private readonly PartService partService;
        private readonly MachineService machineService;

        public PartController(PartService partService, MachineService machineService)
        {
            this.partService = partService;
            this.machineService = machineService;
        }

        public IActionResult Index(string? q = null)
        {
            ViewData["Title"] = "Mã sản phẩm / Index";
            ViewData["Menu"] = "parts";
            ViewBag.Search = q;
            return View(partService.GetAll(q));
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Thêm Part";
            ViewData["Menu"] = "parts";
            return View("Edit", new PartEditViewModel { Machines = machineService.GetAll(activeOnly: true) });
        }

        [HttpGet]
        public IActionResult Edit(long id)
        {
            var part = partService.Get(id);
            if (part == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Sửa Part";
            ViewData["Menu"] = "parts";
            return View(new PartEditViewModel
            {
                Id = part.Id,
                MaIndex = part.MaIndex,
                MaSp = part.MaSp,
                TenChiTiet = part.TenChiTiet,
                MaMay = part.MaMay,
                Machines = machineService.GetAll(activeOnly: true)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(PartEditViewModel model)
        {
            ViewData["Menu"] = "parts";
            model.Machines = machineService.GetAll(activeOnly: true);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var record = new PartRecord
            {
                Id = model.Id,
                MaIndex = model.MaIndex.Trim(),
                MaSp = model.MaSp.Trim(),
                TenChiTiet = model.TenChiTiet.Trim(),
                MaMay = string.IsNullOrWhiteSpace(model.MaMay) ? null : model.MaMay
            };

            if (model.Id == 0)
            {
                partService.Create(record);
            }
            else
            {
                partService.Update(record);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            try
            {
                partService.Delete(id);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
