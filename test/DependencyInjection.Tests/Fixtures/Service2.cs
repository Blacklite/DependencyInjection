using Microsoft.Framework.DependencyInjection;
using System;

namespace DependencyInjection.Tests.Fixtures
{
    public interface IService2
    {
        int Value { get; }
    }

    [ServiceDescriptor(typeof(IService2), Lifetime = ServiceLifetime.Scoped)]
    class Service2 : IService2
    {
        public int Value
        {
            get
            {
                return 9001;
            }
        }
    }
}
