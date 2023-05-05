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

namespace SAP_ARInvoice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController
    {
        [HttpGet]
        public async Task<string> GetAsync()
        {
            return "SAP B1 Background service";
        }


    }
}