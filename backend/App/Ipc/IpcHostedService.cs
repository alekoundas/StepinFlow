using Business.DataService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace App.Ipc
{
    public class IpcHostedService : BackgroundService
    {
        private readonly ILogger<IpcHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDataService _dataService;

        public IpcHostedService(ILogger<IpcHostedService> logger, IServiceScopeFactory scopeFactory, IDataService dataService)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _dataService = dataService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                string? input = await Console.In.ReadLineAsync();
                if (string.IsNullOrEmpty(input)) continue;

                try
                {
                    Dictionary<string, object>? msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(input);
                    string? command = (string)msg["command"];

                    //using var scope = _scopeFactory.CreateScope();
                    //var flowService = scope.ServiceProvider.GetRequiredService<IFlowService>();

                    switch (command)
                    {
                        case "executeFlow":
                            //var flowId = Convert.ToInt32(msg["flowId"]);
                            //await flowService.ExecuteFlowAsync(flowId);
                            break;
                            // Add more commands
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "IPC error");
                    //Console.WriteLine(JsonConvert.SerializeObject(new { event = "error", message = ex.Message }));
                    //Console.Out.Flush();
                }
}
        }
    }
}