namespace ajiva
{
    internal static class Const
    {
        public enum ExitCode : long
        {
            ShaderCompile = 10000,
        }
        public static class Default
        {
            public const string Config = "default.config";

            public const string AssetsFile = AssetsPath + "/default.asset";
            public const string AssetsPath = "Assets";

            public const int SurfaceWidth = 800;
            public const int SurfaceHeight = 600;

            public const int PosX = 200;
            public const int PosY = 300;

            public const string VertexShaderName = "vert.spv";
            public const string FragmentShaderName = "frag.spv";
        }
    }
}