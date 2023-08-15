using System;
using System.Collections.Generic;

using CommandLine;

#nullable enable

namespace Senq {

    public enum ScrapingMode {
        d,      // Directly search data in downloaded web page.
        js      // Search data after executing JavaScript with Google V8 engine.
    }

    public enum OutputType {
        csv,        // CSV format of output data
        db          // Output to database
    }

    [Verb("scrape", HelpText = "Scrape web content based on provided configuration.")]
    internal class CLISenqConf {
        [Option('w', "webAddr", Required = true, HelpText = "Address of the webpage to scrape.")]
        public string webAddr { get; set; }

        [Option('r', "regex", Required = true, HelpText = "Regex pattern to search for specific data within the web page.")]
        public string targetRegex { get; set; }

        [Option('m', "mode", Default = ScrapingMode.d, HelpText = "Mode of scraping: Direct for HTTP requests or JavaScript for executing scripts with Google V8 engine.")]
        public ScrapingMode mode { get; set; }

        [Option('p', "proxy", HelpText = "Proxy server address to use for scraping. This helps in hiding IP and avoiding website restrictions.")]
        public string? proxy { get; set; } = null;

        [Option('a', "userAgent", HelpText = "User agent string to use for requests. The tool supports user agent rotation if a list is provided.")]
        public string? userAgent { get; set; } = null;

        [Option('o', "filePath", Default = OutputType.csv, HelpText = "Choose the output type: CSV or Database.")]
        public OutputType outputType { get; set; }

        [Option('f', "outputFile", HelpText = "Output file location.")]
        public string? outputFile { get; set; } = null;

        [Option('t', "threads", HelpText = "Number of threads to use for multithreaded scraping.")]
        public int? threadCount { get; set; } = null;

        [Option('d', "maxDepth", HelpText = "Maximum depth of following links.")]
        public int maxDepth { get; set; }
    }

    public static class CLI {

        public static void Main(string[] args) {
            Parser parser = new Parser();

            var parserResult = parser.ParseArguments<CLISenqConf>(args);

            parserResult.WithParsed(conf => { 
                Scraper scraper = new Scraper();
                
                scraper.Scrape(conf);
            })
            .WithNotParsed((errs) =>  {
                foreach (var error in errs) {
                    Console.WriteLine($"Failed to parse option '{error.Tag}'");
                }
            });
        }
    }
}