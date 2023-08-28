using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Senq {

    public class DataMiner {
        static Regex linkRegex = new Regex("<a\\s+(?:[^>]*?\\s+)?href=([\"])(?<target>.*?)\\1");

        public static List<string> FindAll(string input, Regex regex) {
            MatchCollection matches = regex.Matches(input);
    
            List<string> matchList = new List<string>();
            foreach (Match match in matches) {
                matchList.Add(match.Groups["target"].Value);
            }

            return matchList;
        }

        public static List<string> FindWithRegex(string input, string pattern) {
            try {
                Regex regex = new Regex(pattern);
                return FindAll(input, regex);
            }
            catch (ArgumentException) {
                Console.WriteLine($"Invalid pattern: {pattern}");
                throw;
            }
        }

        public static List<string> FindLinks(string input) {
            return FindAll(input, linkRegex); 
        }
    }
}