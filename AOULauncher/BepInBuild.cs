namespace AOULauncher;

public struct BepInBuild
{
    public BepInArtifact[] artifacts { get; set; }

    public struct BepInArtifact
    {
        public string description { get; set; }
        public string file { get; set; }
    }
}