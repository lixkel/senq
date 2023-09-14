using System;
using System.Linq;
using System.Collections.Generic;

using CommandLine;
using CommandLine.Text;

#nullable enable

namespace Senq {

    /// <summary>
    /// Specifies the mode in which the scraper operates.
    /// </summary>
    public enum ScrapingMode {
        d,      // Directly search data in downloaded web page.
        js      // Search data after executing JavaScript with Google V8 engine.
    }

    /// <summary>
    /// Specifies how the data should be processed and outputed.
    /// </summary>
    public enum OutputType {
        csv,        // CSV format of output data, output to file or stdout
        db          // Output to database, connection string must be provided
    }

    /// <summary>
    /// Configuration for the command line interface of Senq scraper.
    /// </summary>
    [Verb("scrape", HelpText = "Scrape web content based on provided configuration.")] // TODO: should local address be used for requests
    internal class CLISenqConf {
        [Option('w', "webAddr", Required = true, HelpText = "Address of the webpage to scrape.")]
        public string webAddr { get; set; }

        [Option('r', "regex", Required = true, HelpText = "Regex pattern to search for specific data within the web page.")]
        public string targetRegex { get; set; }

        [Option('m', "mode", Default = ScrapingMode.d, HelpText = "Mode of scraping: Direct for HTTP requests or JavaScript for executing scripts with Google V8 engine.")]
        public ScrapingMode mode { get; set; } = ScrapingMode.d;

        [Option('p', "proxy", Separator = ',', HelpText = "Proxy server address to use for scraping. This helps in hiding IP and avoiding website restrictions.")]
        public IEnumerable<string>? proxyAddresses { get; set; } = null;

        [Option('h', "useHostAddress", HelpText = "Should the host address of this computer also be used for scraping?")]
        public bool useHostAddress { get; set; } = true;

        [Option('a', "userAgents", Separator = '&', HelpText = "User agent string to use for requests. The tool supports user agent rotation if a list is provided.")]
        public IEnumerable<string>? userAgents { get; set; } = new List<string>();

        [Option('o', "outputType", Default = OutputType.csv, HelpText = "Choose the output type: CSV or Database.")]
        public OutputType outputType { get; set; }

        [Option('f', "outputFile", HelpText = "Output file location.")]
        public string? outputFile { get; set; } = null;

        [Option('d', "dbString", HelpText = "Database connection string.")]
        public string? dbString { get; set; } = null;

        [Option('t', "threads", HelpText = "Number of threads to use for multithreaded scraping.")]
        public int? threadCount { get; set; } = null;

        [Option('x', "maxDepth", HelpText = "Maximum depth of following links.")]
        public int maxDepth { get; set; } = 0;

        [Option('s', "stayOnDomain", HelpText = "Can the scraper sleave starting domain?")]
        public bool stayOnDomain { get; set; } = false;
    }

    /// <summary>
    /// Command line interface for the Senq scraper application.
    /// </summary>
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

        /// <summary>
        /// Handles the scenario when the command line arguments were successfully parsed.
        /// </summary>
        /// <param name="conf">Parsed CLI configuration.</param>
        private static void HandleSuccessfulParse(CLISenqConf conf) {
            if (conf.outputType == OutputType.db && string.IsNullOrEmpty(conf.dbString)) {
                Console.Error.WriteLine("Error: If you specify the 'outputType' option as 'db' (database), you must also provide a database connection string using the 'dbString' option.");
                return;
            }

            Scraper scraper = new Scraper();
                
            try {
                scraper.Scrape(SenqConf.ToSenqConf(conf)).GetAwaiter().GetResult();
            }
            catch (BadStartingAddressException e) {
                Console.Error.WriteLine($"Error BadStartingAddressException: {e.Message}");
            }
            catch (NoWorkingClientsException e) {
                Console.Error.WriteLine($"Error NoWorkingClientsException: {e.Message}");
            }
            catch (NoConnectionException e) {
                Console.Error.WriteLine($"Error NoConnectionException: {e.Message}");
            }
        }

        /// <summary>
        /// Handles the scenario when there were errors parsing the command line arguments.
        /// </summary>
        /// <typeparam name="T">Type of the result being processed.</typeparam>
        /// <param name="result">Result of the command line arguments parsing.</param>
        /// <param name="errs">Collection of errors.</param>
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
                //Console.WriteLine($"Error Type: {error.Tag}");
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
                    case NamedError namedError when error is SetValueExceptionError:
                        Console.WriteLine($"Error setting the value for option '{namedError.NameInfo.NameText}'.");
                        break;
                }
            }
        }
    }
}