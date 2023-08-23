using System;
using System.Linq;
using System.Collections.Generic;

using CommandLine;
using CommandLine.Text;

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

        [Option('o', "outputType", Default = OutputType.csv, HelpText = "Choose the output type: CSV or Database.")]
        public OutputType outputType { get; set; }

        [Option('f', "outputFile", HelpText = "Output file location.")]
        public string? outputFile { get; set; } = null;

        [Option('d', "dbString", HelpText = "Database connection string.")]
        public string? dbString { get; set; } = null;

        [Option('t', "threads", HelpText = "Number of threads to use for multithreaded scraping.")]
        public int? threadCount { get; set; } = null;

        [Option('m', "maxDepth", HelpText = "Maximum depth of following links.")]
        public int maxDepth { get; set; } = 1;
    }

    public static class CLI {

        public static void Main(string[] args) {
            Parser parser = new Parser();

            var parserResult = parser.ParseArguments<CLISenqConf>(args);

            parserResult.WithParsed(conf => { 
                HandleSuccessfulParse(conf);
            })
            .WithNotParsed((errs) =>  {
                HandleParseErrors(parserResult, errs);
            });
        }

        private static void HandleSuccessfulParse(CLISenqConf conf) {
            if (conf.outputType == OutputType.db && string.IsNullOrEmpty(conf.dbString)) {
                Console.WriteLine("Error: If you specify the 'outputType' option as 'db' (database), you must also provide a database connection string using the 'dbString' option.");
                return;
            }

            Scraper scraper = new Scraper();
                
            scraper.Scrape(conf);
        }

        private static void HandleParseErrors<T>(ParserResult<T> result, IEnumerable<Error> errs) {

            // Check if help or version was requested, this library treats help nad version flags as errors
            if (errs.IsVersion() || errs.IsHelp())
            {
                var helpText = HelpText.AutoBuild(result);
                Console.WriteLine(helpText);
                return;
            }

            // Display custom messages for error types
            foreach (var error in errs) {
                switch (error) {
                    case NamedError namedError when error is BadFormatTokenError:
                        Console.WriteLine($"Token '{namedError.NameInfo.NameText}' is not in the correct format.");
                        break;
                    case NamedError namedError when error is MissingRequiredOptionError:
                        Console.WriteLine($"Option '{namedError.NameInfo.NameText}' is required.");
                        break;
                    case NamedError namedError when error is MissingValueOptionError:
                        Console.WriteLine($"Option '{namedError.NameInfo.NameText}' requires a value.");
                        break;
                    case NamedError namedError when error is UnknownOptionError:
                        Console.WriteLine($"Unknown option '{namedError.NameInfo.NameText}'.");
                        break;
                    case NamedError namedError when error is BadVerbSelectedError:
                        Console.WriteLine($"Unknown command '{namedError.NameInfo.NameText}'.");
                        break;
                    case NamedError namedError when error is BadFormatConversionError:
                        Console.WriteLine($"Cannot convert option '{namedError.NameInfo.NameText}' to the required type.");
                        break;
                }
            }
        }
    }
}