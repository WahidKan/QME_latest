using SAP_ARInvoice.Model.DTO;
using SAP_QME_POS.Connection;
using SAP_QME_POS.Model;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_QME_POS.Utilities
{
    public class CreditMemoExtension : ICreditMemoExtension
    {
        public List<Orders> CreditMemoMapper(List<ViewModelCreditMemo> data)
        {
            List<Orders> orders = new List<Orders>();
            List<ViewModelCreditMemo> resp = data.Select(x => new { x.CusCode, x.BillNo }).Distinct().Select(x => data.FirstOrDefault(r => r.CusCode == x.CusCode && r.BillNo == x.BillNo)).Distinct().ToList();
            foreach (var item in resp)
            {
                var orderDetail = data.Where(x => x.BillNo == item.BillNo && x.CusCode == item.CusCode).Select(x => new OrderDetail
                {
                    ItemCode = x.ICode,
                    IName = x.IName,
                    Quantity = x.Qty,
                    BankDiscount = x.DisAmt,
                    TaxCode = x.TaxCode,
                    TaxAmount = x.TaxAmt,
                    BankCode = x.BankCode,
                    DisAmt = x.DisAmt,
                    WareHouse = x.BSec,
                    OthDisAmt = x.OthDisAmt,
                    Section = x.BranchId,
                    UnitPrice = double.Parse(x.IRate),
                    OrderCode = x.BillNo

                }).Distinct().ToList();

                orders.Add(new Orders()
                {
                    CustName = item.CusCode,
                    OrderCode = item.BillNo,
                    OrderDate = item.TDate,
                    TaxAmountSum = orderDetail.Sum(x => double.Parse(x.TaxAmount)),
                    BankDiscountSum = orderDetail.Sum(x => double.Parse(x.DisAmt)),
                    OtherDiscountSum = orderDetail.Sum(x => double.Parse(x.OthDisAmt)),
                    BankCode = item.BankCode,
                    //BankDiscount = item.DisAmt,
                    TaxCode = item.TaxCode,
                    //TaxAmount = item.TaxAmt,
                    OrderDetail = orderDetail
                });
            }

            return orders;
        }
        public async Task<bool> CheckIfArMemoExist(List<OrderDetail> orderDetail, ISAP_Connection _connection)
        {
            bool output = false;
            SAPbobsCOM.Items product = null;
            SAPbobsCOM.Recordset recordSet = null;
            recordSet = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
            product = _connection.GetCompany().GetBusinessObject(BoObjectTypes.oItems);

            foreach (var singleOrderDetail in orderDetail)
            {
                recordSet.DoQuery($"SELECT * FROM \"OITM\" WHERE \"ItemCode\"='{singleOrderDetail.ItemCode}'");
                if (recordSet.RecordCount == 0)
                {
                    product.ItemCode = singleOrderDetail.ItemCode;
                    //product.ItemName = item.ItemDescription;
                    //product.PurchaseItemsPerUnit = Double.Parse(item.UnitPrice);

                    var resp = product.Add();
                    if (resp.Equals(0))
                    {
                        output = true;
                    }
                    else
                    {
                        output = false;
                    }
                    //IDictionary<string, string> parameters = new Dictionary<string, string>();
                    //parameters.Add("@ItemCode", singleOrderDetail.ItemCode);
                    //List<Item> items = await _.ArInvoice_SP<Item>("GetItems", parameters);
                    //foreach (var item in items)
                    //{
                    //    product.ItemCode = item.ItemCode;
                    //    product.ItemName = item.ItemDescription;
                    //    product.PurchaseItemsPerUnit = Double.Parse(item.UnitPrice);

                    //    var resp = product.Add();
                    //    if (resp.Equals(0))
                    //    {
                    //        output = true;
                    //    }
                    //    else
                    //    {
                    //        output = false;
                    //    }

                    //}

                }
                else
                {
                    output = true;
                }
            }


            return output;
        }
        public async Task<bool> CheckBussinessCustomer(string CustomerId, ISAP_Connection _connection)
        {
            bool output = false;
            SAPbobsCOM.Recordset recordSet = null;
            BusinessPartners businessPartners = null;
            recordSet = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
            businessPartners = _connection.GetCompany().GetBusinessObject(BoObjectTypes.oBusinessPartners);

            recordSet.DoQuery($"SELECT * FROM \"OCRD\" WHERE \"CardCode\"='{CustomerId}'");
            if (recordSet.RecordCount == 0)
            {
                businessPartners.CardCode = CustomerId;
                var response = businessPartners.Add();
                if (response.Equals(0))
                {
                    return true;

                }
                else
                {
                    return false;
                }
                //IDictionary<string, string> parameters = new Dictionary<string, string>();
                //parameters.Add("@CardCode", CustomerId);

                //List<Customer> customer = await _connection.ArInvoice_SP<Customer>("[dbo].[GetCustomer]", parameters);
                //foreach (var item in customer)
                //{
                //    businessPartners.CardCode = item.CardCode;
                //    businessPartners.CardName = item.CustName;
                //    businessPartners.Phone1 = item.Phone;
                //    businessPartners.CardType = BoCardTypes.cCustomer;
                //    businessPartners.SubjectToWithholdingTax = (BoYesNoNoneEnum)BoYesNoEnum.tNO;
                //    var response = businessPartners.Add();
                //    if (response.Equals(0))
                //    {
                //        return true;

                //    }
                //    else
                //    {
                //        return false;
                //    }
                //}
            }
            else
            {
                output = true;
            }
            return output;
        }
        public bool CheckIfInvoiceExist(string orderCode, ISAP_Connection _connection)
        {
            bool output = false;
            SAPbobsCOM.Recordset recordSet = null;
            recordSet = _connection.GetCompany().GetBusinessObject(BoObjectTypes.BoRecordset);
            recordSet.DoQuery($"SELECT * FROM \"ORIN\" WHERE \"NumAtCard\"='{orderCode}'");
            if (recordSet.RecordCount > 0)
            {
                output = true;
            }
            return output;

        }
    }
}
