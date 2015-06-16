using System;
using System.Reflection;
using System.Linq;
using Xunit;
using Microsoft.Framework.DependencyInjection;
using System.Collections.Generic;
using DependencyInjection.Tests.Fixtures;

namespace DependencyInjection.Tests
{
    public class ServicesContainerExtensionsTests
    {
        [Fact]
        public void ServiceCollectionAcceptsObjectContext()
        {
            var collection = new ServiceCollection();

            collection.AddAssembly(this);

            Assert.Equal(collection.Count(), 14);
        }

        [Fact]
        public void ServiceCollectionAcceptsTypeContext()
        {
            var collection = new ServiceCollection();

            collection.AddAssembly(typeof(ServicesContainerExtensionsTests));

            Assert.Equal(collection.Count(), 14);
        }

        [Fact]
        public void ServiceCollectionAcceptsAssemblyContext()
        {
            var collection = new ServiceCollection();

            collection.AddAssembly(typeof(ServicesContainerExtensionsTests).GetTypeInfo().Assembly);

            Assert.Equal(collection.Count(), 14);
        }

        private class ServiceDescriptorEqualityComparer : IEqualityComparer<ServiceDescriptor>
        {
            public bool Equals(ServiceDescriptor x, ServiceDescriptor y)
            {
                return x.ImplementationType == y.ImplementationType && x.Lifetime == y.Lifetime && x.ServiceType == y.ServiceType;
            }

            public int GetHashCode(ServiceDescriptor obj)
            {
                return obj.GetHashCode();
            }
        }

        public void ServiceCollectionContainsPublicClassesWhenUsedGenerically()
        {
            var collection = new ServiceCollection();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            collection.AddAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IProviderB), typeof(ProviderBC), ServiceLifetime.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IProviderC), typeof(ProviderBC), ServiceLifetime.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(ProviderBC), typeof(ProviderBC), ServiceLifetime.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IProviderD), typeof(ProviderDE), ServiceLifetime.Scoped);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IProviderE), typeof(ProviderDE), ServiceLifetime.Scoped);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(ProviderDE), typeof(ProviderDE), ServiceLifetime.Scoped);

            Assert.DoesNotContain(descriptor, collection, eqalityComparer);
        }

        [Fact]
        public void ServiceCollectionIncludesProviderA()
        {
            var collection = new ServiceCollection();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            collection.AddAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IProviderA), typeof(ProviderA), ServiceLifetime.Singleton);

            Assert.Contains(descriptor, collection, eqalityComparer);
        }

        [Fact]
        public void ServiceCollectionIncludesService1()
        {
            var collection = new ServiceCollection();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            collection.AddAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IService1), typeof(Service1), ServiceLifetime.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);
        }

        [Fact]
        public void ServiceCollectionIncludesService2()
        {
            var collection = new ServiceCollection();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            collection.AddAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IService2), typeof(Service2), ServiceLifetime.Scoped);

            Assert.Contains(descriptor, collection, eqalityComparer);
        }

        [Fact]
        public void ServiceCollectionIncludsOpenProviderA()
        {
            var collection = new ServiceCollection();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            collection.AddAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IOpenProviderA<,>), typeof(OpenProviderA<,>), ServiceLifetime.Singleton);

            Assert.Contains(descriptor, collection, eqalityComparer);
        }

        public void ServiceCollectionContainsOpenPublicClassesWhenUsedGenerically()
        {
            var collection = new ServiceCollection();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            collection.AddAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IOpenProviderB<>), typeof(OpenProviderBC<>), ServiceLifetime.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IOpenProviderC<>), typeof(OpenProviderBC<>), ServiceLifetime.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(OpenProviderBC<>), typeof(OpenProviderBC<>), ServiceLifetime.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IOpenProviderD<>), typeof(OpenProviderDE<>), ServiceLifetime.Scoped);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IOpenProviderE<>), typeof(OpenProviderDE<>), ServiceLifetime.Scoped);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IOpenProviderF<>), typeof(OpenProviderDE<>), ServiceLifetime.Scoped);

            Assert.DoesNotContain(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(OpenProviderDE<>), typeof(OpenProviderDE<>), ServiceLifetime.Scoped);

            Assert.DoesNotContain(descriptor, collection, eqalityComparer);
        }
    }
}
