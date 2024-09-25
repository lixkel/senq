using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Server {

    /// <summary>
    /// Start local http server running on localhost:80 for benchmarking and testing purposes
    /// The server is useful for benchmarking and testing purposes
    /// </summary>
    public class HttpServer : IDisposable {
        private readonly CancellationTokenSource _cancellationTokenS = new CancellationTokenSource();
        private readonly Task _startServerTask;
        private Task _serverTask;

        /// <summary>
        /// Initializes a new instance of HttpServer class and starts the server in a new thread
        /// </summary>
        public HttpServer() {
            // Start the server in new thread with cancellation token
            _startServerTask = Task.Run(() => StartServer(_cancellationTokenS.Token));
        }

        /// <summary>
        /// Starts the HTTP server and serves static files from a specified directory
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to stop the server when needed</param>
        private void StartServer(CancellationToken cancellationToken) {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            // Find path to local website directory
            String gitRootPath = FindGitRoot();
            gitRootPath += "/www/";

            // Setup local http server directory
            app.UseStaticFiles(new StaticFileOptions {
                FileProvider = new PhysicalFileProvider(
                    // TODO: Navigate to git root directory automatically
                    Path.Combine(Directory.GetCurrentDirectory(), gitRootPath)),
                RequestPath = ""
            });

            app.Urls.Add("http://localhost:80");

            // Run the server in a non-blocking way
            _serverTask = app.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Finds the root directory of the Git repository containing the current directory.
        /// </summary>
        /// <returns>The path to the Git root directory</returns>
        /// <exception cref="InvalidOperationException">Thrown when the current directory is not within a Git repository</exception>
        public static string FindGitRoot() {
            string currentDirectory = Directory.GetCurrentDirectory();

            while (!string.IsNullOrEmpty(currentDirectory)) {
                if (Directory.Exists(Path.Combine(currentDirectory, ".git"))) {
                    return currentDirectory;
                }

                DirectoryInfo parentDirectory = Directory.GetParent(currentDirectory);
                if (parentDirectory == null) {
                    break;
                }
                currentDirectory = parentDirectory.FullName;
            }

            throw new InvalidOperationException("Not in a Git repository.");
        }

        /// <summary>
        /// Disposes the server and cancels any running tasks.
        /// </summary>
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