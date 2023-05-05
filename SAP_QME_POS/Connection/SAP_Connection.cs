using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SAP_QME_POS.Model.Setting;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace SAP_QME_POS.Connection
{
    public class SAP_Connection :ISAP_Connection
    {
        private SAPbobsCOM.Company company = new SAPbobsCOM.Company();
        private int connectionResult;
        private int errorCode = 0;
        private string errorMessage = "";
        private Setting _setting;

        public SAP_Connection(Setting setting)
        {
            _setting = setting;
        }

        public int Connect()
        {
            company.Server = _setting.Server;
            company.CompanyDB = _setting.CompanyDB;
            company.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_HANADB;
            company.DbUserName = _setting.DbUserName;
            company.DbPassword = _setting.DbPassword;
            company.UserName = _setting.UserName;
            company.Password = _setting.Password;
            company.language = SAPbobsCOM.BoSuppLangs.ln_English;
            company.UseTrusted = _setting.UseTrusted;
            company.LicenseServer = _setting.LicenseServer;

            connectionResult = company.Connect();

            if (connectionResult != 0)
            {
                company.GetLastError(out errorCode, out errorMessage);
            }

            return connectionResult;
        }
        public int Connect2()
        {
            company.Server = _setting.Server;
            company.CompanyDB = _setting.CompanyDB2;
            company.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_HANADB;
            company.DbUserName = _setting.DbUserName;
            company.DbPassword = _setting.DbPassword;
            company.UserName = _setting.UserName;
            company.Password = _setting.Password;
            company.language = SAPbobsCOM.BoSuppLangs.ln_English;
            company.UseTrusted = _setting.UseTrusted;
            company.LicenseServer = _setting.LicenseServer;

            connectionResult = company.Connect();

            if (connectionResult != 0)
            {
                company.GetLastError(out errorCode, out errorMessage);
            }

            return connectionResult;
        }
        public SAPbobsCOM.Company GetCompany()
        {
            return this.company;
        }
        public int GetErrorCode()
        {
            return this.errorCode;
        }
        public String GetErrorMessage()
        {
            return this.errorMessage;
        }
       
    }
}
