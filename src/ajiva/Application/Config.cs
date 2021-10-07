using System;
using System.IO;
using System.Text.Json;
using ajiva.Systems.VulcanEngine;

namespace ajiva.Application
{
    public class Config
    {
        public Config()
        {
            AssetPath = Const.Default.AssetsFile;
        }

        public WindowConfig Window { get; set; } = new WindowConfig();
        public string AssetPath { get; set; }

        private static Config? _default;
        public static Config Default
        {
            get
            {
                if (_default is null)
                {
                    if (File.Exists(Const.Default.Config))
                    {
                        _default = JsonSerializer.Deserialize<Config>(File.ReadAllText(Const.Default.Config))!;
                    }
                    else
                    {
                        _default = new Config();
                    }
                    File.WriteAllText(Const.Default.Config, JsonSerializer.Serialize(_default, new JsonSerializerOptions() { WriteIndented = true }));
                }
                return _default;
            }
        }
    }
    public class WindowConfig
    {
        public WindowConfig()
        {
            Height = Const.Default.SurfaceHeight;
            Width = Const.Default.SurfaceWidth;
            PosX = Const.Default.PosX;
            PosY = Const.Default.PosY;
        }

        public uint Width { get; set; }
        public uint Height { get; set; }
        
        public int PosX { get; set; }
        public int PosY { get; set; }
    }
}
