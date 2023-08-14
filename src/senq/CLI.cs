using System;
using System.Collections.Generic;

using CommandLine;

namespace Senq {

    public enum ScrapingMode {
        d,      // Directly search data in downloaded web page.
        js   // Search data after executing JavaScript with Google V8 engine.
    }

    public enum OutputType {
        csv,        // CSV format of output data
        db          // Output to database
    }

    [Verb("scrape", HelpText = "Scrape web content based on provided configuration.")]
    internal struct CLISenqConf {
        [Option('w', "webAddr", Required = true, HelpText = "Address of the webpage to scrape.")]
        public string webAddr { get; set; }

        [Option('r', "regex", Required = true, HelpText = "Regex pattern to search for specific data within the web page.")]
        public string targetRegex { get; set; }

        [Option('m', "mode", Default = OutputType.d, HelpText = "Mode of scraping: Direct for HTTP requests or JavaScript for executing scripts with Google V8 engine.")]
        public ScrapingMode mode { get; set; }

        [Option('p', "proxy", HelpText = "Proxy server address to use for scraping. This helps in hiding IP and avoiding website restrictions.")]
        public string? proxy { get; set; } = Null;

        [Option('a', "userAgent", HelpText = "User agent string to use for requests. The tool supports user agent rotation if a list is provided.")]
        public string? userAgent { get; set; } = Null;

        [Option('o', "output", Default = OutputType.Csv, HelpText = "Choose the output type: CSV or Database.")]
        public OutputType output { get; set; }

        [Option('f', "outputFile", HelpText = "Output file location.")]
        public string? outputFile { get; set; } = Null;

        [Option('t', "threads", HelpText = "Number of threads to use for multithreaded scraping.")]
        public int? threadCount { get; set; } = Null;

        [Option('d', "maxDepth", helpText = "Maximum depth of following links.")]
        public int maxDepth { get; set; }
    }

    public static class CLI {

        public static void Main(string[] args) {
            var parserResult = parser.ParseArguments<CLISenqConf>(args);

            parserResult.WithParsed(conf => { 
                Scraper scraper = new Scraper();
                
                scraper.Scrape(conf);
            })
            .WithNotParsed((errs) =>  {
                foreach (var error in errs) {
                    Console.WriteLine($"Failed to parse option '{typeError.Tag}': {typeError.ErrorMessage}");
                }
            });
        }
    }
}