namespace WingetIndexGenerator.Dto;

public class PackageV1
{
    public string? Name { get; set; }
    public string? PackageId { get; set; }
    public string? Version { get; set; }
    public override string ToString()
    {
        return $"{PackageId} [{Version}]";
    }
}