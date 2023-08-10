using System;
using System.Collections.Generic;

namespace Senq {

    public struct SenqConf {
        public readonly string webAddr { get; init; }
        public readonly string target { get; init; }
        public readonly Action<string, string> output { get; init; }
    }

    public class Scraper {
        RequestManager rm = new RequestManager();

        public static void Main(string[] args) {
            SenqConf conf = new SenqConf{ webAddr = "example.com", target = "example", output = csvOut };
            Scraper scraper = new Scraper();
            scraper.scrape(conf);
        }

        public void scrape(SenqConf conf) {
            scrapePage(conf);
        }

        private void scrapePage(SenqConf conf) {
            string content = rm.GET(conf.webAddr);
            List<string> links = DataMiner.FindWithRegex(content, conf.target);
        }

        public static void csvOut(string webAddress, string content) {
            Console.WriteLine($"{webAddress},{content}");
        }
    }
}