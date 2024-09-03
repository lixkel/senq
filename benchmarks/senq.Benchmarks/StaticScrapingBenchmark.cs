using BenchmarkDotNet.Attributes;
using Senq;

namespace Benchmarks {
    public class StaticBenchmarks {

        /// <summary>
        /// Perform benchmark measuring the performance of static scraping using http request.
        /// Where the website is served by a local http server.
        /// </summary>
        [Benchmark]
        public void StaticBenchmark() {

        string result = "";

        var conf = new SenqConf  {
            webAddr = "http://localhost/index.html",
            targetRegex = @"(?<target>spyware)",
            useHostAddress = true,
            output = Output.CSVString.GetWriter(str => { result = str; }),
            maxDepth = 1,
            stayOnDomain = true,
        };

        Scraper scraper = new Scraper();
        scraper.Scrape(conf).GetAwaiter().GetResult();
        }
    }
}
