using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_QME_POS.Model
{
    public class ViewModelCreditMemo
    {
        public string Dtype { get; set; }
        public string BillNo { get; set; }
        public string TDate { get; set; }
        public string CusCode { get; set; }
        public string ICode { get; set; }
        public string Qty { get; set; }
        public string BSec { get; set; }
        public string IRate { get; set; }
        public string TaxCode { get; set; }
        public string TaxAmt { get; set; }
        public string BankCode { get; set; }
        public string DisAmt { get; set; }
        public string OthCode { get; set; }
        public string OthDisAmt { get; set; }
        public string BranchId { get; set; }
        public string IName { get; set; }
        public string CName { get; set; }
    }
}
