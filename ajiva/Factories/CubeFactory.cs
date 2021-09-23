﻿using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.Factory;
using ajiva.Entities;

namespace ajiva.Factories
{
    public class CubeFactory : EntityFactoryBase<Cube>
    {
        /// <inheritdoc />
        public override Cube Create(AjivaEcs system, uint id)
        {
            var cube = new Cube();
            //return cube.Create3DRenderedObject(system);
            system.AttachComponentToEntity<Transform3d>(cube);
            system.AttachComponentToEntity<RenderMesh3D>(cube);
            //system.AttachComponentToEntity<ATexture>(cube);
            return cube;
        }

        /// <param name="disposing"></param>
        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
        }
    }
}
