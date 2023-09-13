using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using PuppeteerSharp;

namespace Senq {

    /// <summary>
    /// Manages HTTP requests with options for random user agent rotation and random proxy rotation.
    /// </summary>
    public class PupepeteerManager : IWebHandler {
        private List<IBrowser> clients = new List<IBrowser>();

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

        public PupepeteerManager() {
            BrowserFetcher browserFetcher = new BrowserFetcher();
            var result = browserFetcher.DownloadAsync(BrowserTag.Latest).Result;
            ChangeProxy(null, true);
        }

        /// <summary>
        /// Initializes a new instance of the RequestManager class with specified proxy addresses and user agents.
        /// </summary>
        /// <param name="proxyAddresses">List of proxy addresses.</param>
        /// <param name="newUserAgents">List of user agent strings.</param>
        public PupepeteerManager(List<String> proxyAddresses, List<string> newUserAgents) {
            BrowserFetcher browserFetcher = new BrowserFetcher();
            var result = browserFetcher.DownloadAsync(BrowserTag.Latest).Result;
            ChangeProxy(proxyAddresses, true);
            userAgents = newUserAgents;
        }

        /// <summary>
        /// Non-blocking method that based on given URL returns its content with executed scripts..
        /// </summary>
        /// <param name="webAddr">The URI to request.</param>
        /// <returns>Whole web page in string.</returns>
        public async Task<string> GET(Uri webAddr) {
            IBrowser browser = GetRandomClient();
            using (IPage newTab = await browser.NewPageAsync()) { // TODO: delete
                await newTab.SetUserAgentAsync(GetRandomUserAgent());
                await newTab.GoToAsync(webAddr.ToString(),
                                       new NavigationOptions { // Wait until HTML document's DOM has been loaded and parsed.
                                            WaitUntil = new WaitUntilNavigation[] { WaitUntilNavigation.DOMContentLoaded }
                                       });
                
                // Get the content of fully loaded page
                return await newTab.GetContentAsync();
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
        private IBrowser GetRandomClient() {
            int index = threadLocalRandom.Value.Next(clients.Count);
            return clients[index];
        }

        /// <summary>
        /// Replaces the current list of user agents with a new one.
        /// </summary>
        /// <param name="newUserAgents">List of user agent strings.</param>
        public void ChangeUserAgents(List<string> newUserAgents) {
            userAgents = newUserAgents;
        }

        /// <summary>
        /// Replaces the current list of Pupeteer instances with new ones based on provided proxy addresses.
        /// </summary>
        /// <param name="proxyAddresses">List of proxy addresses.</param>
        /// <exception cref="NoWorkingClientsException">Thrown when all provided proxies are non functioning and Host address can't be used.</exception>
        /// <exception cref="NoConnectionException">Thrown when connection from host to the internet couldn't be established.</exception>
        public void ChangeProxy(List<string>? proxyAddresses, bool useHostAddress) {
            HttpClient httpClient = new HttpClient();

            // Check connection to internet
            if (!NetworkTools.CheckConnection(httpClient).Result) {
                throw new NoConnectionException();
            }

            List<string> validProxies = new List<string>();

            if (proxyAddresses != null) {
                validProxies = NetworkTools.NewClients(proxyAddresses).Select(c => c.Item2).ToList();
            }

            List<IBrowser> newBrowsers = CreatePuppeteerFromProxy(validProxies);

            if (useHostAddress) {
                newBrowsers.Add(Puppeteer.LaunchAsync(new LaunchOptions()).Result);
            }

            if (newBrowsers.Count == 0) {
                throw new NoWorkingClientsException();
            }

            clients = newBrowsers;
        }

        /// <summary>
        /// Returns newly created puppeteer instances using provided proxy addresses without testing.
        /// </summary>
        /// <param name="proxyAddresses">List of proxy addresses.</param>
        public static List<IBrowser> CreatePuppeteerFromProxy(List<string> proxyAddresses) {
             List<IBrowser> browsers = new List<IBrowser>();
        
            foreach (var proxy in proxyAddresses) {
                var launchOptions = new LaunchOptions {
                    Args = new[] { $"--proxy-server={proxy}" }
                };
                
                IBrowser browser = Puppeteer.LaunchAsync(launchOptions).Result;
                browsers.Add(browser);
            }

            return browsers;
        }
    }
}