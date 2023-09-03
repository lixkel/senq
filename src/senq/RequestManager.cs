using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Senq {

    /// <summary>
    /// Manages HTTP requests with options for random user agent rotation and random proxy rotation.
    /// </summary>
    public class RequestManager : IWebHandler {
        private List<HttpClient> clients = new List<HttpClient>();
    
        /// <summary>
        /// Thread-local random to ensure thread safety when generating random numbers
        /// </summary>
        private readonly ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed))); // TODO: try looking for alternatives

        private static int seed = Environment.TickCount;
        private List<String> userAgents = new List<string> {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/116.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 13.5; rv:109.0) Gecko/20100101 Firefox/116.0",
            "Mozilla/5.0 (X11; Linux i686; rv:109.0) Gecko/20100101 Firefox/116.0"
        };

        public RequestManager() {
            ChangeProxy(null, true);
        }

        /// <summary>
        /// Initializes a new instance of the RequestManager class with specified proxy addresses and user agents.
        /// </summary>
        /// <param name="proxyAddresses">List of proxy addresses.</param>
        /// <param name="newUserAgents">List of user agent strings.</param>
        public RequestManager(List<String> proxyAddresses, List<string> newUserAgents) {
            clients = NewClients(proxyAddresses);
            userAgents = newUserAgents;
        }

        /// <summary>
        /// Non-blocking method that sends a GET request to a given URL and returns its content using HttpClient.
        /// </summary>
        /// <param name="webAddr">The URI to request.</param>
        /// <returns>The response content as a string.</returns>
        public async Task<string> GET(Uri webAddr) {
            HttpClient client = GetRandomClient();

            var request = new HttpRequestMessage(HttpMethod.Get, webAddr);
            request.Headers.UserAgent.ParseAdd(GetRandomUserAgent());

            try {
                using (HttpResponseMessage response = await client.SendAsync(request)) {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException e) { // This exception is thrown by EnsureSuccessStatusCode
                Console.Error.WriteLine($"\nError {webAddr}: {e.Message}");
                return "";
            }
            catch (TaskCanceledException e) { // This is thrown when the request times out
                Console.Error.WriteLine($"\nRequest timed out {webAddr}: {e.Message}");
                return "";
            }
            catch (Exception e) {
                Console.WriteLine(e);
                return"";
            }
        }

        /// <summary>
        /// Replaces the current list of user agents with a new one.
        /// </summary>
        /// <param name="newUserAgents">List of user agent strings.</param>
        public void ChangeUserAgents(List<string> newUserAgents) {
            userAgents = newUserAgents;
        }

        /// <summary>
        /// Replaces the current list of HttpClient instances with new ones based on provided proxy addresses.
        /// </summary>
        /// <param name="proxyAddresses">List of proxy addresses.</param>
        /// <exception cref="NoWorkingClientsException">Thrown when all provided proxies are non functioning and Host address can't be used.</exception>
        /// <exception cref="NoConnectionException">Thrown when connection from host to the internet couldn't be established.</exception>
        public void ChangeProxy(List<string>? proxyAddresses, bool useHostAddress) {
            HttpClient httpClient = new HttpClient();

            if (!NetworkTools.CheckConnection(httpClient).Result) {
                throw new NoConnectionException();
            }

            List<HttpClient> newClients = new List<HttpClient>();

            if (proxyAddresses != null) {
                newClients = NewClients(proxyAddresses);
            }

            if (useHostAddress) {
                newClients.Add(httpClient);
            }

            if (newClients.Count == 0) {
                throw new NoWorkingClientsException();
            }

            clients = newClients;
        }

        /// <summary>
        /// Returns a randomly chosen user agent.
        /// </summary>
        /// <returns>A user agent string.</returns>
        private string GetRandomUserAgent() {
            int index = threadLocalRandom.Value.Next(userAgents.Count);
            return userAgents[index];
        }

        /// <summary>
        /// Returns a randomly chosen HttpClient instance.
        /// </summary>
        /// <returns>An instance of HttpClient.</returns>
        private HttpClient GetRandomClient() {
            int index = threadLocalRandom.Value.Next(clients.Count);
            return clients[index];
        }

        /// <summary>
        /// Creates a list of HttpClient instances from provided proxy addresses. HttpClients are tested if the proxy's are working.
        /// </summary>
        /// <param name="proxyAddresses">List of proxy addresses.</param>
        /// <returns>List of workin HttpClient instances based on provided proxy addresses.</returns>
        public static List<HttpClient> NewClients(List<String> proxyAddresses) {
            var httpClientTasks = new List<Task<HttpClient?>>();

            foreach (string proxyAddress in proxyAddresses) {
                HttpClientHandler handler = new HttpClientHandler {
                    Proxy = new WebProxy(proxyAddress, false),
                    UseProxy = true
                };

                HttpClient newClient = new HttpClient(handler);
                httpClientTasks.Add(NetworkTools.TestProxy(newClient));
            }

            List<HttpClient> httpClients = new List<HttpClient>();

            // Test each proxy and await their results
            while (httpClientTasks.Any()) {
                var completedTask = Task.WhenAny(httpClientTasks).Result;
                httpClientTasks.Remove(completedTask);
                var newClient = completedTask.Result;

                if (newClient != null) {
                    httpClients.Add(newClient);
                }
            }

            return httpClients;
        }
    }
}