namespace AOULauncher;

public struct FileHash(string relativePath, string hash)
{
    public string RelativePath { get; set; } = relativePath;
    public string Hash { get; set; } = hash;
}