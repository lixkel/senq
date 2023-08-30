using System;
using System.Data.SqlClient;
using System.Collections.Generic;


namespace Senq {

    public static class Output {

        public static void CSVOut(string webAddress, string content) {
            Console.WriteLine($"{webAddress},{content}");
        }

        public class CSVWriter : IDisposable {
            private StreamWriter writer;
            private string filePath;

            public CSVWriter(string filePath = "output.csv") {
                filePath = filePath;
                writer = new StreamWriter(filePath, true); // true represents appending to file
            }

            public void Write(string webAddress, string content) {
                writer.WriteLine($"{webAddress},{content}");
            }

            public void Dispose() {
                writer?.Flush();
                writer?.Close();
                writer?.Dispose();
            }
        }

        public class DatabaseWriter : IDisposable {
            private SqlConnection connection;
            private string connectionString;
            private string tableName;

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

            public void Write(string webAddress, string content) {
                string insertQuery = $"INSERT INTO {tableName} (WebAddress, Content) VALUES (@WebAddress, @Content)";
                using (SqlCommand cmd = new SqlCommand(insertQuery, connection)) {
                    cmd.Parameters.AddWithValue("@WebAddress", webAddress);
                    cmd.Parameters.AddWithValue("@Content", content);
                    cmd.ExecuteNonQuery();
                }
            }

            public void Dispose() {
                connection?.Close();
                connection?.Dispose();
            }
        }
    }
}