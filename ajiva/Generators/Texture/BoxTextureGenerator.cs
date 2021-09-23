using System.Drawing;
using ajiva.Components.Media;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Worker;
using Ajiva.Wrapper.Logger;

namespace ajiva.Generators.Texture
{
    [Dependent(typeof(TextureSystem))]
    public class BoxTextureGenerator : SystemBase, IInit
    {
        public ATexture MissingTexture { get; private set; }

        /// <inheritdoc />
        public void Init(IAjivaEcs ecs)
        {
            Ecs.GetSystem<WorkerPool>().EnqueueWork(delegate
            {
                var bitmap = new Bitmap(4048, 4048);

                var g = Graphics.FromImage(bitmap);

                g.DrawRectangle(Pens.Black, 0, 0, bitmap.Height, bitmap.Width);

                g.DrawString("Missing\nTexture", new(FontFamily.GenericMonospace, 600, FontStyle.Bold, GraphicsUnit.Pixel), new SolidBrush(Color.White), new PointF(600, 600));

                g.Flush();

                MissingTexture = ATexture.FromBitmap(Ecs, bitmap);
                //MissingTexture.TextureId = 0;

                Ecs.GetComponentSystem<TextureSystem, ATexture>().AddAndMapTextureToDescriptor(MissingTexture);

                return WorkResult.Succeeded;
            }, LogHelper.WriteLine, "Missing Texture Generator");

            //Ecs.GetSystem<WorkerPool>().;
        }

        /// <inheritdoc />
        public BoxTextureGenerator(IAjivaEcs ecs) : base(ecs)
        {
        }
    }
}
