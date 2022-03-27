using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using xlwDotNet;
using xlwDotNet.xlwTypes;

namespace Template
{
    public class KestrelExperiment
    {
        public static void Main(string[] args) { }

        static IHost myHost;
        static MyHubPersistent myHub;
        static bool hasStarted = false;

       [ExcelOnOpen]
        public static void StartKestrel()
        {
            myHost = CreateHostBuilder().Build();
            myHost.StartAsync().Wait();
            myHub = myHost.Services.GetRequiredService<MyHubPersistent>();
            hasStarted = true;
        }

       [ExcelOnClose]
        public static void StopKestrel()
        {
            myHost.StopAsync().Wait();
        }


        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseKestrel(options => options.ListenAnyIP(12345))
                    .UseStartup<Startup>();
                });



        [ExcelExport("Send Excel Range")]
        public static String SendRange(
         [Parameter("The Cell Range to send")]    CellMatrix cellMatrix)
        {
           
            var block = new object[cellMatrix.RowsInStructure];
            for (int i = 0; i < cellMatrix.RowsInStructure; ++i)
            {
                var row = new object[cellMatrix.ColumnsInStructure];
                for (int j = 0; j < cellMatrix.ColumnsInStructure; ++j)
                {
                    switch (cellMatrix[i, j].ValueType)
                    {
                        case CellValue.ValueTypeEnum.Boolean:
                            row[j] = cellMatrix[i, j].BooleanValue();
                            break;
                        case CellValue.ValueTypeEnum.Empty:
                            row[j] = string.Empty;
                            break;
                        case CellValue.ValueTypeEnum.Number:
                            row[j] = cellMatrix[i, j].NumericValue();
                            break;
                        case CellValue.ValueTypeEnum.String:
                            row[j] = cellMatrix[i, j].StringValue();
                            break;
                        case CellValue.ValueTypeEnum.Error:
                            row[j] = "Error : " + cellMatrix[i, j].ErrorValue();
                            break;
                    }

                }
                block[i] = row;

            }
            myHub.broadcast(block);
            return "Cool";
        }
    }
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddSingleton<MyHubPersistent>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async (context) =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.StatusCode = 200;
                    var htmlFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "index.html");
                    await context.Response.Body.WriteAsync(File.ReadAllBytes(htmlFile));
                });
                endpoints.MapHub<MyHub>("/myhub");
            });
        }


    }



    internal class MyHub : Hub
    {

    }

    internal class MyHubPersistent
    {
        readonly IHubContext<MyHub> hubContext;
        public MyHubPersistent(IHubContext<MyHub> hubContext) =>
            this.hubContext = hubContext;


        public Task broadcast(object[] CellBlock) =>
            hubContext.Clients.All.SendAsync("GetMatrix", CellBlock);
    }
}
