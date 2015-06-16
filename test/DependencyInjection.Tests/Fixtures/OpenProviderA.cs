using Microsoft.Framework.DependencyInjection;
using System;

namespace DependencyInjection.Tests.Fixtures
{
    public interface IOpenProviderA<T,Y>
    {
    }

    [ServiceDescriptor(typeof(IOpenProviderA<,>), Lifetime = ServiceLifetime.Singleton)]
    public class OpenProviderA<T,Y> : IOpenProviderA<T,Y>
    {
    }
}
