using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Senq {

    /// <summary>
    /// Collection utilities for extracting data from strings and using regular expressions.
    /// </summary>
    public class DataMiner {

        /// <summary>
        /// Regex to match HTML links in a string.
        /// </summary>
        static Regex linkRegex = new Regex("<a\\s+(?:[^>]*?\\s+)?href=([\"])(?<target>.*?)\\1");

        /// <summary>
        /// Searches an input string for all occurrences that match the provided regex and returns all found "target" groups.
        /// </summary>
        /// <param name="input">Input string to search.</param>
        /// <param name="regex">Regex used for searching.</param>
        /// <returns>List containing all matches of the "target" group found.</returns>
        public static List<string> FindAll(string input, Regex regex) {
            MatchCollection matches = regex.Matches(input);
    
            List<string> matchList = new List<string>();
            foreach (Match match in matches) {
                matchList.Add(match.Groups["target"].Value);
            }

            return matchList;
        }

        /// <summary>
        /// Verifies regex pattern and searches an input for all occurrences that match the provided pattern.
        /// </summary>
        /// <param name="input">Input string to search.</param>
        /// <param name="pattern">Regex pattern used for searching.</param>
        /// <returns>List containing all matches found.</returns>
        /// <exception cref="ArgumentException">Thrown if the provided pattern is not a valid regex.</exception>

        public static List<string> FindWithRegex(string input, string pattern) { // TODO: custom error and seperate function for validation
            try {
                Regex regex = new Regex(pattern);
                return FindAll(input, regex);
            }
            catch (ArgumentException) {
                // Catch any issues with the provided regex pattern
                Console.WriteLine($"Invalid pattern: {pattern}");
                throw;
            }
        }

        /// <summary>
        /// Searches an input string for all HTML link occurrences.
        /// </summary>
        /// <param name="input">The input string to search.</param>
        /// <returns>List containing all link matches found.</returns>
        public static List<string> FindLinks(string input) {
            return FindAll(input, linkRegex); 
        }
    }
}