using System.Collections.Generic;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Components.RenderAble
{
    public class MeshPool : IRenderMeshPool
    {
        private readonly DeviceSystem deviceSystem;

        public MeshPool(DeviceSystem deviceSystem)
        {
            this.deviceSystem = deviceSystem;
        }

        public Dictionary<uint, IMesh> Meshes { get; } = new();

        /// <inheritdoc />
        public uint LastMeshId { get; set; }

        /// <inheritdoc />
        public void DrawMesh(CommandBuffer buffer, uint meshId)
        {
            IMesh mesh = Meshes[meshId]; // todo: check if exists and take error mesh

            if (meshId != LastMeshId)
            {
                LastMeshId = meshId;
                mesh.Bind(buffer);
            }
            mesh.DrawIndexed(buffer);
        }

        /// <inheritdoc />
        public void Reset()
        {
            LastMeshId = uint.MaxValue;
        }

        public void AddMesh(IMesh mesh)
        {
            mesh.Create(deviceSystem);
            Meshes.Add(mesh.MeshId, mesh);
        }
    }
}
