using Server;
using Senq;

namespace senq.Tests {

    /// <summary>
    /// Context collection fixture to share the same webserver instance across all tests
    /// so that the instances dont have to be created and destroyed for each test class
    /// </summary>
    [CollectionDefinition("HttpServerCollection")]
    public class HttpServerCollection : ICollectionFixture<HttpServer> {
    }
}