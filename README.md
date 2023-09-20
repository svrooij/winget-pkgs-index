# winget-pkgs-index

Open-source package index of [Windows Package Manager repository](https://github.com/microsoft/winget-pkgs)

## Why?

[WingetIntune](https://github.com/svrooij/wingetintune) uses winget to search the correct installer to publish to Intune. It had a dependency on winget (thus making it platform dependent) and it was slow. This project is a simple index of all packages in the winget repository. It is updated every 6 hours through a [github action](https://github.com/svrooij/winget-pkgs-index/actions/workflows/refresh.yml).

## Usage

Use this uri as index for [WingetIntune](https://github.com/svrooij/wingetintune)

```Shell
https://github.com/svrooij/winget-pkgs-index/raw/main/index.json
```

## Issues

Since the winget community does not force semantic versioning, there are some issues with the versioning.
The code that is used to find the latest version for a package is defined as follows.
If you have a better solution, please let me know, or send a PR for the [winget-intune](https://github.com/svrooij/WingetIntune/blob/cead73bcacfa1d9062c77d2fc027175520f407b9/src/Winget.CommunityRepository/VersionsExtensions.cs#L8C1-L36C2).

```csharp
internal static class VersionsExtensions
{
    internal static string? GetHighestVersion(this IEnumerable<string> versions)
    {
        if (versions is null || !versions.Any()) { return string.Empty; }
        return versions.Max(new VersionComparer());
    }
}

internal class VersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x is null && y is null) { return 0; }
        if (x is null) { return -1; }
        if (y is null) { return 1; }
        try
        {
            var xVersion = new Version(x);
            var yVersion = new Version(y);
            return xVersion.CompareTo(yVersion);
        }
        catch
        {
            return x.CompareTo(y);
        }

    }
}
```
