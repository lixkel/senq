using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Threading.Tasks;

namespace Benchmarks
{
    class Benchmark
    {
        public static async Task Main(string[] args)
        {
            // Start the server in new thread
            var serverTask = Task.Run(() => StartServer());

            StartBenchmarks();
        }

        /// <summary>
        /// Sets up and starts local HTTP server running on localhost:80
        /// </summary>
        private static void StartServer()
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            // Setup local http server directory
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    // TODO: Navigete to git root directory
                    Path.Combine(Directory.GetCurrentDirectory(), "../../www")),
                RequestPath = ""
            });

            app.Urls.Add("http://localhost:80");
            app.Run();
        }

        /// <summary>
        /// Runs the actual benchmarks
        /// </summary>
        private static void StartBenchmarks()
        {
            var summary = BenchmarkRunner.Run<StaticBenchmarks>();
            //summary = BenchmarkRunner.Run<DynamicBenchmarks>();
        }
    }
}