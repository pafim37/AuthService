using System.Reflection;

namespace AuthServer.DependencyInjection
{
    public static class ComponentServiceCollectionExtensions
    {
        public static IServiceCollection AddComponentsFromAssemblyContaining<TMarker>(this IServiceCollection services)
        {
            return services.AddComponentsFromAssembly(typeof(TMarker).Assembly);
        }

        public static IServiceCollection AddComponentsFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            IEnumerable<Type> componentTypes = assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false });

            foreach (Type implementationType in componentTypes)
            {
                ComponentAttribute[] components = [.. implementationType.GetCustomAttributes<ComponentAttribute>(inherit: false)];

                foreach (ComponentAttribute component in components)
                {
                    if (!component.ServiceType.IsAssignableFrom(implementationType))
                    {
                        throw new InvalidOperationException(
                            $"{implementationType.FullName} cannot be registered as {component.ServiceType.FullName} because it does not implement or inherit from that type.");
                    }

                    services.Add(new ServiceDescriptor(component.ServiceType, implementationType, component.Lifetime));
                }
            }

            return services;
        }
    }
}
