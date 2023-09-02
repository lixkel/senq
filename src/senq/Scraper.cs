using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

#nullable enable

namespace Senq {

    /// <summary>
    /// Configuration class for the Senq scraper.
    /// </summary>
    public class SenqConf {
        /// <summary>
        /// Web address from where to start scrape.
        /// </summary>
        public string webAddr { get; init; }

        /// <summary>
        /// Regex pattern for target data (Scraper is expecting target group in for the data extraction).
        /// </summary>
        public string targetRegex { get; init; }

        /// <summary>
        /// List of user agents to rotate through.
        /// </summary>
        public List<string>? userAgents { get; init; } = null;

        /// <summary>
        /// List of proxy addresses to rotate through.
        /// </summary>
        public List<string>? proxyAddresses { get; init; } = null;

        /// <summary>
        /// Should the host address of this computer also be used for scraping
        /// </summary>
        public bool useHostAddress { get; init; } = true;

        /// <summary>
        /// Method that handles output of data.
        /// </summary>
        public Action<string, string> output { get; init; }

        /// <summary>
        /// Method that is responsible for finding links inside the webpage code.
        /// </summary>
        public Func<string, List<string>> linkFinder { get; init; } = DataMiner.FindLinks;

        /// <summary>
        /// Specifies the maximum depth the scraper should follow links. 
        /// </summary>
        public int maxDepth { get; init; } = 0;

        /// <summary>
        /// Can the scraper leave starting domain?
        /// </summary>
        public bool stayOnDomain { get; init; } = false;

        /// <summary>
        /// Converts a CLI configuration to a Senq configuration. Using explicit and implicit operator
        /// was not possible as CLISenqConf is internal class.
        /// </summary>
        /// <param name="originalConf">CLI configuration to convert from.</param>
        internal SenqConf(CLISenqConf originalConf) {
            webAddr = originalConf.webAddr;
            targetRegex = originalConf.targetRegex;
            maxDepth = originalConf.maxDepth;
            useHostAddress = originalConf.useHostAddress;
            stayOnDomain = originalConf.stayOnDomain;

            linkFinder = DataMiner.FindLinks;
            userAgents = originalConf.userAgents.ToList();
            proxyAddresses = originalConf.proxyAddresses.ToList();

            switch (originalConf.outputType) {
                case OutputType.csv:
                    if (originalConf.outputFile == null) {
                        output = Output.CSVOut;
                        break;
                    }
                    output = Output.CSVFileWriter.GetWriter(originalConf.outputFile);
                    break;

                case OutputType.db:
                    output = Output.DatabaseWriter.GetWriter(originalConf.dbString);
                    break;
            }
        }

        internal static SenqConf ToSenqConf(CLISenqConf originalConf) {
            return new SenqConf(originalConf);
        }

        public SenqConf() {
        }
    }

    /// <summary>
    /// Internal configuration struct for the Senq scraper.
    /// </summary>
    internal struct InternalSenqConf {
        public Regex regex { get; init; }
        public Uri webAddr { get; init; }
        public List<string>? userAgents { get; init; }
        public Action<string, string> output { get; init; }
        public Func<string, List<string>> linkFinder { get; init; }
        public int maxDepth { get; init; }
        public bool stayOnDomain { get; init; }

        /// <summary>
        /// The current depth of scraping.
        /// </summary>
        public int depth {get; init; } = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalSenqConf"/> class using the provided <see cref="SenqConf"/>.
        /// </summary>
        /// <param name="originalConf">The CLI configuration to convert from.</param>
        public InternalSenqConf(SenqConf originalConf) {
            output = originalConf.output;
            webAddr = CheckStartingAddress(originalConf.webAddr);
            maxDepth = originalConf.maxDepth;
            linkFinder = originalConf.linkFinder;
            stayOnDomain = originalConf.stayOnDomain;

            regex = new Regex(originalConf.targetRegex);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalSenqConf"/> class using the provided <see cref="CLISenqConf"/>.
        /// </summary>
        /// <param name="originalConf">The CLI configuration to convert from.</param>
        public InternalSenqConf(CLISenqConf originalConf) : this(SenqConf.ToSenqConf(originalConf)) {
        }

        /// <summary>
        /// Checks and formats the given URI if possible.
        /// </summary>
        /// <param name="address">Web address to be checked.</param>
        /// <returns>URI of the provided web address.</returns>
        /// <exception cref="BadStartingAddressException">Thrown when starting address for scraping is in bad format.</exception>
        private static Uri CheckStartingAddress(string address) {
            Uri? newUri = RequestManager.FormatUri(address);

            if (newUri == null) {
                throw new BadStartingAddressException();
            }
            return newUri;
        }
    }


    /// <summary>
    /// Main web scraper class thats supposed to be called.
    /// </summary>
    public class Scraper {
        /// <summary>
        /// Handles all interactions with network for all threads.
        /// </summary>
        private RequestManager rm = new RequestManager();

        /// <summary>
        /// Number of currently running scraping tasks.
        /// </summary>
        private int scrapeTasks = 0;

        /// <summary>
        /// Initiate scraping based on the given Senq configuration and configure RequestManager.
        /// </summary>
        /// <param name="conf">The Senq configuration for scraping.</param>
        public async Task Scrape(SenqConf conf) {
            RmConf(conf);

            InternalSenqConf internalConf = new InternalSenqConf(conf);

            await Scrape(internalConf);
        }

        /// <summary>
        /// Initiate scraping based on the given Senq configuration and configure RequestManager.
        /// </summary>
        /// <param name="conf">CLI configuration for scraping.</param>
        internal async Task Scrape(CLISenqConf conf) {
            await Scrape(SenqConf.ToSenqConf(conf));
        }

        /// <summary>
        /// Initiates data structures for scraping and starts thread to scrape the starting page.
        /// </summary>
        /// <param name="conf">Senq configuration for scraping.</param>
        private async Task Scrape(InternalSenqConf conf) {
            BlockingCollection<(string, string)> queue = new BlockingCollection<(string, string)>();

            var outputTask = Task.Factory.StartNew(() => {
                OutputHandler(queue, conf.output);
            }, TaskCreationOptions.LongRunning);

            Task.Run(() => ScrapePage(conf, queue));
            IncrementScrapeTasks();

            // output thread is the last thread that will exit as it wits for all scraping threads to end
            // and for the blocking queue to be empty
            outputTask.Wait();

            Output.DisposeIfIsDisposable(conf.output);
        }

        /// <summary>
        /// Configures the Request Manager based on the given configuration.
        /// </summary>
        /// <param name="conf">The configuration to apply to the Request Manager.</param>
        private void RmConf(SenqConf conf) {
            if (conf.userAgents != null && conf.userAgents.Count != 0) {
                rm.ChangeUserAgents(conf.userAgents);
            }
            rm.ChangeProxyClients(conf.proxyAddresses, conf.useHostAddress);
        }

        /// <summary>
        /// Handles scraping of one page on website. Main scraping function.
        /// </summary>
        /// <param name="conf">The configuration to use while scraping.</param>
        /// <param name="queue">Blocking collection to add matches to.</param>
        private async Task ScrapePage(InternalSenqConf conf, BlockingCollection<(string, string)> queue) {
            //Console.WriteLine($" GET: {conf.webAddr}");

            string webPage = await rm.GET(conf.webAddr);

            HandleMatches(conf, queue, webPage);

            HandleLinks(conf, queue, webPage);

            DecrementScrapeTasks();
            //Console.WriteLine($"tasks: {scrapeTasks}");

            if (scrapeTasks == 0) {
                queue.CompleteAdding();
            }
        }

        /// <summary>
        /// Processes matches from the webpage and adds them to the queue for output.
        /// </summary>
        /// <param name="conf">Internal scraping configuration.</param>
        /// <param name="queue">Blocking collection to add matches to.</param>
        /// <param name="webPage">Webpage content to scrape.</param>
        private void HandleMatches(InternalSenqConf conf, BlockingCollection<(string, string)> queue, string webPage) {
            List<string> matches = DataMiner.FindAll(webPage, conf.regex);

            foreach (string match in matches ) {
                queue.Add((conf.webAddr.ToString(), match));
            }
        }

        /// <summary>
        /// Finds and processes links from the webpage and starts new taks for crawling and further scraping.
        /// </summary>
        /// <param name="conf">Internal scraping configuration.</param>
        /// <param name="queue">Blocking collection for output.</param>
        /// <param name="webPage">Webpage content to process.</param>
        private void HandleLinks(InternalSenqConf conf, BlockingCollection<(string, string)> queue, string webPage) {
            if (conf.depth >= conf.maxDepth) {
                return;
            }

            List<string> links = conf.linkFinder(webPage);

            foreach (string link in links) {
                Uri? newWebAddr = RequestManager.FormatUri(link, conf.webAddr); // TODO: bitmap of visited pages
                if (newWebAddr == null) {
                    continue;
                }
                if (conf.stayOnDomain && !RequestManager.FromSameDomain(conf.webAddr, newWebAddr)) {
                    continue;
                }

                InternalSenqConf newConf = conf with { webAddr = newWebAddr,
                                                        depth = conf.depth + 1 };

                Task.Run(() => ScrapePage(newConf, queue));
                IncrementScrapeTasks();
            }
        }

        /// <summary>
        /// Safely increases the number of currently running scraping tasks.
        /// </summary>
        private void IncrementScrapeTasks() {
            lock (rm) {
                scrapeTasks++;
            }
        }

        /// <summary>
        /// Safely decrements the number of currently running scraping tasks.
        /// </summary>
        private void DecrementScrapeTasks() {
            lock (rm) {
                scrapeTasks--;
            }
        }

        /// <summary>
        /// Handles the output of scraped data from the queue and calls on it output method.
        /// </summary>
        /// <param name="queue">Queue containing data that should be given to the output method.</param>
        /// <param name="output">Method to handle the output.</param>
        public void OutputHandler(BlockingCollection<(string, string)> queue, Action<string, string> output) {
            foreach (var (webAddress, content) in queue.GetConsumingEnumerable()) {
                output(webAddress, content);
            }
        }
    }
}