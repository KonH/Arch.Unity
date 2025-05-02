#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine;
#endif

namespace Arch.Unity
{
    internal static class DisposePoolHelper
    {
        static void Dispose()
        {
            AllocatorPool.Dispose();
            GCHandlePool.Dispose();
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void Init()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
        }
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Application.quitting += Dispose;
        }
#endif
    }
}