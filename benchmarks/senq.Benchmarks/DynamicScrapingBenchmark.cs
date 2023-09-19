using BenchmarkDotNet.Attributes;
using Senq;

namespace Benchmarks {
    public class DynamicBenchmarks {
        [Benchmark]
        public void DynamicBenchmark() {

        string result = "";
            
        var conf = new SenqConf  {
            webAddr = "http://localhost/",
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
