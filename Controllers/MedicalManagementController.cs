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

namespace Itsomax.Module.FarmSystemManagement.Controllers
{
    
    [Authorize(Policy = "ManageAuthentification")]
    public class MedicalManagementController : Controller
    {
        
        private readonly IManageFarmInterface _farm;
        private readonly IToastNotification _toastNotification;
        private readonly UserManager<User> _userManager;

        public MedicalManagementController(IManageFarmInterface farm,IToastNotification toastNotification,
            UserManager<User> userManager)
        {
            _farm = farm;
            _toastNotification = toastNotification;
            _userManager = userManager;
        }
        
        public IActionResult SelectCostCenter()
        {
            ViewBag.LocationList = _farm.GetCostCenterMealList();
            return View();
        }

        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult SelectCostCenter(ConsumptionSelectActivity model)
        {
            if (model.CostCenterId == 0)
            {
                _toastNotification.AddErrorToastMessage("Need to select a Cost Center", new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                ViewBag.LocationList = _farm.GetCostCenterList();
                return View();
            }
            var costCenter = _farm.GetCostCenterById(model.CostCenterId);
            var prodlist = _farm.GetProductList(model.CostCenterId).ToList();
            return RedirectToAction(nameof(AddMedical),new {id=costCenter.Id});
        }
        
        public IActionResult AddMedical(long id)
        {
            var costCenter = _farm.GetCostCenterById(id);
            var prodList = _farm.GetProductList(id).ToList();
            var consumptionList = new ConsumptionViewModel
            {
                CostCenterName = costCenter.Name,
                CostCenterId = costCenter.Id,
                ProductLists = prodList
            };
            
            return View(consumptionList);
        }

        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult AddMedical(ConsumptionViewModel model, IFormCollection form)
        {
            string[] products = form["key"].ToArray();
            string[] values = form["value"].ToArray();

            var farm = _farm.SaveConsumption(model.CostCenterId, products, values, GetCurrentUserAsync().Result.UserName).Result;
            if (farm.Succeeded)
            {
                _toastNotification.AddSuccessToastMessage(farm.ToasterMessage, new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return RedirectToAction(nameof(SelectCostCenter));
            }
            else
            {
                _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return View(nameof(AddMedical),model);
            }
        }

        //Help Region
        private Task<User> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }
    }
}