#if ARCH_UNITY_VCONTAINER_SUPPORT
#nullable disable
using System.Collections.Generic;
using Arch.Unity.Toolkit;
using VContainer;

namespace Arch.Unity
{
    internal sealed class SystemInstanceProvider<T> : IInstanceProvider
        where T : UnitySystemBase
    {
        readonly IInjector injector;
        readonly ISystemRunner systemRunner;
        readonly IReadOnlyList<IInjectParameter> customParameters;

        T instance;

        public SystemInstanceProvider(
            IInjector injector,
            ISystemRunner systemRunner,
            IReadOnlyList<IInjectParameter> customParameters)
        {
            this.injector = injector;
            this.systemRunner = systemRunner;
            this.customParameters = customParameters;
        }

        public object SpawnInstance(IObjectResolver resolver)
        {
            if (instance == null)
            {
                var app = resolver.Resolve<ArchApp>();
                instance = (T)injector.CreateInstance(resolver, customParameters);
                app.RegisterSystem(instance, systemRunner);
            }
            return instance;
        }
    }
}
#endif