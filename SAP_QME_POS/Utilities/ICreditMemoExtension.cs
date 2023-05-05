using SAP_ARInvoice.Model.DTO;
using SAP_QME_POS.Connection;
using SAP_QME_POS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_QME_POS.Utilities
{
    public interface ICreditMemoExtension
    {
        public List<Orders> CreditMemoMapper(List<ViewModelCreditMemo> data);
        public Task<bool> CheckIfArMemoExist(List<OrderDetail> orderDetail, ISAP_Connection _connection);
        public Task<bool> CheckBussinessCustomer(string CustomerId, ISAP_Connection _connection);
        public bool CheckIfInvoiceExist(string orderCode, ISAP_Connection _connection);
    }
}
