namespace AuthServer.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ComponentAttribute(Type serviceType) : Attribute
    {
        public Type ServiceType { get; } = serviceType;

        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
    }
}
