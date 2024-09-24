using Server;
using Senq;


namespace senq.Tests {

    /// <summary>
    /// This tests are testing if the static part of the senq scraper is working correctly
    /// </summary>
    [Collection("HttpServerCollection")]
    public class StaticTests {

        /// <summary>
        /// Tests the scraping functionality with different input strings and expected matches,
        /// tests varies by the depth of the scraping and the regular expression used to identify targets
        /// </summary>
        /// <param name="expected">Expected output string in CSV format</param>
        /// <param name="maxDepth">Maximum depth for URL traversal during scraping</param>
        /// <param name="targetRegex">Regular expression used to identify target content in the HTML</param>
        [Theory]
        [InlineData(
            "http://localhost/index.html,lorem\n" +
            "http://localhost/index.html,Lorem\n",
            0, 
            @"(?i)(?<target>lorem)"
        )]
        [InlineData(
            "http://localhost/index.html,lorem\n" +
            "http://localhost/index.html,Lorem\n" +
            "http://localhost/lorem.html,LOREM\n" +
            "http://localhost/lorem.html,LOREM\n",
            1, 
            @"(?i)(?<target>lorem)"
        )]
        [InlineData(
            "http://localhost/index.html,lorem\n" +
            "http://localhost/index.html,Lorem\n" +
            "http://localhost/lorem.html,LOREM\n" +
            "http://localhost/lorem.html,LOREM\n",
            5, 
            @"(?i)(?<target>lorem)"
        )]
        [InlineData(
            "http://localhost/index.html,content\n" +
            "http://localhost/statistiky.html,content\n" +
            "http://localhost/tucniak_obrovsky.html,content\n" +
            "http://localhost/lorem.html,EGO\n" +
            "http://localhost/lorem.html,EGO\n" +
            "http://localhost/lorem.html,EGO\n" +
            "http://localhost/lorem.html,EGO\n",
            1, 
            @"(?i)(?<target>content|ego)"
        )]
        public void StaticScraping_String_WithMatches(string expected, int maxDepth, string targetRegex) {
            // Arrange
            string result = "";
            var conf = new SenqConf {
                webAddr = "http://localhost/index.html",
                targetRegex = targetRegex,
                useHostAddress = true,
                output = Output.CSVString.GetWriter(str => { result = str; }),
                maxDepth = maxDepth,
                stayOnDomain = true,
            };
            Scraper scraper = new Scraper();

            // Act
            scraper.Scrape(conf).GetAwaiter().GetResult();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests the scraper's functionality in case where no matches are found based on the provided regex
        /// and ensures that the scraper returns empty result when no content matches the target regex
        /// </summary>
        [Fact]
        public void StaticScraping_String_NoMatches() {
            // Arrange
            string expected = "";

            string result = "";
            var conf = new SenqConf {
                webAddr = "http://localhost/index.html",
                targetRegex = @"(?<target>AAAAAAAAA)",
                useHostAddress = true,
                output = Output.CSVString.GetWriter(str => { result = str; }),
                maxDepth = 1,
                stayOnDomain = true,
            };
            
            Scraper scraper = new Scraper();

            // Act
            scraper.Scrape(conf).GetAwaiter().GetResult();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests the scraper's functionality to detect specific JavaScript-related content
        /// and ensures that the scraper can't identify dynamically loaded content from JavaScript
        /// </summary>
        [Fact]
        public void StaticScraping_JS() {
            // Arrange
            string expected = "";

            string result = "";
            var conf = new SenqConf {
                webAddr = "http://localhost/js.html",
                targetRegex = @"(?<target>JS_LOADED)",
                useHostAddress = true,
                output = Output.CSVString.GetWriter(str => { result = str; }),
                maxDepth = 0,
                stayOnDomain = true,
            };
            
            Scraper scraper = new Scraper();

            // Act
            scraper.Scrape(conf).GetAwaiter().GetResult();

            // Assert
            Assert.Equal(expected, result);
        }
    }
}