using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Security.Cryptography;

namespace ConsoleAppServer
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("hostsettings.json", optional: true)
                .AddCommandLine(args)
                .Build();

            string file = "localhost.cer"; // Contains name of certificate file
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            var cert = new X509Certificate2(X509Certificate.CreateFromCertFile(file));
            store.Add(cert);
            store.Close();
            return WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 5000);  // http:localhost:5000
                    options.Listen(IPAddress.Any, 80);         // http:*:80
                    options.Listen(IPAddress.Loopback, 8080, listenOptions =>
                    {
                        listenOptions.UseHttps(cert);
                    });
                })
                .UseConfiguration(config)
                .ConfigureServices(opt =>
                {
                    opt.AddCors(options =>
                    {
                        options.AddPolicy("CorsPolicy", builder => builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
                    });
                })
                .Configure(app =>
                {
                    app.UseCors("CorsPolicy");
                    app.Map("/test", HandleMapTest1);
                    app.Map("", ServeHomePage);
                    //app.Run(context =>
                    //    context.Response.WriteAsync("Hello, World!"));
                });
        }

        private static void ServeHomePage(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                await ReturnIndexPage(context);
            });
        }

        private static void HandleMapTest1(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                await context.Response.WriteAsync("Map Test 1");
            });
        }

        private static async Task ReturnIndexPage(HttpContext context)
        {
            var file = new FileInfo(@"wwwroot\index.html");
            byte[] buffer;
            if (file.Exists)
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "text/html";

                buffer = File.ReadAllBytes(file.FullName);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.ContentType = "text/plain";
                buffer = Encoding.UTF8
                    .GetBytes("Unable to find the requested file");
            }

            context.Response.ContentLength = buffer.Length;

            using (var stream = context.Response.Body)
            {
                await stream.WriteAsync(buffer, 0, buffer.Length);
                await stream.FlushAsync();
            }
        }

        private void SetStartup()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            //set the executable path
            rk.SetValue("MedirisServerInstance", "EXECUTABLEPATH");

        }

        public void GetLatestVersion()
        {
            //Update the application in here
        }
    }
}
