using Server;
using Senq;

namespace senq.Tests {

    /// <summary>
    /// This tests are testing if the static part of the senq scraper is working correctly
    /// </summary>
    [Collection("HttpServerCollection")]
    public class StaticTests {

        [Fact]
        public void Static() {
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