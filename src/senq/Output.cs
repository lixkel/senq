using System;
using System.Text;
using System.Data.SqlClient;
using System.Collections.Generic;


namespace Senq {

    /// <summary>
    /// Collection of various output methods and classes usable as output method in scraper.
    /// </summary>
    public static class Output {

        /// <summary>
        /// If the delegate refers to an instance method, dispose of the instance.
        /// </summary>
        /// <param name="action">Delegate to be checked.</param>
        public static void DisposeIfIsDisposable(Action<string, string> action) {
            // Check if delegate is instance method (has target)
            if (action.Target != null) {
                // Check if the target is IDisposable
                if (action.Target is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// Outputs the scraped content in CSV format to the console.
        /// </summary>
        /// <param name="webAddress">Web address of the page where the wanted content was found.</param>
        /// <param name="content">Actual scraped content.</param>
        public static void CSVOut(string webAddress, string content) {
            Console.WriteLine($"{webAddress},{content}");
        }

        /// <summary>
        /// Represents a writer that takes reference to string and writes to it.
        /// </summary>
        public class CSVString : IDisposable {
            private Action<string> updateString;

            private StringBuilder builder;

            /// <summary>
            /// Initializes a new instance of <see cref="CSVString"/> class.
            /// </summary>
            /// <param name="outString">Reference to string where the output should be written.</param>
            public CSVString(Action<string> outString) {
                builder = new StringBuilder();
                updateString = outString;
            }

            public static Action<string, string> GetWriter(Action<string> outString) {
                var newClass = new CSVString(outString);
                return newClass.Write;
            }

            /// <summary>
            /// Appends the scraped content into string. This is the method you want to pass as output method to scraper.
            /// </summary>
            /// <param name="webAddress">Web address of the scraped content.</param>
            /// <param name="content">Actual scraped content.</param>
            public void Write(string webAddress, string content) {
                 builder.AppendLine($"{webAddress},{content}");
            }

            /// <summary>
            /// Output to the string reference from constructor.
            /// </summary>
            public void Dispose() {
                updateString(builder.ToString());
            }
        }

        /// <summary>
        /// Represents a writer that outputs the scraped content to a CSV file.
        /// </summary>
        public class CSVFileWriter : IDisposable {
            private StreamWriter writer;
            private string filePath;

            /// <summary>
            /// Initializes a new instance of <see cref="CSVFileWriter"/> class.
            /// </summary>
            /// <param name="filePath">Path to the output CSV file.</param>
            public CSVFileWriter(string filePath = "output.csv") {
                filePath = filePath;
                writer = new StreamWriter(filePath, true); // true represents appending to file
            }

            public static Action<string, string> GetWriter(string filePath = "output.csv") {
                var newClass = new CSVFileWriter(filePath);
                return newClass.Write;
            }

            /// <summary>
            /// Writes the scraped content to the CSV file. This is the method you want to pass as output method to scraper.
            /// </summary>
            /// <param name="webAddress">Web address of the scraped content.</param>
            /// <param name="content">Actual scraped content.</param>
            public void Write(string webAddress, string content) {
                writer.WriteLine($"{webAddress},{content}");
            }

            /// <summary>
            /// Disposes of the writer and releases all its resources.
            /// </summary>
            public void Dispose() {
                writer?.Flush();
                writer?.Close();
                writer?.Dispose();
            }
        }

        /// <summary>
        /// Represents a writer that outputs the scraped content to a database.
        /// </summary>
        public class DatabaseWriter : IDisposable {
            private SqlConnection connection;
            private string connectionString;
            private string tableName;

            /// <summary>
            /// Initializes a new instance of <see cref="DatabaseWriter"/> class.
            /// </summary>
            /// <param name="conString">Connection string to the database.</param>
            /// <param name="newTableName">Name of the table to which output will be written. If the table doesn't exist it will be created.</param>
            public DatabaseWriter(string conString, string newTableName = "NONE") {
                connectionString = conString;
                tableName = newTableName;

                connection = new SqlConnection(connectionString);
                connection.Open();

                string checkTableQuery = $"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}') " +
                                        $"CREATE TABLE {tableName} (WebAddress NVARCHAR(500), Content NVARCHAR(MAX))";

                using (SqlCommand cmd = new SqlCommand(checkTableQuery, connection)) {
                    cmd.ExecuteNonQuery();
                }
            }

            public static Action<string, string> GetWriter(string conString, string newTableName = "NONE") {
                var newClass = new DatabaseWriter(conString, newTableName);
                return newClass.Write;
            }

            /// <summary>
            /// Writes the scraped content to the database table. This is the method you want to pass as output method to scraper.
            /// </summary>
            /// <param name="webAddress">Web address of the scraped content.</param>
            /// <param name="content">Actual scraped content.</param>

            public void Write(string webAddress, string content) {
                string insertQuery = $"INSERT INTO {tableName} (WebAddress, Content) VALUES (@WebAddress, @Content)";
                using (SqlCommand cmd = new SqlCommand(insertQuery, connection)) {
                    cmd.Parameters.AddWithValue("@WebAddress", webAddress);
                    cmd.Parameters.AddWithValue("@Content", content);
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// Disposes of the database connection and releases all its resources.
            /// </summary>
            public void Dispose() {
                connection?.Close();
                connection?.Dispose();
            }
        }
    }
}