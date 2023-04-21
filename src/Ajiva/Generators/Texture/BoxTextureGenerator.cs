using System.Drawing;
using Ajiva.Components.Media;
using Ajiva.Systems.VulcanEngine.Interfaces;
using Ajiva.Worker;

namespace Ajiva.Generators.Texture;

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
        var texture = textureSystem.CreateTextureAndMapToDescriptor(bitmap);

        //return WorkResult.Succeeded;
        //}, ALog.WriteLine, "Missing Texture Generator");
    }

    public ATexture MissingTexture { get; }
}