using System;

namespace ImTools.SimpleDIPlayground
{
    class Program
    {
        static void Main()
        {
            var di = new DIContainer();

            di.Register<I, A>();

            var x = di.Resolve<I>();

            Console.WriteLine($"Hello {x}");
        }

        public interface I { }
        public class A : I { }
    }

    public class DIContainer
    {
        private readonly Ref<Registry> _registry = Ref.Of(new Registry());

        public void Register<TService, TImpl>() where TImpl : TService, new()
        {
            _registry.Swap(registry => registry.With(typeof(TService), new Factory(typeof(TImpl))));
        }

        public object Resolve<TService>()
        {
            return (TService)(_registry.Value.Resolve(typeof(TService)) 
                ?? throw new InvalidOperationException("Unable to resolve: " + typeof(TService)));
        }

        private class Registry
        {
            private ImHashMap<Type, Factory> _registrations = ImHashMap<Type, Factory>.Empty;

            private Ref<ImHashMap<Type, Func<object>>> _resolutionCache = Ref.Of(ImHashMap<Type, Func<object>>.Empty);

            // Creating a new registry with +1 registration and the new reference to cache value
            public Registry With(Type serviceType, Factory implFactory)
            {
                return new Registry
                {
                    _registrations = _registrations.AddOrUpdate(serviceType, implFactory),

                    // Here is the most interesting part:
                    //
                    // We are creating a new independent reference pointing to the cache value,
                    // isolating it from any possible parallel resolutions, with will proceed to work with the old cache.
                    //
                    _resolutionCache = Ref.Of(_resolutionCache.Value)
                };
            }

            public object Resolve(Type serviceType)
            {
                var cachedDelegate = _resolutionCache.Value.GetValueOrDefault(serviceType);
                if (cachedDelegate != null)
                    return cachedDelegate.Invoke();

                var factory = _registrations.GetValueOrDefault(serviceType);
                if (factory == null)
                    return null;

                cachedDelegate = factory.CompileDelegate();
                _resolutionCache.Swap(cache => cache.AddOrUpdate(serviceType, cachedDelegate));
                return cachedDelegate.Invoke();
            }
        }

        // Simple factory class is just demonstration
        internal class Factory
        {
            public readonly Type ImplType;
            public Factory(Type implType) { ImplType = implType; }
            public Func<object> CompileDelegate() { return () => Activator.CreateInstance(ImplType); }
        }
    }
}
