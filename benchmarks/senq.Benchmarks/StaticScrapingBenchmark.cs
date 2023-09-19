using BenchmarkDotNet.Attributes;
using Senq;

namespace Benchmarks {
    public class StaticBenchmarks {
        [Benchmark]
        public void StaticBenchmark() {

        string result = "";
            
        var conf = new SenqConf  {
            webAddr = "http://localhost/",
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
