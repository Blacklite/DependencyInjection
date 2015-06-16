using Microsoft.Framework.DependencyInjection;
using System;

// We're cheating here, so we don't have to have two different difference namespaces everywhere
namespace Microsoft.Framework.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceDescriptorAttribute : Attribute
    {
        public ServiceDescriptorAttribute(Type serviceType = null)
        {
            ServiceType = serviceType;
        }

        public Type ServiceType { get; }
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
    }
}
