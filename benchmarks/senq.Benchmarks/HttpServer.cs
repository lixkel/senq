using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Threading.Tasks;

namespace Server {

    /// <summary>
    /// Start local http server running on localhost:80 for benchmarking and testing purposes
    /// </summary>
    public class HttpServer : IDisposable {
        private readonly CancellationTokenSource _cancellationTokenS = new CancellationTokenSource();
        private readonly Task _startServerTask;
        private Task _serverTask;

        public HttpServer() {
            // Start the server in new thread with cancellation token
            _startServerTask = Task.Run(() => StartServer(_cancellationTokenS.Token));
        }

        private void StartServer(CancellationToken cancellationToken) {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            // Setup local http server directory
            app.UseStaticFiles(new StaticFileOptions {
                FileProvider = new PhysicalFileProvider(
                    // TODO: Navigate to git root directory automatically
                    Path.Combine(Directory.GetCurrentDirectory(), "../../../../../www/")),
                RequestPath = ""
            });

            app.Urls.Add("http://localhost:80");

            // Run the server in a non-blocking way
            _serverTask = app.StartAsync(cancellationToken);
        }

        public void Dispose() {
            _cancellationTokenS.Cancel();
            try {
                _serverTask.Wait();
            }
            catch (AggregateException) when (_serverTask.IsCanceled) {
                // Expected when task is canceled
            }
        }
    }
}