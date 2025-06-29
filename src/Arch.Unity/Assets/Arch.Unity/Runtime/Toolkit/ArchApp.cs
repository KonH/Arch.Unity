#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;

namespace Arch.Unity.Toolkit
{
    public sealed partial class ArchApp : IDisposable
    {
        World world;
        Dictionary<ISystemRunner, List<UnitySystemBase>> systemGroups;
        HashSet<UnitySystemBase> systemSet = new();
        bool isRunning;

        internal object[] createInstanceParameterCache;

        public World World => world;
        public bool IsRunning => isRunning;

        ArchApp() { }
        public static ArchApp Create()
        {
            var world = World.Create();
            return Create(world);
        }

        public static ArchApp Create(World world)
        {
            var app = new ArchApp
            {
                world = world,
                systemGroups = new(),
                createInstanceParameterCache = new object[] { world }
            };
            return app;
        }

        public void RegisterSystem(UnitySystemBase system)
        {
            RegisterSystem(system, SystemRunner.Default);
        }

        public void RegisterSystem(UnitySystemBase system, ISystemRunner runner)
        {
            if (!systemSet.Add(system)) return;

            if (!systemGroups.TryGetValue(runner, out var list))
            {
                list = new();
                systemGroups.Add(runner, list);
            }
            list.Add(system);

            if (IsRunning)
            {
                system.TryInitialize();
                runner.Add(system);
            }
        }

        public TSystem GetSystem<TSystem>() where TSystem : UnitySystemBase
        {
            return GetSystems<TSystem>().FirstOrDefault();
        }

        public IEnumerable<TSystem> GetSystems<TSystem>() where TSystem : UnitySystemBase
        {
            return systemGroups.SelectMany(x => x.Value.OfType<TSystem>());
        }

        public IEnumerable<UnitySystemBase> GetAllSystems()
        {
            return systemGroups.SelectMany(x => x.Value.OfType<UnitySystemBase>());
        }

        public ArchApp Run()
        {
            if (IsRunning) throw new InvalidOperationException("App is running.");

            isRunning = true;

            foreach (var kv in systemGroups)
            {
                foreach (var system in kv.Value)
                {
                    system.TryInitialize();
                    kv.Key.Add(system);
                }
            }

            return this;
        }

        public void Stop()
        {
            if (!IsRunning) throw new InvalidOperationException("App is not running.");

            foreach (var kv in systemGroups)
            {
                foreach (var system in kv.Value)
                {
                    kv.Key.Remove(system);
                }
            }
        }

        public void Dispose()
        {
            if (isRunning) Stop();

            foreach (var kv in systemGroups)
            {
                foreach (var system in kv.Value)
                {
                    system.Dispose();
                }
            }

            world.Dispose();
        }
    }
}