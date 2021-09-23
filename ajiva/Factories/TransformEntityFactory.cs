using ajiva.Components.Media;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.Factory;
using ajiva.Entities;
using GlmSharp;

namespace ajiva.Factories
{
    public class TransformEntityFactory : EntityFactoryBase<TransformFormEntity<Transform3d, vec3, mat4>>
    {
        /// <inheritdoc />
        public override TransformFormEntity<Transform3d, vec3, mat4> Create(IAjivaEcs system, uint id)
        {
            var entity = new TransformFormEntity<Transform3d, vec3, mat4>
                {Id = id};
            system.AttachNewComponentToEntity<Transform3d>(entity);
            return entity;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
        }
    }
}
