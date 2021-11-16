namespace ajiva.Systems.Assets;

public class AssetHelper
{
    public static string Combine(string p1, string p2)
    {
        return Path.Combine(p1, p2);
    }

    public static string AsName(string name)
    {
        return string.Create(name.Length, name, (span, s) =>
        {
            for (var i = 0; i < s.Length; i++)
                if (s[i] is '\\' or '/')
                    span[i] = ':';
                else
                    span[i] = char.ToLower(s[i]);
        });
        return name;
    }
}