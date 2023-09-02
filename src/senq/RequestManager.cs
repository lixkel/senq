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
    public class RequestManager {
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

        /// <summary>
        /// Address that will be used for testing internet connection by <see cref="CheckConnection"/>
        /// Note: Feel free to replace it with any other reliable URL if needed.
        /// </summary>
        private const string TestAddress = "https://www.google.com/";

        public RequestManager() {
            ChangeProxyClients(null, true);
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
        /// Blocking method that sends a GET request to a given URL and returns its content.
        /// </summary>
        /// <param name="webAddr">The URI to request.</param>
        /// <returns>The response content as a string.</returns>
        public string GET(Uri webAddr) {             // TODO: http://localhost:8000/
            HttpClient client = GetRandomClient();   // Error: Connection refused (localhost:8000)

            var request = new HttpRequestMessage(HttpMethod.Get, webAddr); // GOT and still running
            request.Headers.UserAgent.ParseAdd(GetRandomUserAgent());

            try {
                using (HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult()) {
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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
            userAgents = newUserAgents; // TODO: User Agent verification
        }

        /// <summary>
        /// Replaces the current list of HttpClient instances with new ones based on provided proxy addresses.
        /// </summary>
        /// <param name="proxyAddresses">List of proxy addresses.</param>
        /// <exception cref="NoWorkingClientsException">Thrown when all provided proxies are non functioning and Host address can't be used.</exception>
        /// <exception cref="NoConnectionException">Thrown when connection from host to the internet couldn't be established.</exception>
        public void ChangeProxyClients(List<string>? proxyAddresses, bool useHostAddress) {
            HttpClient httpClient = new HttpClient();

            if (!CheckConnection(httpClient).Result) {
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
        /// Checks if HttpClient provides an active internet connection.
        /// </summary>
        /// <param name="client">An instance of HttpClient used to make a web request.</param>
        /// <returns>True if there's internet connectivity, otherwise False.</returns>
        public async Task<bool> CheckConnection(HttpClient client) {
            try {
            var response = await client.GetAsync(TestAddress);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException) {
            // This catch block will handle cases where the request fails
            return false;
        }
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
        /// Converts a string into a valid URI object if possible, considering it may be relative to the provided URI.
        /// </summary>
        /// <param name="address">Address string.</param>
        /// <param name="currentServerUri">Current server URI.</param>
        /// <returns>Valid Uri object or null.</returns>
        public static Uri? FormatUri(string address, Uri currentServerUri) {
            // Check if the address is already a valid absolute URI
            if (Uri.IsWellFormedUriString(address, UriKind.Absolute)) {
                return new Uri(address);
            }

            // Check if the address is a valid relative URI
            if (Uri.IsWellFormedUriString(address, UriKind.Relative)) {
                return new Uri(currentServerUri, address);
            }

            // If it's neither, try to make it a valid relative URI by adding slash in front
            if (!address.StartsWith("/")) {
                string newAddress = "/" + address;

                if (Uri.IsWellFormedUriString(newAddress, UriKind.Relative)) {
                    return new Uri(currentServerUri, newAddress);
                }
            }
            

            // try formatting non relative address
            Uri? newUri = FormatUri(address, currentServerUri.Scheme); // scheme http or https

            if (newUri != null) {
                return newUri;
            }

            return null;
        }

        /// <summary>
        /// Converts a string into a valid URI object if possible, with the help of provided protocol scheme.
        /// </summary>
        /// <param name="address">The address string.</param>
        /// <param name="scheme">The scheme (default is "http").</param>
        /// <returns>A valid Uri object or null.</returns>
        public static Uri? FormatUri(string address, string scheme) {
            // If address seems like a domain (contains a dot but doesn't start with a slash), 
            // but lacks the scheme, prepend it with the scheme from currentServerUri
            if (!address.StartsWith("/")) {
                string newAddress = scheme + "://" + address;

                if (Uri.IsWellFormedUriString(newAddress, UriKind.Absolute)) {
                    return new Uri(newAddress);
                }
            }

            return null;
        }

        /// <summary>
        /// Converts a string into a valid URI object if possible, assuming default protocol scheme is http.
        /// </summary>
        /// <param name="address">The address string.</param>
        /// <returns>A valid Uri object or null.</returns>
        public static Uri? FormatUri(string address) {
            // Check if the address is already a valid absolute URI
            if (Uri.IsWellFormedUriString(address, UriKind.Absolute)) {
                return new Uri(address);
            }

            string defaultScheme = "http";
            return FormatUri(address, defaultScheme);
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
                httpClientTasks.Add(TestProxy(newClient));
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

        /// <summary>
        /// Tests a proxy by sending an api request and checking if the IP matches the proxy's IP.
        /// </summary>
        /// <param name="client">The HttpClient instance to test.</param>
        /// <returns>The tested HttpClient if the proxy is working, otherwise null.</returns>
        public static async Task<HttpClient?> TestProxy(HttpClient client) {
            const string apiAddress = "https://api.ipify.org?format=json";

            try {
                var response = client.GetStringAsync(apiAddress).Result;
                using (JsonDocument doc = JsonDocument.Parse(response)) {
                    if (doc.RootElement.TryGetProperty("ip", out var ipElement) && ipElement.GetString() != null) {
                        string returnedIp = ipElement.GetString();
                        IPAddress[] proxyIpAddresses;

                        proxyIpAddresses = await Dns.GetHostAddressesAsync(client.BaseAddress.ToString());

                        // Check if any of the resolved IP addresses match the returned IP
                        foreach (var proxyIp in proxyIpAddresses) {
                            if (proxyIp.ToString() == returnedIp) {
                                return client; // The returned IP matches one of the proxy's IP addresses
                            }
                        }
                    }
                    return null;
                }
            }
            catch (Exception) {
                // If any exception occurs it's almost certain that the proxy isn't working.
                return null;
            }
        }
    }
}