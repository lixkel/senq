using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Senq {

    public struct SenqConf {
        public string webAddr { get; init; }
        public string target { get; init; }
        public Action<string, string> output { get; init; }
        public int maxDepth { get; init; }
    }

    internal struct InternalSenqConf {
        private SenqConf _originalConf;
        public Regex regex { get; }
        public int depth {get; set; } = 0;
        public string webAddr { get; init; }
        public Action<string, string> output => _originalConf.output;
        public int maxDepth => _originalConf.maxDepth;

        public InternalSenqConf(SenqConf originalConf) {
            _originalConf = originalConf;
            webAddr = _originalConf.webAddr;
            regex = new Regex(originalConf.target);
        }
    }


    public class Scraper {
        private RequestManager rm = new RequestManager();
        private readonly ConcurrentBag<Task> allTasks = new ConcurrentBag<Task>();

        public static void Main(string[] args) {
            SenqConf conf = new SenqConf{ webAddr = "example.com", target = "example", output = CSVOut, maxDepth = 0 };

            Scraper scraper = new Scraper();
            scraper.Scrape(conf);
        }

        public void Scrape(SenqConf conf) {
            BlockingCollection<(string, string)> queue = new BlockingCollection<(string, string)>();

            InternalSenqConf internalConf = new InternalSenqConf(conf);

            Task.Factory.StartNew(() => {
                OutputHandler(queue, conf.output);
            }, TaskCreationOptions.LongRunning);

            var task = Task.Run(() => ScrapePage(internalConf, queue));
            allTasks.Add(task);

            Task.WhenAll(allTasks);
        }

        private void ScrapePage(InternalSenqConf conf, BlockingCollection<(string, string)> queue) {
            string webPage = rm.GET(conf.webAddr);

            HandleMatches(conf, queue, webPage);

            HandleLinks(conf, queue, webPage);

            List<string> webAddress = DataMiner.FindLinks(webPage);
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

            List<string> links = DataMiner.FindLinks(webPage);

            foreach (string link in links) {
                InternalSenqConf newConf = conf with { webAddr = link };

                var task = Task.Run(() => ScrapePage(newConf, queue));
                allTasks.Add(task);
            }
        }

        public static void OutputHandler(BlockingCollection<(string, string)> queue, Action<string, string> output) {
            foreach (var (webAddress, content) in queue.GetConsumingEnumerable()) {
                output(webAddress, content);
            }
        }

        public static void CSVOut(string webAddress, string content) {
            Console.WriteLine($"{webAddress},{content}");
        }
    }
}