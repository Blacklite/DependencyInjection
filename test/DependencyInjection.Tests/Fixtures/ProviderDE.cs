using Microsoft.Framework.DependencyInjection;
using System;

namespace DependencyInjection.Tests.Fixtures
{
    public interface IProviderD
    {
        int ItemA { get; }
    }

    public interface IProviderE
    {
        decimal ItemB { get; }
    }

    [ServiceDescriptor(Lifecycle = LifecycleKind.Scoped)]
    class ProviderDE : IProviderD, IProviderE
    {
        public int ItemA
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public decimal ItemB
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
