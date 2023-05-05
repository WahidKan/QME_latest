using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_QME_POS.Connection
{
    public interface ISAP_Connection
    {
        int Connect();
        int Connect2();

        public SAPbobsCOM.Company GetCompany();
        int GetErrorCode();

        String GetErrorMessage();

    }
}
