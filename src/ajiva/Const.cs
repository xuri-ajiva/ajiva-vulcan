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


            public const string VertexShaderName = "vert.spv";
            public const string FragmentShaderName = "frag.spv";
            public const int ModelBufferSize = 1_000_000;
            public const int BackupBuffers = 2;
        }
    }
}
