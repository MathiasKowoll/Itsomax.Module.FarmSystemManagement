using System.Linq;
using System.Threading.Tasks;
using Itsomax.Module.Core.Models;
using Itsomax.Module.FarmSystemCore.Interfaces;
using Itsomax.Module.FarmSystemCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
//using NToastNotify.Libraries;


namespace Itsomax.Module.FarmSystemManagement.Controllers
{
    [Authorize(Policy = "ManageAuthentification")]
    public class MedicalManagementController : Controller
    {

        private readonly IManageFarmInterface _farm;
        private readonly IToastNotification _toastNotification;
        private readonly UserManager<User> _userManager;

        public MedicalManagementController(IManageFarmInterface farm, IToastNotification toastNotification,
            UserManager<User> userManager)
        {
            _farm = farm;
            _toastNotification = toastNotification;
            _userManager = userManager;
        }

        public IActionResult AddMedical(long id)
        {
            var costCenter = _farm.GetCostCenterById(id);
            var prodList = _farm.GetProductList(id, "Medical").ToList();
            var consumptionList = new ConsumptionViewModel
            {
                CostCenterName = costCenter.Name,
                CostCenterId = costCenter.Id,
                ProductLists = prodList
            };
            var list = string.Empty;
            foreach (var item in prodList)
            {
                list = list +"<option>" + item.Name + "<option/>";
            }

            ViewBag.Complete = prodList;

            return View(consumptionList);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AddMedical(ConsumptionViewModel model, IFormCollection form)
        {
            string[] products = form["key"].ToArray();
            string[] values = form["value"].ToArray();


            var farm = _farm.SaveConsumption(model.CostCenterId, products, values,
                GetCurrentUserAsync().Result.UserName, null).Result;
            if (farm.Succeeded)
            {
                _toastNotification.AddSuccessToastMessage(farm.OkMessage, new ToastrOptions
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return RedirectToAction(nameof(SelectCostCenter));
            }

            _toastNotification.AddWarningToastMessage(farm.Errors, new ToastrOptions
            {
                PositionClass = ToastPositions.TopCenter
            });
            var newConsumption = new ConsumptionViewModel
            {
                CostCenterId = model.CostCenterId,
                CostCenterName = model.CostCenterName,
                ProductLists = _farm.GetProductListFailed(model.CostCenterId, "Medical", products, values).ToList()
            };
            return View(nameof(AddMedical), newConsumption);
        }

        public IActionResult SelectCostCenter()
        {
            ViewBag.LocationList = _farm.GetCostCenterMedicalList();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult SelectCostCenter(ConsumptionSelectActivity model)
        {
            if (model.CostCenterId == 0)
            {
                _toastNotification.AddWarningToastMessage("Need to select a Cost Center", new ToastrOptions
                {
                    PositionClass = ToastPositions.TopCenter
                });
                ViewBag.LocationList = _farm.GetCostCenterMedicalList();
                return View();
            }
            var costCenter = _farm.GetCostCenterById(model.CostCenterId);

            return RedirectToAction(nameof(AddMedical), new { id = costCenter.Id });
        }

        public JsonResult GetProducts(long id,string prefix)
        {
            var prodList = (from n in _farm.GetProductList(id, "Medical").ToList()
                    where n.Name.StartsWith(prefix)
                    select new {n.Name}
                );
            return Json(prodList);
        }

        //#help region 
        private Task<User> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }
    }
}
