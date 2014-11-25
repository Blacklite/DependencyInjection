using Microsoft.Framework.DependencyInjection;
using System;

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
        public LifecycleKind Lifecycle { get; set; } = LifecycleKind.Transient;
    }
}
