using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itsomax.Module.Core.Interfaces;
using Itsomax.Module.Core.Models;
using Itsomax.Module.FarmSystemCore.Interfaces;
using Itsomax.Module.FarmSystemCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NToastNotify;
//using NToastNotify.Libraries;

namespace Itsomax.Module.FarmSystemManagement.Controllers
{
    [Authorize(Policy = "ManageAuthentification")]
    public class FarmManagementController : Controller
    {
        private readonly IManageFarmInterface _farm;
        private readonly IToastNotification _toastNotification;
        private readonly UserManager<User> _userManager;
        private readonly IManageExcelFile _excel;

        public FarmManagementController(IManageFarmInterface farm,IToastNotification toastNotification, UserManager<User> userManager,
            IManageExcelFile excel)
        {
            _farm = farm;
            _toastNotification = toastNotification;
            _userManager = userManager;
            _excel = excel;
        }

        public IActionResult AddBaseUnit()
        {
            return View();
        }

        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult AddBaseUnit(BaseUnitViewModel model)
        {
            var farm = _farm.AddBaseUnit(model,GetCurrentUserAsync().Result.UserName).Result;
            if (farm.Succeeded)
            {
                _toastNotification.AddSuccessToastMessage(farm.ToasterMessage, new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return RedirectToAction(nameof(ListBaseUnit));
            }
            else
            {
                _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return View(nameof(AddBaseUnit),model);
            }
        }

        public IActionResult AddLocation()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddLocation(LocationViewModel model)
        {
            var farm = _farm.AddLocation(model,GetCurrentUserAsync().Result.UserName).Result;
            if (farm.Succeeded)
            {
                _toastNotification.AddSuccessToastMessage(farm.ToasterMessage, new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return RedirectToAction(nameof(ListLocation));
            }
            else
            {
                _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return View(nameof(AddLocation),model);
            }
        }

        [Route("/get/location/{id}")]
        public IActionResult EditLocation(long id)
        {
            var location = _farm.GetLocationById(id);
            var editLocation = new LocationEditViewModel
            {
                Id = location.Id,
                Name = location.Name,
                Active = location.Active,
                Code = location.Code,
                Description = location.Description,
            };
            ViewBag.Code = location.Code;
            return View(editLocation);
        }

        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult EditLocationPost(LocationEditViewModel model)
        {
            var farm = _farm.EditLocation(model, GetCurrentUserAsync().Result.UserName).Result;
            if (ModelState.IsValid)
            {
                if (farm.Succeeded)
                {
                    _toastNotification.AddSuccessToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    return RedirectToAction(nameof(ListLocation));
                }
                else
                {
                    _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    return View(nameof(EditLocation),model);
                }
            }
            else
            {
                _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return View(nameof(EditLocation),model);
            }
            
        }
        public IActionResult AddCostCenter()
        {          
            ViewBag.LocationList = _farm.GetLocationList(null);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCostCenter(CostCenterViewModel model)
        {
            
            if (model.LocationId == 0)
            {
                var locationList = _farm.GetLocationList(null);
                ViewBag.LocationList = locationList;
                
                _toastNotification.AddErrorToastMessage("Need to select a Location", new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return View(model);
            }
            
            var farm = _farm.AddCostCenter(model,GetCurrentUserAsync().Result.UserName).Result;
            if (ModelState.IsValid)
            {
                if (farm.Succeeded)
                {
                    _toastNotification.AddSuccessToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    return RedirectToAction(nameof(ListCostCenter));
                }
                else
                {
                    _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    var locationList = _farm.GetLocationList(null);
                    ViewBag.LocationList = locationList;
                    return View(nameof(AddCostCenter),model);
                }
            }
            else
            {
                var locationList = _farm.GetLocationList(null);
                ViewBag.LocationList = locationList;
                return View(nameof(AddCostCenter), model);
            }
            
        }

        [Route("/get/cost-center/{id}")]
        public IActionResult EditCostCenter(long id)
        {
            var costCenter = _farm.GetCostCenterById(id);
            var editCostCenter = new CostCenterEditViewModel
            {
                Active = costCenter.Active,
                Code = costCenter.Code,
                CreatedOn = costCenter.CreatedOn,
                Description = costCenter.Description,
                Id = costCenter.Id,
                LocationId = costCenter.LocationId,
                Name = costCenter.Name,
                WarehouseCode = costCenter.WarehouseCode,
                IsFarming =  costCenter.IsFarming,
                IsMeal = costCenter.IsMeal,
                IsMedical = costCenter.IsMedical
            };
            ViewBag.LocationList = _farm.GetLocationList(costCenter.Id);
            ViewBag.Code = costCenter.Code;
            return View(editCostCenter);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult EditCostCenterPost(CostCenterEditViewModel model)
        {
            var farm = _farm.EditCostCenter(model, GetCurrentUserAsync().Result.UserName).Result;
            if (ModelState.IsValid)
            {
                if (farm.Succeeded)
                {
                    _toastNotification.AddSuccessToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    return RedirectToAction(nameof(ListCostCenter));
                }
                else
                {
                    _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    ViewBag.LocationList = _farm.GetLocationList(model.Id);
                    ViewBag.Code = model.Code;
                    return View(nameof(EditCostCenter),model);
                }
            }
            else
            {
                _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                ViewBag.LocationList = _farm.GetLocationList(model.Id);
                ViewBag.Code = model.Code;
                return View(nameof(EditCostCenter),model);
            }
        }

        public IActionResult AddProductsToCostCenter()
        {
            var products = new ProductCostCenterViewModel();
            var list = (List<SelectListItem>) _farm.GetProducts().Select(x => new SelectListItem()
            {
                Value = x.Name,
                Text = x.Name

            });
            list.Add(new SelectListItem() {Text = "Select a Product", Value = string.Empty,Selected = true});
            products.Products = list;
            return View(products);
        }
        


        public IActionResult AddProduct()
        {
            ViewBag.BaseUnitList = _farm.GetBaseUnitList(null);
            return View();
        }

        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult AddProduct(ProductViewModel model)
        {
            if (model.BaseUnitId == 0)
            {
                var baseUnitList = _farm.GetBaseUnitList(null);
                ViewBag.BaseUnitList = baseUnitList;
                
                _toastNotification.AddErrorToastMessage("Need to select a Base Unit", new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return View(model);
            }
            
            if (ModelState.IsValid)
            {
                var farm = _farm.AddProduct(model,GetCurrentUserAsync().Result.UserName).Result;
                if (farm.Succeeded)
                {
                    _toastNotification.AddSuccessToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    return RedirectToAction(nameof(ListProduct));
                }
                else
                {
                    _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    var baseUnitList = _farm.GetBaseUnitList(null);
                    ViewBag.BaseUnitList = baseUnitList;
                    return View(nameof(AddProduct),model);
                }
            }
            else
            {
                var baseUnitList = _farm.GetBaseUnitList(null);
                ViewBag.BaseUnitList = baseUnitList;
                return View(nameof(AddProduct),model);
            }
            
        }
        [Route("/get/product/{id}")]
        public IActionResult EditProduct(long id)
        {
            var product = _farm.GetProductById(id);
            ViewBag.Code = product.Code;
            ViewBag.BaseUnitList = _farm.GetBaseUnitList(product.Id);
            var model = new ProductEditViewModel
            {
                Id = product.Id,
                Name = product.Name,
                BaseUnitId = product.BaseUnitId,
                Active = product.Active,
                Code = product.Code,
                Description = product.Description
            };
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditProductPost(ProductEditViewModel model)
        {
            var farm = _farm.EditProduct(model, GetCurrentUserAsync().Result.UserName).Result;
            if (ModelState.IsValid)
            {
                if (farm.Succeeded)
                {
                    _toastNotification.AddSuccessToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    return RedirectToAction(nameof(ListProduct));
                }
                else
                {
                    _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    var baseUnitList = _farm.GetBaseUnitList(null);
                    ViewBag.BaseUnitList = baseUnitList;
                    return View(nameof(EditProduct),model);
                }
            }
            else
            {
                _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                var baseUnitList = _farm.GetBaseUnitList(null);
                ViewBag.BaseUnitList = baseUnitList;
                return View(nameof(EditProduct),model);
            }
        }

        [Route("/get/costcenterproducts/{id}")]
        public IActionResult AddProductsToCostCenter(long? id)
        {
            if (id == null)
            {
                _toastNotification.AddErrorToastMessage("Need to select a Base Unit", new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return View(nameof(ListCostCenter));
            }

            var costCenter = _farm.GetCostCenterById(id.Value);

            var model = new ProductCostCenterViewModel
            {
                CostCenterId = costCenter.Id,
                Name = _farm.GetCostCenterProductName(costCenter.Id),
                Active = _farm.GetCostCenterProductActive(costCenter.Id)
            };

            model.Products = _farm.GetSelectListProducts(costCenter.Id);
            ViewBag.Name = costCenter.Name;
            ViewBag.Code = costCenter.Code;
            return View(model);
        }

        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult AddProductsToCostCenterPost(ProductCostCenterViewModel model, params string[] selectedProducts)
        {
            if (selectedProducts.Count() == 0)
            {
                _toastNotification.AddWarningToastMessage("Need to select a product", new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                var costCenter2 = _farm.GetCostCenterById(model.CostCenterId);
                model.Products = _farm.GetSelectListProducts(costCenter2.Id);
                ViewBag.Name = costCenter2.Name;
                ViewBag.Code = costCenter2.Code;
                
                return View(nameof(AddProductsToCostCenter),model);
            }
            
            var farm = _farm.AddProductsToCostCenter(model,GetCurrentUserAsync().Result.UserName,selectedProducts).Result;
            if (farm.Succeeded)
            {
                _toastNotification.AddSuccessToastMessage(farm.ToasterMessage, new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return RedirectToAction(nameof(ListCostCenter));
            }

            _toastNotification.AddWarningToastMessage(farm.ToasterMessage, new ToastrOptions()
            {
                PositionClass = ToastPositions.TopCenter
            });
            var costCenter = _farm.GetCostCenterById(model.CostCenterId);
            model.Products = _farm.GetSelectListProducts(costCenter.Id);
            ViewBag.Name = costCenter.Name;
            ViewBag.Code = costCenter.Code;
            return View(nameof(AddProductsToCostCenter),model);
        }

        /*
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AddProduct(FormCollection form)
        {
            var products = form["key"].ToArray();
            var values = form["values"].ToArray();

            return View();
        }
        */
        public IActionResult GetConsumptionReport()
        {
            var list = _farm.GetWarehouseListNames();
            
            ViewBag.WarehouseList = list;
			ViewBag.Url = "";
            return View();
        }
		[HttpPost,ValidateAntiForgeryToken]
        public IActionResult GetConsumptionReport(GenerateConsumptionReportViewModel model)
        {
            if (model.WarehouseName == "Select a Warehouse")
            {
                _toastNotification.AddInfoToastMessage("You did not select a warehouse", new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                var list2 = _farm.GetWarehouseListNames();
                ViewBag.WarehouseList = list2;
                ViewBag.Url = "";
                return View();
            }
			var report = _farm.ConsumptionReport(model.ConsumptionDate,model.Folio,model.WarehouseName).ToList();
			if(!report.Any())
			{
				_toastNotification.AddInfoToastMessage("There is no consumption report for date "+model.ConsumptionDate.ToString("MM-dd-yyyy"), new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
			    var list2 = _farm.GetWarehouseListNames();
			    ViewBag.WarehouseList = list2;
			    ViewBag.Url = "";
				return View();
			}
			var excel = _excel.GenerateExcelName("SalidaSoftland"+model.WarehouseName,model.ConsumptionDate,model.WarehouseName);
			var excelPath = excel[0];
			var excelName = excel[1];
			if(excel[0] == null)
			{
				_toastNotification.AddInfoToastMessage("There is no consumption report for date " + model.ConsumptionDate.ToString("MM-dd-yyyy"), new ToastrOptions()
                {
                    PositionClass = ToastPositions.TopCenter
                });
                var list2 = _farm.GetWarehouseListNames();
                ViewBag.WarehouseList = list2;
                ViewBag.Url = "";
                return View();
			}
            //var file = new FileInfo(excel);

			using (var fs = new FileStream(excelPath,FileMode.Create,FileAccess.ReadWrite))
            {
                var workbook = new XSSFWorkbook();
                //var excelSheet = workbook.GetSheetAt(0);
                //var headerRow = excelSheet.GetRow(0);
                var cellCount = 9;
                var i = 1;
                ISheet excelSheet = workbook.CreateSheet("Entrada");
				var rowHeader = excelSheet.CreateRow(0);
				rowHeader.CreateCell(0).SetCellValue("Código Bodega");
				rowHeader.CreateCell(1).SetCellValue("Número de Folio Guía de Salida");
				rowHeader.CreateCell(2).SetCellValue("Fecha de generación Guía de Salida");
				rowHeader.CreateCell(3).SetCellValue("Concepto de Salida a Bodega");
				rowHeader.CreateCell(4).SetCellValue("Descripción");
				rowHeader.CreateCell(5).SetCellValue("Código Centro de Costo");
				rowHeader.CreateCell(6).SetCellValue("Código de Producto");
				rowHeader.CreateCell(7).SetCellValue("Código Unidad de Medida");
				rowHeader.CreateCell(8).SetCellValue("Cantidad Despachada");
                foreach (var item in report)
                {
                    var row = excelSheet.CreateRow(i);
                    for (var j = 0; j < cellCount; j++)
                    {
                        switch (j)
                        {
                            case 0:
                                row.CreateCell(j).SetCellValue(item.Warehouse);
                                break;
                            case 1:
                                row.CreateCell(j).SetCellValue(item.Folio);
                                break;
                            case 2:
                                row.CreateCell(j).SetCellValue("'" + item.GeneratedDate);
                                break;
                            case 3:
                                row.CreateCell(j).SetCellValue(item.WarehouseOut);
                                break;
                            case 4:
                                row.CreateCell(j).SetCellValue(item.Description);
                                break;
                            case 5:
                                row.CreateCell(j).SetCellValue(item.CenterCostCode);
                                break;
                            case 6:
                                row.CreateCell(j).SetCellValue(item.ProductCode);
                                break;
                            case 7:
                                row.CreateCell(j).SetCellValue(item.BaseUnit);
                                break;
                            case 8:
                                row.CreateCell(j).SetCellValue(item.Amount);
                                break;
                        }
                    }

                    i++;
                }
                workbook.Write(fs);
            }
            
            var list = _farm.GetWarehouseListNames();
            
            ViewBag.WarehouseList = list;
            ViewBag.Url = "/Temp/" + excelName;
			return View();
        }

        [HttpDelete]
        public IActionResult ActivateLocation(long? id)
        {
            if (id == null)
                return Json(false);
            return  Json(_farm.EnableDisableLocation(id.Value, GetCurrentUserAsync().Result.UserName));
        }
        [HttpDelete]
        public IActionResult ActivateCostCenter(long? id)
        {
            if (id == null)
                return Json(false);
            return  Json(_farm.EnableDisableCostCenter(id.Value, GetCurrentUserAsync().Result.UserName));
        }
        [HttpDelete]
        public IActionResult ActivateProduct(long? id)
        {
            if (id == null)
                return Json(false);
            return  Json(_farm.EnableDisableProduct(id.Value, GetCurrentUserAsync().Result.UserName));
        }
        [HttpDelete]
        public IActionResult ActivateBaseUnit(long? id)
        {
            if (id == null)
                return Json(false);
            return  Json(_farm.EnableDisableBaseUnit(id.Value, GetCurrentUserAsync().Result.UserName));
        }
        
        public IActionResult ListLocation()
        {
            return View();
        }

        [Route("/get/location/json/")]
        public JsonResult ListLocationJson()
        {
            return Json(from a in _farm.GetLocation().ToList()
                select new
                {
                    a.Id,
                    a.Code,
                    a.Name,
                    a.Active,
                    a.UpdatedOn
                });
        }

        public IActionResult ListProduct()
        {
            return View();
        }

        [Route("/get/products/json")]
        public JsonResult ListProductsJson()
        {
            return Json(from a in _farm.GetProducts().ToList()
                select new
                {
                    a.Id,
                    a.Code,
                    a.Name,
                    a.Active,
                    a.UpdatedOn
                });
        }

        public IActionResult ListCostCenter()
        {
            return View();
        }

        [Route("/get/cost-center/json")]
        public JsonResult ListCostCenterJson()
        {
            return Json(from a in _farm.GetCostCenters().ToList()
                select new
                {
                    a.Id,
                    a.Code,
                    a.Name,
                    a.Active,
                    a.UpdatedOn,
                    a.IsFarming,
                    a.IsMedical,
                    a.IsMeal
                });

        }
        public IActionResult ListBaseUnit()
        {
            return View();
        }

        [Route("/get/baseunit/json")]
        public JsonResult ListBaseUnitJson()
        {
            return Json(from a in _farm.GetBaseUnitList().ToList()
                select new
                {
                    a.Id,
                    a.Value,
                    a.Name,
                    a.Active,
                    a.UpdatedOn
                });

        }

        public IActionResult LoadInitial()
        {
            _farm.LoadInitialDataFarm();
            return Ok();
        }

        //#Helper Region
        private Task<User> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

    }
}