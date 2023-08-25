using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Senq {

    public class RequestManager {
        static readonly HttpClient client = new HttpClient();
        // I should try looking for alternatives
        private readonly ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));
        private static int seed = Environment.TickCount;
        private List<String> userAgents = new List<string> {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/116.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 13.5; rv:109.0) Gecko/20100101 Firefox/116.0",
            "Mozilla/5.0 (X11; Linux i686; rv:109.0) Gecko/20100101 Firefox/116.0"
        };

        public string GET(string webAddr) {
            try {
                var request = new HttpRequestMessage(HttpMethod.Get, webAddr);
                request.Headers.UserAgent.ParseAdd(GetRandomUserAgent());

                using (HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult()) {
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
            }
            catch (HttpRequestException e) { // This exception is thrown by EnsureSuccessStatusCode
                Console.WriteLine("\nError: {0}", e.Message);
                return "";
            }
            catch (TaskCanceledException e) { // This is thrown when the request times out
                Console.WriteLine("\nRequest timed out: {0}", e.Message);
                return "";
            }
            catch (Exception e) {
                //Console.WriteLine(e);
                return"";
            }
        }

        public void changeUserAgents(List<string> newUserAgents) {
            userAgents = newUserAgents;
        }

        private string GetRandomUserAgent() {
            int index = threadLocalRandom.Value.Next(userAgents.Count);
            return userAgents[index];
        }
    }
}