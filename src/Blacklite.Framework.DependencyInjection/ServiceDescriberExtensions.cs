using Blacklite;
using Microsoft.Framework.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// We're cheating here, so we don't have to have two different difference namespaces everywhere
namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceDescriberExtensions
    {
        public static IEnumerable<IServiceDescriptor> FromAssembly([NotNull] this ServiceDescriber describer, [NotNull] object context)
        {
            return describer.FromAssembly(context.GetType());
        }

        public static IEnumerable<IServiceDescriptor> FromAssembly([NotNull] this ServiceDescriber describer, [NotNull] Type type)
        {
            var assembly = type.GetTypeInfo().Assembly;
            return describer.FromAssembly(assembly);
        }

        public static IEnumerable<IServiceDescriptor> FromAssembly([NotNull] this ServiceDescriber describer, [NotNull] Assembly assembly)
        {
            return GetServiceDescriptors(assembly);
        }

        internal static IEnumerable<IServiceDescriptor> GetServiceDescriptors(Assembly assembly)
        {
            var services = assembly.DefinedTypes
                .Select(x => new
                {
                    ImplementationType = x.AsType(),
                    Attribute = x.GetCustomAttribute<ServiceDescriptorAttribute>(true)
                })
                .Where(x => x.Attribute != null);

            foreach (var service in services)
            {
                var implementationType = service.ImplementationType;
                IEnumerable<Type> serviceTypes = null;
                if (service.Attribute.ServiceType == null)
                {
                    serviceTypes = implementationType.GetInterfaces();
                    if (implementationType.GetTypeInfo().IsPublic)
                    {
                        // TODO:  Should this include all base types?  Should it be the lowest base type (HttpContext for example)?
                        serviceTypes = serviceTypes.Concat(new[] { implementationType });
                    }

                    if (implementationType.GetTypeInfo().ContainsGenericParameters)
                    {
                        var parameters = implementationType.GetGenericArguments();

                        serviceTypes = serviceTypes.Where(type => parameters
                            .Join(type.GetGenericArguments(), x => x.Name, x => x.Name, (x, y) => true).Count() == parameters.Count())
                            .Select(x => x.GetGenericTypeDefinition());
                    }
                }
                else
                {
                    if (service.Attribute.ServiceType.GetTypeInfo().IsGenericTypeDefinition)
                    {
                        implementationType = implementationType.GetGenericTypeDefinition();
                    }
                    serviceTypes = new[] { service.Attribute.ServiceType };
                }

                foreach (var serviceType in serviceTypes)
                {
                    if (service.Attribute.ServiceType == null || // We're registering everything, and we've already filtered inapplicable types
                        serviceType.IsAssignableFrom(implementationType) || // Handle the most basic registration
                        service.ImplementationType.GetInterfaces() // Handle the open implementation....
                            .Concat(GetAllBaseTypes(service.ImplementationType))
                            .Select(z => z.GetGenericTypeDefinition())
                            .Any(z => z == serviceType)
                        )
                    {
                        var lifecycle = service.Attribute.Lifecycle;

                        yield return new ServiceDescriptor(serviceType, implementationType, service.Attribute.Lifecycle);
                    }
                    else
                    {
                        throw new InvalidCastException(string.Format("Service Type '{0}' is not assignable from Implementation Type '{1}'.",
                            serviceType.FullName,
                            implementationType.FullName)
                        );
                    }
                }
            }
        }

        private static IEnumerable<Type> GetAllBaseTypes(Type type)
        {
            while (type.GetTypeInfo().BaseType != null)
            {
                yield return type;
                type = type.GetTypeInfo().BaseType;
            }
        }
    }
}
