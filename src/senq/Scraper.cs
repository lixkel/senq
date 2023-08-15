using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

#nullable enable

namespace Senq {

    public class SenqConf {
        public string webAddr { get; init; }
        public string targetRegex { get; init; }
        public Action<string, string> output { get; init; }
        public Func<string, List<string>> linkFinder { get; init; } = DataMiner.FindLinks;
        public int maxDepth { get; init; }
    }

    internal struct InternalSenqConf {
        public Regex regex { get; init; }
        public int depth {get; init; } = 0;
        public string webAddr { get; init; }
        public Action<string, string> output { get; init; }
        public Func<string, List<string>> linkFinder { get; init; }
        public int maxDepth { get; init; }

        public InternalSenqConf(SenqConf originalConf) {
            output = originalConf.output;
            webAddr = originalConf.webAddr;
            maxDepth = originalConf.maxDepth;
            linkFinder = originalConf.linkFinder;

            regex = new Regex(originalConf.targetRegex);
        }

        public InternalSenqConf(CLISenqConf originalConf) {
            webAddr = originalConf.webAddr;
            maxDepth = originalConf.maxDepth;
            linkFinder = DataMiner.FindLinks;

            switch (originalConf.outputType) {
                case OutputType.csv:
                    if (originalConf.outputFile == null) {
                        output = Output.CSVOut;
                        break;
                    }
                    Output.CSVWriter writer = new Output.CSVWriter(originalConf.outputFile);
                    output = writer.Write;
                    break;

                case OutputType.db:
                    output = Output.db;
                    break;
            }

            regex = new Regex(originalConf.targetRegex);
        }
    }


    public class Scraper {
        private RequestManager rm = new RequestManager();
        private int scrapeTasks = 0;

        public void Scrape(SenqConf conf) {
            InternalSenqConf internalConf = new InternalSenqConf(conf);

            Scrape(internalConf);
        }

        internal void Scrape(CLISenqConf conf) {
            InternalSenqConf internalConf = new InternalSenqConf(conf);

            Scrape(internalConf);
        }

        private void Scrape(InternalSenqConf conf) {
            BlockingCollection<(string, string)> queue = new BlockingCollection<(string, string)>();

            var outputTask = Task.Factory.StartNew(() => {
                OutputHandler(queue, conf.output);
            }, TaskCreationOptions.LongRunning);

            Task.Run(() => ScrapePage(conf, queue));
            IncrementScrapeTasks();

            outputTask.Wait();
        }

        private void ScrapePage(InternalSenqConf conf, BlockingCollection<(string, string)> queue) {
            string webPage = rm.GET(conf.webAddr);

            HandleMatches(conf, queue, webPage);

            HandleLinks(conf, queue, webPage);

            DecrementScrapeTasks();
        }

        private void HandleMatches(InternalSenqConf conf, BlockingCollection<(string, string)> queue, string webPage) {
            List<string> matches = DataMiner.FindAll(webPage, conf.regex);

            foreach (string match in matches ) {
                queue.Add((conf.webAddr, match));
            }
        }

        private void HandleLinks(InternalSenqConf conf, BlockingCollection<(string, string)> queue, string webPage) {
            if (conf.depth >= conf.maxDepth) {
                return;
            }

            List<string> links = conf.linkFinder(webPage);

            foreach (string link in links) {
                InternalSenqConf newConf = conf with { webAddr = link };

                Task.Run(() => ScrapePage(newConf, queue));
                IncrementScrapeTasks();
            }
        }

        private void IncrementScrapeTasks() {
            lock (rm) {
                scrapeTasks++;
            }
        }

        private void DecrementScrapeTasks() {
            lock (rm) {
                scrapeTasks--;
            }
        }

        public void OutputHandler(BlockingCollection<(string, string)> queue, Action<string, string> output) {
            foreach (var (webAddress, content) in queue.GetConsumingEnumerable()) {
                output(webAddress, content);
                if (scrapeTasks == 0 && queue.Count == 0) {
                    return;
                }
            }
        }
    }
}