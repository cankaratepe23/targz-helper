namespace targz_helper;

public class ConfigModel
{
    public bool IsWhitelistEnabled { get; set; }
    public List<string> Includes { get; set; } = new();
    public List<string> Excludes { get; set; } = new();
}