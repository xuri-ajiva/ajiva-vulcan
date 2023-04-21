using System.Text.Json;

namespace Ajiva.Application;

public class AjivaConfig
{
    public WindowConfig Window { get; set; } = new WindowConfig();
    public ShaderConfig ShaderConfig { get; set; } = new ShaderConfig();

    public string AssetPath { get; set; } = Const.Default.AssetsFile;
}
public class WindowConfig
{
    public uint Width { set; get; } = 800;
    public uint Height { set; get; } = 600;

    public int PosX { set; get; } = 200;
    public int PosY { set; get; } = 300;
}
public class ShaderConfig
{
    // ReSharper disable InconsistentNaming
    public int TEXTURE_SAMPLER_COUNT { set; get; } = 128;

    // ReSharper restore InconsistentNaming

    public (string name, object value)[] GetAll()
    {
        return new (string name, object value)[] {
            (nameof(TEXTURE_SAMPLER_COUNT), TEXTURE_SAMPLER_COUNT)
        };
    }
}
