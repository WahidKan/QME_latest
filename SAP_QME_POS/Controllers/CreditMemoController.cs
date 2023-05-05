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
using SAP_QME_POS.Model.Setting;
using SAP_QME_POS.Model;
using SAP_QME_POS.Connection;
using SAP_QME_POS.Utilities;

namespace SAP_ARInvoice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CreditMemoController : Controller
    {
        private readonly ILogger _logger;
        private readonly ISAP_Connection _connection;
        private readonly IDataContext _dataContext;
        private readonly ICreditMemoExtension _creditMemoExtension;

        public CreditMemoController(IOptions<Setting> setting, ILogger<HomeController> logger, ICreditMemoExtension creditMemoExtension)
        {
            this._connection = new SAP_Connection(setting.Value);
            _logger = logger;
            _creditMemoExtension = creditMemoExtension;
            _dataContext = new DataContext(setting.Value);
        }

        [HttpGet]
        public async Task<string> GetAsync()
        {
            if (_connection.Connect() == 0)
            {
                Documents invoice = null;
                IDictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("@TDate", "02-10-2023");
                parameters.Add("@DTp", "SIV");
                List<Orders> invoices = _creditMemoExtension.CreditMemoMapper(await _dataContext.ArInvoice_SP<ViewModelCreditMemo>("SAP20W", parameters));
                foreach (var singleInvoice in invoices)
                {
                    var userResponse = await _creditMemoExtension.CheckBussinessCustomer(singleInvoice.CustName, _connection);

                    if (!userResponse)
                    {
                        _logger.LogError("Unable to Create New User");
                        return "SAP B1 Background service";
                    }

                    //var productResponse = await _creditMemoExtension.CheckIfArMemoExist(singleInvoice.OrderDetail, _connection);
                    //if (!productResponse)
                    //{
                    //    _logger.LogError("Unable to Create New Item");
                    //    return "SAP B1 Background service";
                    //}

                    //var invocieResponse = _creditMemoExtension.CheckIfInvoiceExist(singleInvoice.OrderCode,_connection);
                    //if (invocieResponse)
                    //{
                    //    _logger.LogError("Credit Memo Already Exist");
                    //    return "SAP B1 Background service";
                    //}

                    invoice = _connection.GetCompany().GetBusinessObject(BoObjectTypes.oCreditNotes);

                    invoice.CardCode = singleInvoice.CustName;
                    invoice.DocDueDate = DateTime.Now;
                    invoice.DocDate = DateTime.Now;
                    invoice.NumAtCard = singleInvoice.OrderCode;

                    foreach (var OrderItem in singleInvoice.OrderDetail)
                    {
                        invoice.Lines.ItemCode = OrderItem.ItemCode;
                        invoice.Lines.ItemDescription = OrderItem.IName;
                        invoice.Lines.WarehouseCode = OrderItem.WareHouse;
                        invoice.Lines.Quantity = Math.Abs(double.Parse(OrderItem.Quantity));
                        invoice.Lines.UnitPrice = OrderItem.UnitPrice;
                        invoice.Lines.CostingCode = OrderItem.Section;

                        #region Expenses
                        SAPbobsCOM.Recordset expenseRecordSet = null;
                        expenseRecordSet = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
                        expenseRecordSet.DoQuery($"SELECT T0.\"ExpnsCode\" FROM OEXD T0 WHERE Lower(\"ExpnsName\") = Lower('{OrderItem.TaxCode}') ");
                        if (expenseRecordSet.RecordCount != 0)
                        {
                            var expenseCode = expenseRecordSet.Fields.Item(0).Value;
                            invoice.Lines.Expenses.ExpenseCode = expenseCode;
                            invoice.Lines.Expenses.LineTotal = Math.Abs(double.Parse(OrderItem.TaxAmount));
                            invoice.Expenses.TaxCode = "S1";
                            invoice.Lines.Expenses.Add();
                        }

                        ///////////BankCode//////////////////
                        SAPbobsCOM.Recordset BankRecordSet = null;
                        BankRecordSet = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
                        BankRecordSet.DoQuery($"SELECT T0.\"ExpnsCode\" FROM OEXD T0 WHERE Lower(\"ExpnsName\") = Lower('{OrderItem.BankCode}') ");
                        if (BankRecordSet.RecordCount != 0)
                        {
                            var BankCode = BankRecordSet.Fields.Item(0).Value;
                            //invoice.Expenses.SetCurrentLine(1);
                            invoice.Expenses.ExpenseCode = BankCode;
                            invoice.Expenses.LineTotal = -double.Parse(OrderItem.BankDiscount);
                            invoice.Expenses.TaxCode = "S1";
                            invoice.Expenses.Add();
                        }
                        #endregion

                        invoice.Lines.Add();
                    }
                    if (invoice.Add() == 0)
                    {
                        _logger.LogInformation($"Record added successfully");
                    }
                    else
                    {
                        var errCode = _connection.GetCompany().GetLastErrorCode();
                        var response = _connection.GetCompany().GetLastErrorDescription();
                        _logger.LogError($"{errCode}:{response}");
                    }
                    _connection.GetCompany().Disconnect();
                }
            }
            else
            {
                _logger.LogError(_connection.GetErrorCode() + ": " + _connection.GetErrorMessage());
            }
            return "SAP B1 Background service";
        }
    }
}
