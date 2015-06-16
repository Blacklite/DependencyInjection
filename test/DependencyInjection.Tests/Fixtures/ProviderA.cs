using Microsoft.Framework.DependencyInjection;
using System;

namespace DependencyInjection.Tests.Fixtures
{
    public interface IProviderA
    {
        decimal GetValue();
    }

    [ServiceDescriptor(typeof(IProviderA), Lifetime = ServiceLifetime.Singleton)]
    public class ProviderA : IProviderA
    {
        public decimal GetValue()
        {
            return 9000.99M;
        }
    }
}
