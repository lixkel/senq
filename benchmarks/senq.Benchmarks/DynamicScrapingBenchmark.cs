using BenchmarkDotNet.Attributes;
using Senq;

namespace Benchmarks {
    public class DynamicBenchmarks {

        /// <summary>
        /// Perform benchmark measuring the performance of dynamic scraping using puppeteer.
        /// Where the website is served by a local http server.
        /// </summary>
        [Benchmark]
        public void DynamicBenchmark() {

        string result = "";

        var conf = new SenqConf  {
            webAddr = "http://localhost/index.html",
            targetRegex = @"(?<target>spyware)",
            useHostAddress = true,
            output = Output.CSVString.GetWriter(str => { result = str; }),
            webHandlerFactory = () => new PuppeteerManager(),
            maxDepth = 1,
            stayOnDomain = true,
        };

        Scraper scraper = new Scraper();
        scraper.Scrape(conf).GetAwaiter().GetResult();
        }
    }
}
