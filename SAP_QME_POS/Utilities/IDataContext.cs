using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_QME_POS.Utilities
{
    public interface IDataContext
    {
        public  Task<List<T>> ArInvoice_SP<T>(string SpName, IDictionary<string, string> parameters);
        public List<T> ArInvoice_API<T>(string baseURI);
       
    }
}
