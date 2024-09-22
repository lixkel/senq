using Server;
using Senq;

namespace senq.Tests {

    /// <summary>
    /// This tests are testing if configuration of the senq scraper does not accept invalid configurations arguments
    /// </summary>
    [Collection("HttpServerCollection")]
    public class ConfigTests {

        /// <summary>
        /// Tests if providing an invalid web address as the starting point throws a BadStartingAddressException
        /// </summary>
        /// <param name="webAddr">The invalid web address to test</param>
        [Theory]
        [InlineData("&")]
        [InlineData("[]")]
        [InlineData("://example.com")]
        [InlineData("<invalid>http://example.com")]
        [InlineData("http://invalid space.com")]
        public void BadStartingAddress(string webAddr) {
            string result = "";

            var conf = new SenqConf  {
                webAddr = webAddr,
                targetRegex = "",
                useHostAddress = true,
                output = Output.CSVString.GetWriter(str => { result = str; }),
                maxDepth = 1,
                stayOnDomain = true,
            };
            
            Scraper scraper = new Scraper();
            Assert.Throws<BadStartingAddressException>(() => scraper.Scrape(conf).GetAwaiter().GetResult());
        }


        /// <summary>
        /// Tests if providing an invalid regex pattern throws a BadRegexException
        /// </summary>
        /// <param name="pattern">The invalid regex pattern to test</param>
        [Theory]
        [InlineData("[]")]
        [InlineData("\\")]
        [InlineData("(abc")]
        [InlineData("(?<name")]
        public void BadRegex(string pattern) {
            string result = "";

            var conf = new SenqConf  {
                webAddr = "http://localhost/index.html",
                targetRegex = pattern,
                useHostAddress = true,
                output = Output.CSVString.GetWriter(str => { result = str; }),
                maxDepth = 1,
                stayOnDomain = true,
            };
            
            Scraper scraper = new Scraper();
            Assert.Throws<BadRegexException>(() => scraper.Scrape(conf).GetAwaiter().GetResult());
        }
    }
}