using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Net.NetworkInformation;
using System.Timers;
using Timer = System.Threading.Timer;
using System.Net.Http;
using SAP_ARInvoice.Controllers;

namespace SAP_QME_POS.Service
{
    public class DIService : IHostedService, IDisposable
    {
        private Timer timer;
        private readonly ILogger<DIService> logger;

        public DIService(ILogger<DIService> logger)
        {
            this.logger = logger;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
      {
            timer = new Timer(async o =>
            {
                using (var client = new HttpClient())
                {
                    //var result = await client.GetAsync("https://localhost:5001/arinvoice/1");

                    //var result2 = await client.GetAsync("https://localhost:5001/arinvoice");
                }
                logger.LogInformation($"Background Service");
            },
      null,
      TimeSpan.Zero,
      TimeSpan.FromMinutes(5));

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            timer.Dispose();
        }
    }
}

