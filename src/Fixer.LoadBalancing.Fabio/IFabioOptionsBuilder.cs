using System;
using System.Collections.Generic;
using System.Text;

namespace Fixer.LoadBalancing.Fabio
{
    public interface IFabioOptionsBuilder
    {
        IFabioOptionsBuilder Enable(bool enabled);
        IFabioOptionsBuilder WithUrl(string url);
        IFabioOptionsBuilder WithService(string service);
        FabioOptions Build();
    }
}
