using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Senq {

    public struct SenqConf {
        public readonly string webAddr { get; init; }
        public readonly string target { get; init; }
        public readonly Action<string, string> output { get; init; }
    }

    public class Scraper {
        RequestManager rm = new RequestManager();

        public static void Main(string[] args) {
            SenqConf conf = new SenqConf{ webAddr = "example.com", target = "example", output = CSVOut };

            Scraper scraper = new Scraper();
            scraper.Scrape(conf);
        }

        public void Scrape(SenqConf conf) {
            BlockingCollection<(string, string)> queue = new BlockingCollection<(string, string)>();

            Task.Factory.StartNew(() => {
                OutputHandler(queue, conf.output);
            }, TaskCreationOptions.LongRunning);

            Task.Run(() => ScrapePage(conf, queue));
        }

        private void ScrapePage(SenqConf conf, BlockingCollection<(string, string)> queue) {
            string webPage = rm.GET(conf.webAddr);

            HandleMatches(conf, queue, webPage);

            HandleLinks(conf, queue, webPage);

            List<string> webAddress = DataMiner.FindLinks(webPage);
        }

        private void HandleMatches(SenqConf conf, BlockingCollection<(string, string)> queue, string webPage) {
            List<string> matches = DataMiner.FindWithRegex(webPage, conf.target);

            foreach (string match in matches ) {
                queue.Add((conf.webAddr, match));
            }
        }

        private void HandleLinks(SenqConf conf, BlockingCollection<(string, string)> queue, string webPage) {
            List<string> links = DataMiner.FindWithRegex(webPage, conf.target);

            foreach (string link in links) {
                SenqConf newConf = conf with { webAddr = link };

                Task.Run(() => ScrapePage(newConf, queue));
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