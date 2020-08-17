using Fixer.HTTP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Fixer.LoadBalancing.Fabio.Http
{
    internal sealed class FabioHttpClient : FixerHttpClient, IFabioHttpClient
    {
        public FabioHttpClient(HttpClient client, HttpClientOptions options, ILogger<IHttpClient> logger)
            : base(client, options, logger)
        {
        }
    }
}
