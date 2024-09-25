using Server;
using Senq;

namespace senq.Tests {

    /// <summary>
    /// This tests are testing if the output functions of senq scraper are working correctly
    /// </summary>
    [Collection("HttpServerCollection")]
    public class OutputTests {

        /// <summary>
        /// Test to check if the scraper correctly generates a CSV string output
        /// </summary>
        [Fact]
        public void CSVStringOutput() {
            // Arrange
            string result = "";
            string expected = "http://localhost/index.html,lorem\n" +
                              "http://localhost/index.html,Lorem\n";

            var conf = new SenqConf  {
                webAddr = "http://localhost/index.html",
                targetRegex = @"(?i)(?<target>lorem)",
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

        /// <summary>
        /// Test to check if the scraper correctly writes output to the standard output
        /// </summary>
        [Fact]
        public void stdoutOutput() {
            // Arrange
            string result = "";

            // Save the original stdout
            var originalStdOut = Console.Out;

            // Set stdout to stringWriter
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            var conf = new SenqConf
            {
                webAddr = "http://localhost/index.html",
                targetRegex = @"(?i)(?<target>lorem)",
                useHostAddress = true,
                output = Output.CSVOut,
                maxDepth = 0,
                stayOnDomain = true,
            };
            Scraper scraper = new Scraper();

            // Act
            scraper.Scrape(conf).GetAwaiter().GetResult();

            // Capture the output
            result = stringWriter.ToString().Trim();

            // Restore stdout
            Console.SetOut(originalStdOut);

            // Assert
            Assert.Contains("http://localhost/index.html,lorem", result);
            Assert.Contains("http://localhost/index.html,Lorem", result);
        }

        /// <summary>
        /// Test to check if the scraper correctly writes output to a CSV file
        /// </summary>
        [Fact]
        public void CSVFileOutput() {
            // Arrange
            string result = "";
            string expected = "http://localhost/index.html,lorem\n" +
                              "http://localhost/index.html,Lorem\n";
            
            string outputFile = Path.GetTempFileName();

            var conf = new SenqConf  {
                webAddr = "http://localhost/index.html",
                targetRegex = @"(?i)(?<target>lorem)",
                useHostAddress = true,
                output = Output.CSVFileWriter.GetWriter(outputFile),
                maxDepth = 0,
                stayOnDomain = true,
            };
            
            Scraper scraper = new Scraper();

            // Act
            scraper.Scrape(conf).GetAwaiter().GetResult();

            // Read all contents of the output file into the result string
            result = File.ReadAllText(outputFile);

            // Assert
            Assert.Equal(expected, result);

            // Cleanup
            // Delete the output file
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
        }
    }
}