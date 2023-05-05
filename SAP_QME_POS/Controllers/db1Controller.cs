using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System;
using SAPbobsCOM;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using SAP_ARInvoice.Model;
using Microsoft.Extensions.Logging;
using SAP_ARInvoice.Model.DTO;
using SAP_QME_POS.Connection;
using SAP_QME_POS.Model;
using SAP_QME_POS.Model.Setting;
using SAP_QME_POS.Service;
using Microsoft.Extensions.Hosting;
using SAP_QME_POS.Utilities;

namespace SAP_ARInvoice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class db1Controller : Controller
    {
        private readonly ILogger _logger;
        private readonly ISAP_Connection _connection;
        private readonly DIService _BackServices;
        private readonly IARInvoiceExtension _invoiceExtension;
        private readonly IDataContext _dataContext;
        public db1Controller(IOptions<Setting> setting, ILogger<HomeController> logger,
            DIService hostedService, IARInvoiceExtension invoiceExtension)
        {
            _connection = new SAP_Connection(setting.Value);
            _logger = logger;
            _BackServices = hostedService;
            _invoiceExtension = invoiceExtension;
            _dataContext = new DataContext(setting.Value);
        }

        [HttpGet("arinvoice")]
        public async Task<string> ArInvoice1()
        {
            _ = new List<Orders>();
            if (_connection.Connect() == 0)
            {
                _logger.LogInformation("SAP connection build...");

                DateTime StartDate = new DateTime(2023, 02, 22);
                DateTime EndDate = new DateTime(2023, 02, 28);

                foreach (DateTime day in EachDay(StartDate, EndDate))
                {
                    Documents invoice = null;
                    IDictionary<string, string> parameters = new Dictionary<string, string>();
                    parameters.Add("@TDate", day.ToString("yyyy-MM-dd"));
                    parameters.Add("@DTp", "SIV");

                    var TotalPostedInvoices = 0;
                    double LineWiseOthDiscount = 0;
                    double LineWiseTaxSum = 0;
                    double ItemLineTotalSum = 0;

                    double LineWiseTaxAmtSum = 0;
                    double LineWiseotherDiscountSum = 0;
                    double HeaderDiscount;

                    double DiscountPercent;
                    double result2 = 0;

                    List<Orders> invoices = _invoiceExtension.InvoiceMapper(await _dataContext.ArInvoice_SP<DataModelSP>("SAP20", parameters));

                    if (invoices != null)
                    {
                        _logger.LogInformation("Invoices retrived from DB..." + day.Date.ToShortDateString());
                    }
                    else
                    {
                        //await _BackServices.StartAsync(new System.Threading.CancellationToken());
                        return "SAP B1 Background service | No Invoices for Date " + day.Date.ToShortDateString() + " QME";
                    }
                    foreach (var singleInvoice in invoices)
                    {
                        HeaderDiscount = 0;
                        DiscountPercent = 0;
                        LineWiseTaxAmtSum = singleInvoice.TaxAmountSum;
                        LineWiseotherDiscountSum = singleInvoice.OtherDiscountSum;

                        var userResponse = await _invoiceExtension.IsCustomerExist(singleInvoice, _connection);
                        if (!userResponse)
                        {
                            _logger.LogError("Unable to Create New Customer");
                            return "SAP B1 Background service";
                        }

                        var arMemo = _invoiceExtension.IsInvoiceExist(singleInvoice.OrderCode, _connection);
                        if (arMemo)
                        {
                            _logger.LogError("AR Invoice Already Exist: " + singleInvoice.OrderCode + " QME");
                            continue;
                        }
                        else
                        {
                            var productResponse = await _invoiceExtension.IsItemExist(singleInvoice.OrderDetail, _connection, BranchSelector.QME);
                            if (!productResponse)
                            {
                                _logger.LogError("Unable to Create New Item | QME");
                                return "SAP B1 Background service";
                            }

                            invoice = _connection.GetCompany().GetBusinessObject(BoObjectTypes.oInvoices);
                            invoice.CardCode = singleInvoice.CustName;
                            invoice.DocDueDate = DateTime.Parse(singleInvoice.OrderDate);
                            invoice.DocDate = DateTime.Parse(singleInvoice.OrderDate);
                            invoice.NumAtCard = singleInvoice.BranchName + "-" + singleInvoice.OrderCode;
                            invoice.Comments = "Comment Added Through DI-Api";
                            invoice.UserFields.Fields.Item("U_PBN").Value = singleInvoice.BranchName + "-" + singleInvoice.OrderCode;

                            ItemLineTotalSum = 0;
                            result2 = 0;
                            foreach (var item in singleInvoice.OrderDetail)
                            {
                                ItemLineTotalSum += (item.UnitPrice * double.Parse(item.Quantity));
                            }

                            foreach (var OrderItem in singleInvoice.OrderDetail)
                            {
                                invoice.Lines.ItemCode = OrderItem.ItemCode;
                                invoice.Lines.ItemDescription = OrderItem.IName;
                                invoice.Lines.WarehouseCode = OrderItem.WareHouse;
                                invoice.Lines.Quantity = double.Parse(OrderItem.Quantity);
                                invoice.Lines.UnitPrice = OrderItem.UnitPrice;
                                invoice.Lines.CostingCode = OrderItem.Section;

                                LineWiseTaxSum = 0;
                                LineWiseOthDiscount = 0;

                                LineWiseTaxSum += singleInvoice.TaxAmountSum;
                                LineWiseOthDiscount += singleInvoice.OtherDiscountSum;

                                var result = LineWiseOthDiscount - ItemLineTotalSum;
                                if ((singleInvoice.OrderDetail.IndexOf(OrderItem)) == 0)
                                {
                                    switch (singleInvoice.TaxCode)
                                    {
                                        case "16":
                                            invoice.Lines.Expenses.TaxCode = "14";
                                            invoice.Lines.Expenses.ExpenseCode = 14;
                                            //invoice.Lines.Expenses.TaxCode = "14";
                                            break;
                                        case "5":
                                            invoice.Lines.Expenses.TaxCode = "12";
                                            invoice.Lines.Expenses.ExpenseCode = 12;
                                            break;
                                        case "15":
                                            invoice.Lines.Expenses.TaxCode = "13";
                                            invoice.Lines.Expenses.ExpenseCode = 13;
                                            break;
                                        case "0":
                                            invoice.Lines.Expenses.TaxCode = "11";
                                            invoice.Lines.Expenses.ExpenseCode = 11;
                                            break;
                                    }

                                    if (result > 0)
                                    {
                                        invoice.Lines.Expenses.LineTotal = 0;
                                        //LineWiseTaxAmtSum -= LineWiseTaxSum;
                                        //HeaderDiscount += (otherDiscountSum - LineWiseTaxSum);
                                    }
                                    else
                                    {

                                        //invoice.Expenses.LineTotal = singleInvoice.TaxAmountSum;
                                        HeaderDiscount += LineWiseOthDiscount;
                                    }
                                    if (ItemLineTotalSum > LineWiseotherDiscountSum)
                                    {

                                        invoice.Lines.Expenses.LineTotal = singleInvoice.TaxAmountSum;
                                        invoice.Lines.Expenses.Add();
                                    }
                                    else
                                    {
                                        invoice.Lines.Expenses.LineTotal = 0;
                                        invoice.Lines.Expenses.Add();
                                    }
                                }
                                invoice.Lines.Add();

                                #region Batch wise Item
                                //SAPbobsCOM.Items product = null;
                                //SAPbobsCOM.Recordset recordSet = null;
                                //SAPbobsCOM.Recordset recordSetOBTN = null;
                                //recordSet = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
                                //recordSetOBTN = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
                                //product = _connection.GetCompany().GetBusinessObject(BoObjectTypes.oItems);

                                //recordSet.DoQuery($"select T1.\"U_ItemCode\",T1.\"U_Qty\",T1.\"U_Whs\",T1.\"U_Section\" from \"@BOMH\" T0 " +
                                //    $"INNER JOIN \"@BOMR\" T1 ON T0.\"DocEntry\"=T1.\"DocEntry\" " +
                                //    $"WHERE T0.\"U_ItemCode\"='{OrderItem.ItemCode}' AND NOT T1.\"U_ItemCode\" IS NULL AND T0.\"U_Whs\"='{OrderItem.WareHouse}' AND T0.\"U_Section\"='{OrderItem.Section}'");

                                //var BOMTotal = recordSet.RecordCount;
                                //var BOMCurrentCount = 0;
                                //if (recordSet.RecordCount != 0)
                                //{
                                //    while (BOMTotal > BOMCurrentCount)
                                //    {
                                //        var itemCode = recordSet.Fields.Item(0).Value.ToString();
                                //        var IngredientQuantity = double.Parse(recordSet.Fields.Item(1).Value.ToString()) * double.Parse(OrderItem.Quantity);
                                //        double Qty = double.Parse(recordSet.Fields.Item(1).Value.ToString());
                                //        var whs = recordSet.Fields.Item(2).Value.ToString();
                                //        var section = recordSet.Fields.Item(3).Value.ToString();

                                //        invoice.Lines.ItemCode = itemCode;
                                //        invoice.Lines.WarehouseCode = whs;
                                //        invoice.Lines.CostingCode = section;

                                //        invoice.Lines.Quantity = double.Parse($"{IngredientQuantity}");

                                //        recordSetOBTN.DoQuery($"select T0.\"ItemCode\",T1.\"Quantity\", T0.\"DistNumber\" from \"OBTN\" T0 " +
                                //            $"INNER JOIN \"OBTQ\" T1 on T0.\"ItemCode\" = T1.\"ItemCode\" and T0.\"SysNumber\" = T1.\"SysNumber\" " +
                                //            $"INNER JOIN \"OITM\" T2 on T0.\"ItemCode\" = T2.\"ItemCode\" where T1.\"Quantity\" > 0 and T0.\"ItemCode\" = '{itemCode}' and T1.\"WhsCode\"='{whs}' order by T0.\"ExpDate\"");

                                //        var TotalCount = recordSetOBTN.RecordCount;
                                //        var CurrentCount = 0;

                                //        while (TotalCount > CurrentCount)
                                //        {
                                //            if (IngredientQuantity > 0)
                                //            {
                                //                var ExpDate = recordSetOBTN.Fields.Item(0).Value.ToString();
                                //                var AvailableQuantity = recordSetOBTN.Fields.Item(1).Value.ToString();
                                //                var BatchNumber = recordSetOBTN.Fields.Item(2).Value.ToString();
                                //                if (double.Parse(AvailableQuantity) > 0)
                                //                {
                                //                    invoice.Lines.BatchNumbers.BatchNumber = BatchNumber;
                                //                    invoice.Lines.BatchNumbers.ItemCode = itemCode;
                                //                    //invoice.Lines.BatchNumbers.ExpiryDate = ExpDate;

                                //                    if (double.Parse(AvailableQuantity) >= IngredientQuantity)
                                //                    {
                                //                        invoice.Lines.BatchNumbers.Quantity = IngredientQuantity;
                                //                        IngredientQuantity = 0;
                                //                    }
                                //                    else
                                //                    {
                                //                        invoice.Lines.BatchNumbers.Quantity = double.Parse(AvailableQuantity);
                                //                        IngredientQuantity = IngredientQuantity - double.Parse(AvailableQuantity);
                                //                    }
                                //                    invoice.Lines.BatchNumbers.Add();
                                //                };
                                //            }
                                //            CurrentCount += 1;
                                //            recordSetOBTN.MoveNext();
                                //        }

                                //        //if (!IngredientQuantity.Equals(0))
                                //        //{
                                //        //    _logger.LogError($"Not Enough Data in Given Batch= " + OrderItem.ItemCode);
                                //        //    //continue;
                                //        //    return "SAP B1 Background service";
                                //        //}

                                //        invoice.Lines.Add();
                                //        BOMCurrentCount += 1;
                                //        recordSet.MoveNext();
                                //    }
                                //}

                                //else
                                //{
                                //    _logger.LogError($"No BOM found angainst given Item No= " + OrderItem.ItemCode + " | QME");
                                //    //return "SAP B1 Background service";
                                //    continue;
                                //}

                                #endregion
                            }

                            #region Expenses

                            if (LineWiseotherDiscountSum > 0)
                            {
                                if (ItemLineTotalSum > LineWiseotherDiscountSum)
                                {
                                    result2 = (LineWiseotherDiscountSum / ItemLineTotalSum) * 100; //+ LineWiseTaxSum;
                                    DiscountPercent = result2;
                                }
                                else
                                {
                                    var ab = LineWiseotherDiscountSum - LineWiseTaxAmtSum;
                                    DiscountPercent = (ab / ItemLineTotalSum) * 100;
                                }
                            }

                            if (singleInvoice.BankCode != null)
                            {

                                if (LineWiseotherDiscountSum > 0)
                                {
                                    if (ItemLineTotalSum < LineWiseotherDiscountSum)
                                    {
                                        //LineWiseTaxSum -= LineWiseTaxSum;

                                        invoice.Expenses.LineTotal = singleInvoice.BankDiscountSum; //- LineWiseTaxSum;
                                    }
                                    //else
                                    //{
                                    //    invoice.Expenses.LineTotal = -singleInvoice.BankDiscountSum;
                                    //}
                                }
                                else
                                {
                                    invoice.Expenses.LineTotal = -singleInvoice.BankDiscountSum;
                                }
                                if (singleInvoice.BankCode == "0")
                                {
                                    invoice.Expenses.ExpenseCode = 31;
                                    invoice.Expenses.TaxCode = "31";
                                }

                                else
                                {
                                    invoice.Expenses.ExpenseCode = int.Parse(singleInvoice.BankCode);
                                    invoice.Expenses.TaxCode = singleInvoice.BankCode;
                                }

                                invoice.Expenses.Add();
                            }
                            #endregion
                            if (DiscountPercent > 100)
                            {
                                invoice.DiscountPercent = 100;
                            }
                            else
                            {
                                invoice.DiscountPercent = DiscountPercent;
                            }

                            if (invoice.Add() == 0)
                            {
                                _logger.LogInformation($"Invoice added for bill: " + singleInvoice.BranchName + "-" + singleInvoice.OrderCode + " | QME");
                                TotalPostedInvoices += 1;
                            }
                            else
                            {
                                var errCode = _connection.GetCompany().GetLastErrorCode();
                                var response = _connection.GetCompany().GetLastErrorDescription();
                                _logger.LogError($"{errCode}:{response}:{ singleInvoice.BranchName + "-" + singleInvoice.OrderCode}");
                            }
                        }
                    }
                    _logger.LogError($"{TotalPostedInvoices} Invoices posted successfully! Date=" + day.ToShortDateString() + " QME");
                   
                }
                _connection.GetCompany().Disconnect();
                //disc
                // await _BackServices.StartAsync(new System.Threading.CancellationToken());
            }
            else
            {
                _logger.LogError(_connection.GetErrorCode() + ": " + _connection.GetErrorMessage());
            }

            return "SAP B1 Background service";
        }


        //[HttpGet("ArInvoiceTest")]
        //public async Task<string> ArInvoiceTest()
        //{
        //    _ = new List<Orders>();
        //    if (_connection.Connect2() == 0)
        //    {
        //        _logger.LogInformation("Connected to SAP B1");

        //        DateTime StartDate = new DateTime(2023, 02, 03);
        //        DateTime EndDate = new DateTime(2023, 02, 03);

        //        foreach (DateTime day in EachDay(StartDate, EndDate))
        //        {
        //            Documents invoice = null;
        //            IDictionary<string, string> parameters = new Dictionary<string, string>();
        //            parameters.Add("@TDate", day.ToString("yyyy-MM-dd"));
        //            parameters.Add("@DTp", "SIV");

        //            var TotalPostedInvoices = 0;
        //            double LineWiseOthDiscount = 0;
        //            double LineWiseTaxSum = 0;
        //            double ItemLineTotalSum = 0;

        //            double LineWiseTaxAmtSum = 0;
        //            double LineWiseotherDiscountSum = 0;
        //            double HeaderDiscount;

        //            double DiscountPercent;
        //            double result2 = 0;
        //            List<Orders> invoices = _invoiceExtension.InvoiceMapper(await _dataContext.ArInvoice_SP<DataModelSP>("SAP20", parameters));

        //            foreach (var singleInvoice in invoices)
        //            {
        //                if (singleInvoice.CustName == "102000703" && singleInvoice.OrderCode == "000181")
        //                {
        //                    HeaderDiscount = 0;
        //                    DiscountPercent = 0;
        //                    LineWiseTaxAmtSum = singleInvoice.TaxAmountSum;
        //                    LineWiseotherDiscountSum = singleInvoice.OtherDiscountSum;

        //                    //var userResponse = await _invoiceExtension.IsCustomerExist(singleInvoice, _connection);
        //                    //if (!userResponse)
        //                    //{
        //                    //    _logger.LogError("Unable to Create New User");
        //                    //    return "SAP B1 Background service";
        //                    //}

        //                    //var arMemo = CheckIfInvoiceExist(singleInvoice.OrderCode);
        //                    //if (arMemo)
        //                    //{
        //                    //    _logger.LogError("AR Invoice Already Exist");
        //                    //    continue;
        //                    //}

        //                    var productResponse = await _invoiceExtension.IsItemExist(singleInvoice.OrderDetail, _connection, BranchSelector.QME);
        //                    if (!productResponse)
        //                    {
        //                        _logger.LogError("Unable to Create New Item");
        //                        return "SAP B1 Background service";
        //                    }

        //                    invoice = _connection.GetCompany().GetBusinessObject(BoObjectTypes.oInvoices);
        //                    invoice.CardCode = singleInvoice.CustName;
        //                    invoice.DocDueDate = DateTime.Parse(singleInvoice.OrderDate);
        //                    invoice.DocDate = DateTime.Parse(singleInvoice.OrderDate);
        //                    invoice.NumAtCard = singleInvoice.BranchName + "-" + singleInvoice.OrderCode;
        //                    invoice.Comments = "Comment Added Through DI-Api";
        //                    invoice.UserFields.Fields.Item("U_PBN").Value = singleInvoice.OrderCode;

        //                    ItemLineTotalSum = 0;
        //                    result2 = 0;
        //                    foreach (var item in singleInvoice.OrderDetail)
        //                    {
        //                        ItemLineTotalSum += (item.UnitPrice * double.Parse(item.Quantity));
        //                    }

        //                    foreach (var OrderItem in singleInvoice.OrderDetail)
        //                    {
        //                        //if (OrderItem.ItemCode == "M009")
        //                        //{
        //                        invoice.Lines.ItemCode = OrderItem.ItemCode;
        //                        invoice.Lines.ItemDescription = OrderItem.IName;//
        //                        invoice.Lines.WarehouseCode = OrderItem.WareHouse;
        //                        invoice.Lines.Quantity = double.Parse(OrderItem.Quantity);
        //                        invoice.Lines.UnitPrice = OrderItem.UnitPrice;
        //                        invoice.Lines.CostingCode = OrderItem.Section;

        //                        LineWiseTaxSum = 0;
        //                        LineWiseOthDiscount = 0;

        //                        LineWiseTaxSum += singleInvoice.TaxAmountSum;
        //                        LineWiseOthDiscount += singleInvoice.OtherDiscountSum;

        //                        var result = LineWiseOthDiscount - ItemLineTotalSum;
        //                        if ((singleInvoice.OrderDetail.IndexOf(OrderItem)) == 0)
        //                        {
        //                            switch (singleInvoice.TaxCode)
        //                            {
        //                                case "16":
        //                                    invoice.Lines.Expenses.TaxCode = "14";
        //                                    invoice.Lines.Expenses.ExpenseCode = 14;
        //                                    //invoice.Lines.Expenses.TaxCode = "14";
        //                                    break;
        //                                case "5":
        //                                    invoice.Lines.Expenses.TaxCode = "12";
        //                                    invoice.Lines.Expenses.ExpenseCode = 12;
        //                                    break;
        //                                case "15":
        //                                    invoice.Lines.Expenses.TaxCode = "13";
        //                                    invoice.Lines.Expenses.ExpenseCode = 13;
        //                                    break;
        //                                case "0":
        //                                    invoice.Lines.Expenses.TaxCode = "11";
        //                                    invoice.Lines.Expenses.ExpenseCode = 11;
        //                                    break;
        //                            }

        //                            if (result > 0)
        //                            {
        //                                invoice.Lines.Expenses.LineTotal = 0;
        //                                //LineWiseTaxAmtSum -= LineWiseTaxSum;
        //                                //HeaderDiscount += (otherDiscountSum - LineWiseTaxSum);
        //                            }
        //                            else
        //                            {
        //                                //invoice.Expenses.LineTotal = singleInvoice.TaxAmountSum;
        //                                HeaderDiscount += LineWiseOthDiscount;
        //                            }
        //                            if (ItemLineTotalSum > LineWiseotherDiscountSum)
        //                            {
        //                                invoice.Lines.Expenses.LineTotal = Math.Ceiling(singleInvoice.TaxAmountSum);
        //                                invoice.Lines.Expenses.Add();
        //                            }
        //                            else
        //                            {
        //                                invoice.Lines.Expenses.LineTotal = 0;
        //                                invoice.Lines.Expenses.Add();
        //                            }
        //                        }
        //                        invoice.Lines.Add();
        //                        //}
        //                    }

        //                    #region Expenses

        //                    if (LineWiseotherDiscountSum > 0)
        //                    {
        //                        if (ItemLineTotalSum > LineWiseotherDiscountSum)
        //                        {
        //                            result2 = (LineWiseotherDiscountSum / ItemLineTotalSum) * 100; //+ LineWiseTaxSum;
        //                            DiscountPercent = result2;
        //                        }
        //                        else
        //                        {
        //                            var ab = LineWiseotherDiscountSum - LineWiseTaxAmtSum;
        //                            DiscountPercent = (ab / ItemLineTotalSum) * 100;
        //                        }
        //                    }

        //                    if (singleInvoice.BankCode != null)
        //                    {

        //                        if (LineWiseotherDiscountSum > 0)
        //                        {
        //                            if (ItemLineTotalSum < LineWiseotherDiscountSum)
        //                            {
        //                                //LineWiseTaxSum -= LineWiseTaxSum;

        //                                invoice.Expenses.LineTotal = Math.Ceiling(singleInvoice.BankDiscountSum); //- LineWiseTaxSum;
        //                            }
        //                            //else
        //                            //{
        //                            //    invoice.Expenses.LineTotal = -singleInvoice.BankDiscountSum;
        //                            //}
        //                        }
        //                        else
        //                        {
        //                            invoice.Expenses.LineTotal = Math.Ceiling(-singleInvoice.BankDiscountSum);
        //                        }

        //                        if (singleInvoice.BankCode == "0")
        //                        {
        //                            invoice.Expenses.ExpenseCode = 31;
        //                            invoice.Expenses.TaxCode = "31";
        //                        }
        //                        else
        //                        {
        //                            invoice.Expenses.ExpenseCode = int.Parse(singleInvoice.BankCode);
        //                            invoice.Expenses.TaxCode = singleInvoice.BankCode;
        //                        }

        //                        invoice.Expenses.Add();
        //                    }
        //                    #endregion
        //                    if (DiscountPercent > 100)
        //                    {
        //                        invoice.DiscountPercent = 100;
        //                    }
        //                    else
        //                    {
        //                        invoice.DiscountPercent = DiscountPercent;
        //                    }

        //                    if (invoice.Add() == 0)
        //                    {
        //                        _logger.LogInformation($"Record added successfully for Invoice No= " + singleInvoice.OrderCode + " | AH");
        //                        TotalPostedInvoices += 1;
        //                    }
        //                    else
        //                    {
        //                        var errCode = _connection.GetCompany().GetLastErrorCode();
        //                        var response = _connection.GetCompany().GetLastErrorDescription();
        //                        _logger.LogError($"{errCode}:{response}:{singleInvoice.OrderCode}");
        //                    }
        //                }

        //            }
        //            _logger.LogError($"{TotalPostedInvoices} Invoices posted successfully! Date=" + day.ToShortDateString());
        //            //return $"{TotalPostedInvoices} Invoices posted successfully!";
        //        }
        //        _connection.GetCompany().Disconnect();
        //        await _BackServices.StartAsync(new System.Threading.CancellationToken());
        //    }
        //    else
        //    {
        //        _logger.LogError(_connection.GetErrorCode() + ": " + _connection.GetErrorMessage());
        //    }

        //    return "SAP B1 Background service";
        //}
        public IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
                yield return day;
        }
    }


}

