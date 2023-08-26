using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Senq {

    public class RequestManager {
        static readonly HttpClient client = new HttpClient();
        // I should try looking for alternatives
        private readonly ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));
        private static int seed = Environment.TickCount;
        private List<String> userAgents = new List<string> {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/116.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 13.5; rv:109.0) Gecko/20100101 Firefox/116.0",
            "Mozilla/5.0 (X11; Linux i686; rv:109.0) Gecko/20100101 Firefox/116.0"
        };

        RequestManager(List<String> proxyAddresses, List<string> userAgents) {
            List<HttpClient> httpClients = new List<HttpClient>();

            foreach (string proxyAddress in proxyAddresses) {
                HttpClientHandler handler = new HttpClientHandler {
                    Proxy = new WebProxy(proxyAddress, false),
                    UseProxy = true
                };

                httpClients.Add(new HttpClient(handler));
            }
        }

        public string GET(string webAddr) {
            try {
                var request = new HttpRequestMessage(HttpMethod.Get, webAddr);
                request.Headers.UserAgent.ParseAdd(GetRandomUserAgent());

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

        public void changeUserAgents(List<string> newUserAgents) {
            userAgents = newUserAgents;
        }

        private string GetRandomUserAgent() {
            int index = threadLocalRandom.Value.Next(userAgents.Count);
            return userAgents[index];
        }

        public static Uri? FormatUri(string address, Uri currentServerUri) {
            // try formatting non relative address
            Uri? newUri = FormatUri(address);

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

        public static Uri? FormatUri(string address) {
            // Check if the address is already a valid absolute URI
            if (Uri.IsWellFormedUriString(address, UriKind.Absolute)) {
                return new Uri(address);
            }

            // If address seems like a domain (contains a dot but doesn't start with a slash), 
            // but lacks the scheme, prepend it with the scheme from currentServerUri
            if (!address.StartsWith("/") && address.Contains(".")) {
                string scheme = currentServerUri.Scheme; // http or https
                string newAddress = scheme + "://" + address;

                if (Uri.IsWellFormedUriString(newAddress, UriKind.Absolute)) {
                    return new Uri(address);;
                }
            }

            return null;
        }

    }
}