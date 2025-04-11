namespace WingetIndexGenerator.Dto;

public class PackageV2
{
    public string? Name { get; set; }
    public string? PackageId { get; set; }
    public string? Version { get; set; }
    public IEnumerable<string>? Tags { get; set; }
    public DateTimeOffset? LastUpdate { get; set; }

    public override string ToString()
    {
        return $"{PackageId} [{Version}] {LastUpdate}";
    }
}