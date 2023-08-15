using System;
using System.Collections.Generic;


namespace Senq {

    public static class Output {

        public static void db(string webAddress, string content) {
            return;
        }

        public static void CSVOut(string webAddress, string content) {
            Console.WriteLine($"{webAddress},{content}");
        }

        public class CSVWriter : IDisposable {
            private StreamWriter writer;
            private string filePath;

            public CSVWriter(string filePath = "output.csv") {
                filePath = filePath;
                writer = new StreamWriter(filePath, true); // true means appending to file
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
    }
}