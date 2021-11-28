using System.Drawing;
using ajiva.Components.Media;
using ajiva.Ecs;
using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Worker;

namespace ajiva.Generators.Texture;

[Dependent(typeof(TextureSystem))]
public class BoxTextureGenerator : SystemBase, IInit
{
    /// <inheritdoc />
    public BoxTextureGenerator(IAjivaEcs ecs) : base(ecs)
    {
    }

    public ATexture MissingTexture { get; private set; }

    /// <inheritdoc />
    public void Init()
    {
        Ecs.Get<WorkerPool>().EnqueueWork(delegate
        {
            var bitmap = new Bitmap(4048, 4048);

            var g = Graphics.FromImage(bitmap);

            g.DrawRectangle(Pens.Black, 0, 0, bitmap.Height, bitmap.Width);

            g.DrawString("Missing\nTexture", new Font(FontFamily.GenericMonospace, 600, FontStyle.Bold, GraphicsUnit.Pixel), new SolidBrush(Color.White), new PointF(600, 600));

            g.Flush();

            MissingTexture = ATexture.FromBitmap(Ecs, bitmap);
            //MissingTexture.TextureId = 0;

            Ecs.Get<ITextureSystem>().AddAndMapTextureToDescriptor(MissingTexture);

            return WorkResult.Succeeded;
        }, ALog.WriteLine, "Missing Texture Generator");

        //Ecs.Get<WorkerPool>().;
    }
}
