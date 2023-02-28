using System.Drawing;
using ajiva.Components.Media;
using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Worker;

namespace ajiva.Generators.Texture;

[Dependent(typeof(TextureSystem))]
public class BoxTextureGenerator : SystemBase
{
    /// <inheritdoc />
    public BoxTextureGenerator(WorkerPool workerPool, ITextureSystem textureSystem, TextureCreator creator)
    {
        //workerPool.EnqueueWork(delegate
        //{
        var bitmap = new Bitmap(4048, 4048);

        var g = Graphics.FromImage(bitmap);

        g.DrawRectangle(Pens.Black, 0, 0, bitmap.Height, bitmap.Width);

        g.DrawString("Missing\nTexture", new Font(FontFamily.GenericMonospace, 600, FontStyle.Bold, GraphicsUnit.Pixel), new SolidBrush(Color.White), new PointF(600, 600));

        g.Flush();

        //MissingTexture.TextureId = 0;
        ATexture texture = textureSystem.CreateTextureAndMapToDescriptor(bitmap);

        //return WorkResult.Succeeded;
        //}, ALog.WriteLine, "Missing Texture Generator");
    }

    public ATexture MissingTexture { get; private set; }
}
