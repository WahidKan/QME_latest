using SAP_ARInvoice.Model.DTO;
using SAP_QME_POS.Connection;
using SAP_QME_POS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_QME_POS.Utilities
{
    public interface IARInvoiceExtension
    {
        public List<Orders> InvoiceMapper(List<DataModelSP> data);
        public Task<bool> IsCustomerExist(Orders orders, ISAP_Connection _connection);
        public Task<bool> IsItemExist(List<OrderDetail> orderDetail, ISAP_Connection _connection, BranchSelector branchSelector);
        public bool IsInvoiceExist(string orderCode, ISAP_Connection _connection);
    }
}
