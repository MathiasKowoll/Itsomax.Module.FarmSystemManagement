using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itsomax.Module.Core.Interfaces;
using Itsomax.Module.Core.Models;
using Itsomax.Module.FarmSystemCore.Interfaces;
using Itsomax.Module.FarmSystemCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NToastNotify;

namespace Itsomax.Module.FarmSystemManagement.Controllers
{
    [Authorize(Policy = "ManageAuthentification")]
    public class FarmSpecialManagementController : Controller
    {
        
        private readonly IManageFarmInterface _farm;
        private readonly IToastNotification _toastNotification;
        private readonly UserManager<User> _userManager;
        private readonly IManageExcelFile _excel;

        public FarmSpecialManagementController(IManageFarmInterface farm,IToastNotification toastNotification, 
            UserManager<User> userManager, IManageExcelFile excel)
        {
            _farm = farm;
            _toastNotification = toastNotification;
            _userManager = userManager;
            _excel = excel;
        }
        
        public IActionResult AddSpecialMeal(long id)
        {
            var costCenter = _farm.GetCostCenterById(id);
            var prodList = _farm.GetProductList(id,"Meal").ToList();
            var consumptionList = new ConsumptionViewModel
            {
                CostCenterName = costCenter.Name,
                CostCenterId = costCenter.Id,
                ProductLists = prodList
            };
            
            return View(consumptionList);
        }
        
        
        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult AddSpecialMeal(ConsumptionViewModel model, IFormCollection form)
        {
            string[] products = form["key"].ToArray();
            string[] values = form["value"].ToArray();

            var farm = _farm.SaveConsumption(model.CostCenterId, products, values,
                GetCurrentUserAsync().Result.UserName, model.LateDateTime).Result;
            if (farm.Succeeded)
            {
                _toastNotification.AddSuccessToastMessage(farm.OkMessage, new ToastrOptions
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return RedirectToAction(nameof(SelectMealCostCenter));
            }

            _toastNotification.AddWarningToastMessage(farm.Errors, new ToastrOptions
            {
                PositionClass = ToastPositions.TopCenter
            });
            var newConsumption = new ConsumptionViewModel
            {
                CostCenterId = model.CostCenterId,
                CostCenterName = model.CostCenterName,
                ProductLists = _farm.GetProductListFailed(model.CostCenterId,"Meal", products, values).ToList()
            };
            return View(nameof(AddSpecialMeal), newConsumption);
        }

        public IActionResult AddSpecialMedical(long id)
        {
            var costCenter = _farm.GetCostCenterById(id);
            var prodList = _farm.GetProductList(id, "Medical").ToList();
            var consumptionList = new ConsumptionViewModel
            {
                CostCenterName = costCenter.Name,
                CostCenterId = costCenter.Id,
                ProductLists = prodList
            };

            return View(consumptionList);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AddSpecialMedical(ConsumptionViewModel model, IFormCollection form)
        {
            string[] products = form["key"].ToArray();
            string[] values = form["value"].ToArray();

            var farm = _farm.SaveConsumption(model.CostCenterId, products, values,
                GetCurrentUserAsync().Result.UserName, model.LateDateTime).Result;
            if (farm.Succeeded)
            {
                _toastNotification.AddSuccessToastMessage(farm.OkMessage, new ToastrOptions
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return RedirectToAction(nameof(SelectMealCostCenter));
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
            return View(nameof(AddSpecialMeal), newConsumption);
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
                _toastNotification.AddWarningToastMessage("Need to select a Cost Center", new ToastrOptions
                {
                    PositionClass = ToastPositions.TopCenter
                });
                ViewBag.LocationList = _farm.GetCostCenterMealList();
                return View();
            }
            var costCenter = _farm.GetCostCenterById(model.CostCenterId);

            return RedirectToAction(nameof(AddSpecialMeal),new {id=costCenter.Id});
        }

        public IActionResult SelectMedicalCostCenter()
        {
            ViewBag.LocationList = _farm.GetCostCenterMedicalList();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult SelectMedicalCostCenter(ConsumptionSelectActivity model)
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

            return RedirectToAction(nameof(AddSpecialMedical), new { id = costCenter.Id });
        }

        [Route("/get/consumption/json")]
        public JsonResult GetConsumptionJson()
        {
            return Json(_farm.GetConsumptionList());
        }

        [Route("/get/consumptionfolios/json/")]
        public JsonResult GetConsumptionFoliosJson()
        {
            return Json(_farm.GetFolio());
        }

        [Route("/get/consumptionfolio/json/{id}")]
        public JsonResult GetConsumptionFolioJson(long id)
        {
            return Json(_farm.GetConsumptionListByFolio(id));
        }

        public IActionResult ListConsumptionFolio()
        {
            return View();
        }

        public IActionResult ConsumptionDetail(long id)
        {
            ViewBag.Id = id;
            return View();
        }

        public IActionResult ConsumptionFolio(long id)
        {

            var folio = _farm.GetFolio().FirstOrDefault(x => x.Id == id);
            var model = new ReportPreview
            {
                ToConsumptionDate = folio.InitialDate.DateTime,
                WarehouseName = "",
                FromConsumptionDate = folio.FinalDate.DateTime,
            };
            
            

            var report = _farm.ConsumptionReport(model.FromConsumptionDate, model.ToConsumptionDate, model.WarehouseName, false,folio.Id).ToList();

            model.WarehouseName = report.Select(x => x.Warehouse).FirstOrDefault(); //report.Select(x => x.Warehouse).Distinct().ToString();
            model.ReportTable = report;

            var excel = _excel.GenerateExcelName("SalidaSoftland" + model.WarehouseName, model.ToConsumptionDate,
                model.WarehouseName);
            var excelPath = excel[0];
            var excelName = excel[1];
            if (excel[0] == null)
            {
                _toastNotification.AddInfoToastMessage(
                    "There is no consumption report between date " + model.FromConsumptionDate.ToString("MM-dd-yyyy") + " and "
                    + model.ToConsumptionDate.ToString("MM-dd-yyyy"),
                    new ToastrOptions
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                var list2 = _farm.GetWarehouseListNames();
                ViewBag.WarehouseList = list2;
                ViewBag.Url = "";
                return View(model);
            }
            //var file = new FileInfo(excel);

            using (var fs = new FileStream(excelPath, FileMode.Create, FileAccess.ReadWrite))
            {
                var workbook = new XSSFWorkbook();
                var format = workbook.CreateDataFormat();
                var style = workbook.CreateCellStyle();
                style.DataFormat = format.GetFormat("text");

                var numberFormat = workbook.CreateDataFormat();
                var numberStyle = workbook.CreateCellStyle();
                numberStyle.DataFormat = numberFormat.GetFormat("#.###");

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
                                row.CreateCell(j).SetCellValue(string.Empty);
                                break;
                            case 2:
                                var cell = row.CreateCell(j, CellType.String);
                                cell.SetCellValue(item.GeneratedDate);
                                cell.CellStyle = style;
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
                                var celln = row.CreateCell(j, CellType.Numeric);
                                celln.SetCellValue((double)item.Amount);
                                celln.CellStyle = numberStyle;
                                //row.CreateCell(j).SetCellValue((double) item.Amount);
                                break;
                        }

                    }

                    i++;
                }
                workbook.Write(fs);
            }

            ViewBag.Url = "/Temp/" + excelName;
            ViewBag.HeaderText = "Information for Folio: " + folio.Id;
            return View(model);
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
            if (consumption.CreatedOn <= DateTimeOffset.Now.AddDays(-1000))
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

        public IActionResult ViewConsumption(long? id)
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
            if (consumption.CreatedOn <= DateTimeOffset.Now.AddDays(-1000))
            {
                _toastNotification.AddWarningToastMessage("Id not valid", new ToastrOptions
                {
                    PositionClass = ToastPositions.TopCenter
                });
                return View(nameof(ListConsumption));
            }
            var folio = _farm.GetConsumptionById(id.Value).FolioId;
            var prodList = _farm.GetProductListFolio(id.Value,folio.Value).ToList();
            var consumptionList = new ConsumptionEditViewModel
            {
                CostCenterName = prodList.FirstOrDefault().CenterCostName,
                ConsumptionId = consumption.Id,
                ProductListEdit = prodList
            };
            ViewBag.FolioId = folio;
            return View(consumptionList);
        }

        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult EditConsumption(ConsumptionEditViewModel model,IFormCollection form)
        {
            string[] products = form["key"].ToArray();
            string[] values = form["value"].ToArray();
            if (ModelState.IsValid)
            {
                var farm = _farm.SaveConsumptionEdit(model.ConsumptionId, products, values,
                    GetCurrentUserAsync().Result.UserName).Result;
                if (farm.Succeeded)
                {
                    _toastNotification.AddSuccessToastMessage(farm.OkMessage, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    return RedirectToAction(nameof(ListConsumption));
                }
                else
                {
                    _toastNotification.AddWarningToastMessage(farm.Errors, new ToastrOptions()
                    {
                        PositionClass = ToastPositions.TopCenter
                    });
                    var newConsumption = new ConsumptionEditViewModel
                    {
                        CostCenterName = model.CostCenterName,
                        ConsumptionId = model.ConsumptionId,
                        ProductListEdit = _farm.GetProductListEditFailed(model.ConsumptionId,products,values).ToList()
                    };
                    return View(newConsumption);
                }
            }
            var newConsumptionState = new ConsumptionEditViewModel
            {
                CostCenterName = model.CostCenterName,
                ConsumptionId = model.ConsumptionId,
                ProductListEdit = _farm.GetProductListEditFailed(model.ConsumptionId, products, values).ToList()
            };
            return View(newConsumptionState);
        }
        
        //#help region 
        private Task<User> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }
    }
}