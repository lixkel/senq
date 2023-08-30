using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Senq {

    public class RequestManager {
        private List<HttpClient> clients = new List<HttpClient>{ new HttpClient() };
        // I should try looking for alternatives
        private readonly ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));
        private static int seed = Environment.TickCount;
        private List<String> userAgents = new List<string> {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/116.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 13.5; rv:109.0) Gecko/20100101 Firefox/116.0",
            "Mozilla/5.0 (X11; Linux i686; rv:109.0) Gecko/20100101 Firefox/116.0"
        };

        public RequestManager() {
        }

        public RequestManager(List<String> proxyAddresses, List<string> newUserAgents) {
            clients = NewClients(proxyAddresses);
            userAgents = newUserAgents;
        }

        public string GET(Uri webAddr) {
            HttpClient client = GetRandomClient();

            var request = new HttpRequestMessage(HttpMethod.Get, webAddr);
            request.Headers.UserAgent.ParseAdd(GetRandomUserAgent());

            try {
                using (HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult()) {
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
            }
            catch (HttpRequestException e) { // This exception is thrown by EnsureSuccessStatusCode
                Console.WriteLine("\nError: {0}", e.Message);
                return "";
            }
            catch (TaskCanceledException e) { // This is thrown when the request times out
                Console.WriteLine("\nRequest timed out: {0}", e.Message);
                return "";
            }
            catch (Exception e) {
                //Console.WriteLine(e);
                return"";
            }
        }

        public void ChangeUserAgents(List<string> newUserAgents) {
            userAgents = newUserAgents;
        }

        public void ChangeProxy(List<string> proxyAddresses) {
            clients = NewClients(proxyAddresses);
        }

        private string GetRandomUserAgent() {
            int index = threadLocalRandom.Value.Next(userAgents.Count);
            return userAgents[index];
        }

        private HttpClient GetRandomClient() {
            int index = threadLocalRandom.Value.Next(clients.Count);
            return clients[index];
        }

        public static Uri? FormatUri(string address, Uri currentServerUri) {
            // try formatting non relative address
            Uri? newUri = FormatUri(address, currentServerUri.Scheme); // scheme http or https

            if (newUri != null) {
                return newUri;
            }

            // Check if the address is a valid relative URI
            if (Uri.IsWellFormedUriString(address, UriKind.Relative)) {
                return new Uri(currentServerUri, address);
            }

            // If it's neither, try to make it a valid relative URI by adding slash in front
            if (!address.StartsWith("/")) {
                string newAddress = "/" + address;

                if (Uri.IsWellFormedUriString(newAddress, UriKind.Relative)) {
                    return new Uri(currentServerUri, address);
                }
            }

            return null;
        }

        public static Uri? FormatUri(string address, string scheme = "http") {
            // Check if the address is already a valid absolute URI
            if (Uri.IsWellFormedUriString(address, UriKind.Absolute)) {
                return new Uri(address);
            }

            // If address seems like a domain (contains a dot but doesn't start with a slash), 
            // but lacks the scheme, prepend it with the scheme from currentServerUri
            if (!address.StartsWith("/") && address.Contains(".")) {
                string newAddress = scheme + "://" + address;

                if (Uri.IsWellFormedUriString(newAddress, UriKind.Absolute)) {
                    return new Uri(address);
                }
            }

            return null;
        }

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

            while (httpClientTasks.Any()) {
                var completedTask = Task.WhenAny(httpClientTasks).Result;
                httpClientTasks.Remove(completedTask);
                var newClient = completedTask.Result;

                if (newClient != null) {
                    httpClients.Add(newClient);
                }
            }

            if (httpClients.Count == 0) {
                httpClients.Add(new HttpClient());
            }

            return httpClients;
        }

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