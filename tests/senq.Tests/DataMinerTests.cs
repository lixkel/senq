using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Senq;

namespace senq.Tests {

    /// <summary>
    /// This tests are testing if the text finding functions in the DataMiner class are working correctly
    /// </summary>
     public class DataMinerTests {

        /// <summary>
        /// Tests that FindAll method returns the correct list of email matches 
        /// when provided with a valid input string containing emails
        /// </summary>
        [Fact]
        public void FindAll_ReturnsCorrectMatches() {
            // Arrange
            string input = "Contact us at contact@example.com or support@test.com.";
            Regex regex = new Regex("(?<target>[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,})");
            List<string> expected = new List<string> { "contact@example.com", "support@test.com" };

            // Act
            List<string> result = DataMiner.FindAll(input, regex);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that FindWithRegex method returns the correct list of email matches
        /// when using a specified regex pattern
        /// </summary>
        [Fact]
        public void FindWithRegex_ReturnsCorrectMatches() {
            // Arrange
            string input = "Emails: person1@example.com, person2@test.org";
            string pattern = "(?<target>[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,})";
            List<string> expected = new List<string> { "person1@example.com", "person2@test.org" };

            // Act
            List<string> result = DataMiner.FindWithRegex(input, pattern);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that FindAll method returns an empty list when no matches are found
        /// in the input string
        /// </summary>
        [Fact]
        public void FindAll_NoMatches() {
            // Arrange
            string input = "No emails here!";
            Regex regex = new Regex("(?<target>[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,})");
            List<string> expected = new List<string> ();

            // Act
            List<string> result = DataMiner.FindAll(input, regex);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that FindWithRegex method returns an empty list when no matches
        /// are found using the specified regex pattern
        /// </summary>
        [Fact]
        public void FindWithRegex_NoMatches() {
            // Arrange
            string input = "No emails here!";
            string pattern = "(?<target>[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,})";
            List<string> expected = new List<string> ();

            // Act
            List<string> result = DataMiner.FindWithRegex(input, pattern);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that FindLinks method correctly extracts and returns a list of 
        /// URL links from the given input string containing HTML anchor tags
        /// </summary>
        [Fact]
        public void FindLinks_ReturnsCorrectMatches() {
            // Arrange
            string input = "Some HTML content with links: <a href=\"http://example.com\">example</a> and <a href=\"http://test.com\">test</a>";
            List<string> expected = new List<string> { "http://example.com", "http://test.com" };

            // Act
            List<string> result = DataMiner.FindLinks(input);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that FindLinks method returns an empty list when no HTML anchor
        /// tags with links are present in the input string.
        /// </summary>
        [Fact]
        public void FindLinks_ReturnsEmptyList() {
            // Arrange
            string input = "No HTML links here!";

            // Act
            List<string> result = DataMiner.FindLinks(input);

            // Assert
            Assert.Empty(result);
        }
    }
}