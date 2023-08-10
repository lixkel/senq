using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Senq {

    public class RequestManager {
        static readonly HttpClient client = new HttpClient();

        public string GET(string webAddr) {
            try {
                HttpResponseMessage response = client.GetAsync(webAddr).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                Console.WriteLine(responseBody);
                return responseBody;
            }
            catch(HttpRequestException e) {
                Console.WriteLine("\nError: {0} ", e.Message);
            }
        }
    }
}