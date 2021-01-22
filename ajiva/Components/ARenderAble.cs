using System.Collections.Generic;
using ajiva.Ecs.Component;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.EngineManagers;

namespace ajiva.Components
{
    public class ARenderAble : DisposingLogger, IComponent
    {
        public uint Id { get; }
        public Mesh? Mesh { get; private set; }
        public bool Render { get; set; }

        public ARenderAble()
        {
            Render = false;
            Id = NextId();
        }
        
        public void SetMesh(Mesh? mesh, DeviceComponent component)
        {
            Mesh?.Dispose();
            Mesh = mesh;
            Mesh?.Create(component);
            Render &= mesh != null;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            UsedIds.Remove(Id);
            Mesh?.Dispose();
        }

        private static readonly HashSet<uint> UsedIds = new();

        public static uint NextId()
        {
            for (uint i = 0;; i++)
            {
                if (UsedIds.Contains(i)) continue;
                
                UsedIds.Add(i);
                return i;
            }
        }
    }
}
