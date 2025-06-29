#nullable disable
using Arch.Core;
using UnityEngine;

namespace Arch.Unity.Editor
{
    public sealed class EntitySelectionProxy : ScriptableObject
    {
        public World world;
        public Entity entityReference;
    }
}