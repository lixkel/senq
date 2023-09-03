using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Senq {

    /// <summary>
    /// Defines contract for web handler that can fetch websites (optionally also performing scripts),
    /// change user agents and use proxies.
    /// </summary>
    public interface IWebHandler {

        /// <summary>
        /// Fetches website and optionally performs
        /// </summary>
        /// <param name="webAddr">URI of the web page which should be fetched.</param>
        /// <returns>HTML document inside string.</returns>
        public Task<string> GET(Uri webAddr);

        /// <summary>
        /// Changes the user agents used for sending web requests.
        /// </summary>
        /// <param name="newUserAgents">The list of new user agents to be used.</param>
        public void ChangeUserAgents(List<string> newUserAgents);

        /// <summary>
        /// Changes the proxies for sending web requests.
        /// </summary>
        /// <param name="proxyAddresses">List of proxy addresses to use. Pass null to disable proxy usage.</param>
        /// <param name="useHostAddress">If set to true host address will be also used for sending requests.</param>
        public void ChangeProxy(List<string>? proxyAddresses, bool useHostAddress);
    }
}