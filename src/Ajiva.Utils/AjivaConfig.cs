using System.Numerics;

namespace Ajiva.Utils;

public class AjivaConfig
{
    public const string FileName = "Ajiva.json";
    public WindowConfig WindowConfig { get; set; } = new WindowConfig();
    public ShaderConfig ShaderConfig { get; set; } = new ShaderConfig();
    public CameraConfig CameraConfig { get; set; } = new CameraConfig();

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
public class CameraConfig
{
    public SerializedVector3 Position { get; set; }
    public SerializedVector3 Rotation { get; set; }
    public float Zoom { get; set; }
    public float Near { get; set; } = 0.1f;
    public float Far { get; set; } = 1000.0f;
    public float Fov { get; set; } = 100.0f;
    public float Speed { get; set; } = 1.0f;
    public float Sensitivity { get; set; } = 0.1f;
    public float AspectRatio { get; set; } = 1.3333333333333333f;
}
[Serializable]
public class SerializedVector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public SerializedVector3()
    {
    }

    public SerializedVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static implicit operator Vector3(SerializedVector3 vector3) => new Vector3(vector3.X, vector3.Y, vector3.Z);

    public static implicit operator SerializedVector3(Vector3 vector3) => new SerializedVector3(vector3.X, vector3.Y, vector3.Z);
}
