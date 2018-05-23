﻿using System;
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
    public class FarmSpecialManagementController : Controller
    {
        
        private readonly IManageFarmInterface _farm;
        private readonly IToastNotification _toastNotification;
        private readonly UserManager<User> _userManager;

        public FarmSpecialManagementController(IManageFarmInterface farm,IToastNotification toastNotification, UserManager<User> userManager)
        {
            _farm = farm;
            _toastNotification = toastNotification;
            _userManager = userManager;
        }
        
        public IActionResult AddSpecialMeal(long id)
        {
            var costCenter = _farm.GetCostCenterById(id);
            var prodList = _farm.GetProductList(id).ToList();
            var consumptionList = new ConsumptionViewModel
            {
                CostCenterName = costCenter.Name,
                CostCenterId = costCenter.Id,
                ProductLists = prodList,
                LateDateTime = DateTimeOffset.Now
            };
            
            return View(consumptionList);
        }
        
        
        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult AddSpecialMeal(ConsumptionViewModel model, IFormCollection form)
        {
            string[] products = form["key"].ToArray();
            string[] values = form["value"].ToArray();

            var farm = _farm.SaveConsumption(model.CostCenterId, products, values, GetCurrentUserAsync().Result.UserName,model.LateDateTime).Result;
            if (farm.Succeeded)
            {
                _toastNotification.AddSuccessToastMessage(farm.ToasterMessage, new ToastrOptions
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return RedirectToAction(nameof(SelectMealCostCenter));
            }
            else
            {
                _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return View(nameof(AddSpecialMeal),model);
            }
        }

        public IActionResult SelectMealCostCenter()
        {
            ViewBag.LocationList = _farm.GetCostCenterMealList();
            return View();
        }

        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult SelectMealCostCenter(ConsumptionSelectActivity model)
        {
            if (model.CostCenterId == 0)
            {
                _toastNotification.AddErrorToastMessage("Need to select a Cost Center", new ToastrOptions
                {
                    PositionClass = ToastPositions.TopCenter
                });
                ViewBag.LocationList = _farm.GetCostCenterList();
                return View();
            }
            var costCenter = _farm.GetCostCenterById(model.CostCenterId);
            //var prodlist = _farm.GetProductList(model.CostCenterId).ToList();

            return RedirectToAction(nameof(AddSpecialMeal),new {id=costCenter.Id});
        }

        [Route("/get/consumption/json")]
        public JsonResult GetConsumptionJson()
        {
            return Json(_farm.GetConsumptionList());
        }
        
        public IActionResult ListConsumption()
        {
            return View();
        }

        public IActionResult EditConsumption(int? id)
        {
            if (id == null)
            {
                _toastNotification.AddWarningToastMessage("Consumption not Found", new ToastrOptions
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return View(nameof(ListConsumption));
            }

            var consumption = _farm.GetConsumptionById(id.Value);
            if (consumption.CreatedOn <= DateTimeOffset.Now.AddDays(-1))
            {
                _toastNotification.AddWarningToastMessage("Id not valid", new ToastrOptions
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return View(nameof(ListConsumption));
            }
            
            var prodList = _farm.GetProductListEdit(id.Value).ToList();
            var consumptionList = new ConsumptionEditViewModel
            {
                CostCenterName = prodList.FirstOrDefault().CenterCostName,
                ConsumptionId = consumption.Id,
                ProductListEdit = prodList
            };
            
            return View(consumptionList);
        }

        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult EditConsumption(ConsumptionEditViewModel model,IFormCollection form)
        {
            if (ModelState.IsValid)
            {
                string[] products = form["key"].ToArray();
                string[] values = form["value"].ToArray();

                var farm = _farm.SaveConsumptionEdit(model.ConsumptionId, products, values, GetCurrentUserAsync().Result.UserName).Result;
                if (farm.Succeeded)
                {
                    _toastNotification.AddSuccessToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    return RedirectToAction(nameof(ListConsumption));
                }
                else
                {
                    _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    return View(model);
                }
            }
            return View(model);
        }
        
        //#help region 
        private Task<User> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }
    }
}