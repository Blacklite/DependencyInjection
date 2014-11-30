using System;
using System.Reflection;
using System.Linq;
using Xunit;
using Microsoft.Framework.DependencyInjection;
using System.Collections.Generic;
using DependencyInjection.Tests.Fixtures;

namespace DependencyInjection.Tests
{
    public class ServiceDescriberExtensionsTests
    {
        [Fact]
        public void ServiceDescriberAcceptsObjectContext()
        {
            var describer = new ServiceDescriber();

            var collection = describer.FromAssembly(this);

            Assert.Equal(collection.Count(), 14);
        }

        [Fact]
        public void ServiceDescriberAcceptsTypeContext()
        {
            var describer = new ServiceDescriber();

            var collection = describer.FromAssembly(typeof(ServicesContainerExtensionsTests));

            Assert.Equal(collection.Count(), 14);
        }

        [Fact]
        public void ServiceDescriberAcceptsAssemblyContext()
        {
            var describer = new ServiceDescriber();

            var collection = describer.FromAssembly(typeof(ServicesContainerExtensionsTests).GetTypeInfo().Assembly);

            Assert.Equal(collection.Count(), 14);
        }

        private class ServiceDescriptorEqualityComparer : IEqualityComparer<IServiceDescriptor>
        {
            public bool Equals(IServiceDescriptor x, IServiceDescriptor y)
            {
                return x.ImplementationType == y.ImplementationType && x.Lifecycle == y.Lifecycle && x.ServiceType == y.ServiceType;
            }

            public int GetHashCode(IServiceDescriptor obj)
            {
                return obj.GetHashCode();
            }
        }

        public void ServiceDescriberContainsPublicClassesWhenUsedGenerically()
        {
            var describer = new ServiceDescriber();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            var collection = describer.FromAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IProviderB), typeof(ProviderBC), LifecycleKind.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IProviderC), typeof(ProviderBC), LifecycleKind.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(ProviderBC), typeof(ProviderBC), LifecycleKind.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IProviderD), typeof(ProviderDE), LifecycleKind.Scoped);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IProviderE), typeof(ProviderDE), LifecycleKind.Scoped);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(ProviderDE), typeof(ProviderDE), LifecycleKind.Scoped);

            Assert.DoesNotContain(descriptor, collection, eqalityComparer);
        }

        [Fact]
        public void ServiceDescriberIncludesProviderA()
        {
            var describer = new ServiceDescriber();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            var collection = describer.FromAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IProviderA), typeof(ProviderA), LifecycleKind.Singleton);

            Assert.Contains(descriptor, collection, eqalityComparer);
        }

        [Fact]
        public void ServiceDescriberIncludesService1()
        {
            var describer = new ServiceDescriber();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            var collection = describer.FromAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IService1), typeof(Service1), LifecycleKind.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);
        }

        [Fact]
        public void ServiceDescriberIncludesService2()
        {
            var describer = new ServiceDescriber();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            var collection = describer.FromAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IService2), typeof(Service2), LifecycleKind.Scoped);

            Assert.Contains(descriptor, collection, eqalityComparer);
        }

        [Fact]
        public void ServiceDescriberIncludsOpenProviderA()
        {
            var describer = new ServiceDescriber();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            var collection = describer.FromAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IOpenProviderA<,>), typeof(OpenProviderA<,>), LifecycleKind.Singleton);

            Assert.Contains(descriptor, collection, eqalityComparer);
        }

        public void ServiceDescriberContainsOpenPublicClassesWhenUsedGenerically()
        {
            var describer = new ServiceDescriber();
            var eqalityComparer = new ServiceDescriptorEqualityComparer();

            var collection = describer.FromAssembly(this);

            var descriptor = new ServiceDescriptor(typeof(IOpenProviderB<>), typeof(OpenProviderBC<>), LifecycleKind.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IOpenProviderC<>), typeof(OpenProviderBC<>), LifecycleKind.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(OpenProviderBC<>), typeof(OpenProviderBC<>), LifecycleKind.Transient);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IOpenProviderD<>), typeof(OpenProviderDE<>), LifecycleKind.Scoped);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IOpenProviderE<>), typeof(OpenProviderDE<>), LifecycleKind.Scoped);

            Assert.Contains(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(IOpenProviderF<>), typeof(OpenProviderDE<>), LifecycleKind.Scoped);

            Assert.DoesNotContain(descriptor, collection, eqalityComparer);

            descriptor = new ServiceDescriptor(typeof(OpenProviderDE<>), typeof(OpenProviderDE<>), LifecycleKind.Scoped);

            Assert.DoesNotContain(descriptor, collection, eqalityComparer);
        }
    }
}
