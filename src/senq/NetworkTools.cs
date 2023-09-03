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
    public static class NetworkTools {

        /// <summary>
        /// Address that will be used for testing internet connection by <see cref="CheckConnection"/>
        /// Note: Feel free to replace it with any other reliable URL if needed.
        /// </summary>
        private const string TestAddress = "https://www.google.com/";

        /// <summary>
        /// Checks if HttpClient provides an active internet connection.
        /// </summary>
        /// <param name="client">An instance of HttpClient used to make a web request.</param>
        /// <returns>True if there's internet connectivity, otherwise False.</returns>
        public static async Task<bool> CheckConnection(HttpClient client) {
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

        /// <summary>
        /// Test if two Uris are from the same domain.
        /// </summary>
        public static bool FromSameDomain(Uri uri1, Uri uri2) {
            return uri1.Host == uri2.Host;
        }
    }
}