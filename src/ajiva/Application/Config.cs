using System.Text.Json;

namespace ajiva.Application;

public class Config
{
    private static Config? _default;

    public Config()
    {
        AssetPath = Const.Default.AssetsFile;
    }

    public WindowConfig Window { get; set; } = new WindowConfig();
    public ShaderConfig ShaderConfig { get; set; } = new ShaderConfig();

    public string AssetPath { get; set; }
    public static Config Default
    {
        get
        {
            if (_default is not null) return _default;

            _default = File.Exists(Const.Default.Config)
                ? JsonSerializer.Deserialize<Config>(File.ReadAllText(Const.Default.Config))!
                : new Config();
            File.WriteAllText(Const.Default.Config,
                JsonSerializer.Serialize(_default,
                    new JsonSerializerOptions
                        { WriteIndented = true }
                )
            );
            return _default;
        }
    }
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
        return new (string name, object value)[] { (nameof(TEXTURE_SAMPLER_COUNT), TEXTURE_SAMPLER_COUNT) };
    }
}
