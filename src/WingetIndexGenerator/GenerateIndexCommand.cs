using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WingetIndexGenerator.Models;
namespace WingetIndexGenerator;

internal sealed class GenerateCommand : Command
{
    private const string DEFAULT_SOURCE_URL = "https://cdn.winget.microsoft.com/cache/source2.msix";
    internal const string CommandName = "generate";
    internal const string CommandDescription = "Generate winget index for the specified url.";

    private readonly static Option<Uri> _urlOption = new Option<Uri>(new[] { "--url", "-u" }, () => new Uri(DEFAULT_SOURCE_URL), "The url where to download the index package.")
    {
        IsRequired = true,
        ArgumentHelpName = "url"
    };

    private readonly static Option<bool> _keepDatabase = new Option<bool>(new[] { "--keep-db", "-d" }, () => false, "Keep the database after use.")
    {
        IsRequired = false,
        IsHidden = true,
        ArgumentHelpName = "keep-db"
    };

    private readonly static Option<string> _outputFolder = new Option<string>(new[] { "--output", "-o" }, () => Path.GetFullPath(Environment.CurrentDirectory), "The output folder where to save the index files.")
    {
        IsRequired = true,
        ArgumentHelpName = "output"
    };

    internal GenerateCommand() : base(CommandName, CommandDescription)
    {
        AddOption(_outputFolder);
        AddOption(_urlOption);
        AddOption(_keepDatabase);
        this.SetHandler((context) =>
        {
            var outputFolder = context.ParseResult.GetValueForOption(_outputFolder)!;
            if (!Path.IsPathFullyQualified(outputFolder))
            {
                outputFolder = Path.Combine(Environment.CurrentDirectory, outputFolder);
            }
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            var uri = context.ParseResult.GetValueForOption(_urlOption)!;
            var keepDb = context.ParseResult.GetValueForOption(_keepDatabase);
            return commandHandler(uri, outputFolder, !keepDb, context);
        });
    }

    private static async Task<int> commandHandler(Uri uri, string outputFolder, bool removeDatabase, InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();

        // Download the source and extract the .db file to a temporary location
        Console.WriteLine($"Downloading source from {uri}");
        var databaseFilePath = await DownloadAndExtractSourceAsync(uri, cancellationToken);
        try
        {
            // Create a new WingetContext using the temporary database file path
            using var db = new Models.WingetContext($"Data Source='{databaseFilePath}';Pooling=True;");

            var timestamp = DateTimeOffset.UtcNow;

            // Perform operations with the context, e.g., querying the database
            var packages = await db.Packages
                .Include(p => p.Tags)
                .ToListAsync(cancellationToken);

            if (packages == null || packages.Count == 0)
            {
                Console.WriteLine("No packages found in the database.");
                return 1;
            }
            Console.WriteLine($"Found {packages.Count} packages in the database.");
            // Order packages by ID ignoring the case
            packages = packages.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase).ToList();

            // Write the packages to the v1 json file
            Console.WriteLine($"Writing {packages.Count} packages to {Path.Combine(outputFolder, "index.json")}");
            await WritePackagesToJsonV1Async(packages, Path.Combine(outputFolder, "index.json"), cancellationToken);

            // Write the packages to the v2 json file (and use it to detect which packages are updated)
            Console.WriteLine($"Writing {packages.Count} packages to {Path.Combine(outputFolder, "index.v2.json")}");
            var packageV2List = await WritePackagesToJsonV2Async(packages, Path.Combine(outputFolder, "index.v2.json"), timestamp, cancellationToken);

            // Write the packages to the csv files
            Console.WriteLine($"Writing {packages.Count} packages to csv files in {outputFolder}");
            await WriteCsvFilesAsync(packageV2List, outputFolder, cancellationToken);

            try
            {
                // Write a summary to the GITHUB_STEP_SUMMARY file if it exists and is set.
                await WriteGithubSummaryAsync(packageV2List, timestamp, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to GITHUB_STEP_SUMMARY: {ex.Message}");
            }

        }
        finally
        {
            if (removeDatabase)
            {
                // Clean up the temporary file after use
                File.Delete(databaseFilePath);
            }
            else
            {
                // If not deleting, just inform the user
                Console.WriteLine($"Temporary database file not deleted: {databaseFilePath}");
            }
        }
        return 0;
    }

    /// <summary>
    /// Downloads the source from the specified URI and extracts the .db file to a temporary location.
    /// The temporary file path is returned.
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="cancellationToken"></param>
    /// <remarks>Be sure to delete the temporary file after use.</remarks>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="IOException"></exception>
    internal static async Task<string> DownloadAndExtractSourceAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var archive = new ZipArchive(contentStream, ZipArchiveMode.Read, leaveOpen: false);
        var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".db", StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            throw new InvalidOperationException("No .db file found in the archive.");
        }
        var tempFilePath = Path.GetTempFileName();
        using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
        {
            await entry.Open().CopyToAsync(fileStream, cancellationToken);
        }

        return tempFilePath;
    }

    internal static async Task WritePackagesToJsonV1Async(IEnumerable<Package> packages, string outputFile, CancellationToken cancellationToken = default)
    {
        var packageV1List = packages.Select(p => new Dto.PackageV1
        {
            Name = p.Name,
            PackageId = p.Id,
            Version = p.LatestVersion
        }).ToList();
        var json = JsonSerializer.Serialize(packageV1List, new JsonSerializerOptions { WriteIndented = false });

        await File.WriteAllTextAsync(outputFile, json, cancellationToken);
    }

    internal static async Task<IEnumerable<Dto.PackageV2>> WritePackagesToJsonV2Async(IEnumerable<Package> packages, string outputFile, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {

        var packageV2List = packages.Select(p => new Dto.PackageV2
        {
            Name = p.Name,
            PackageId = p.Id,
            Version = p.LatestVersion,
            LastUpdate = timestamp,
            Tags = p.Tags?.Select(t => t.TagValue!).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList()
        }).ToList();

        if (File.Exists(outputFile))
        {
            var existingJson = await File.ReadAllTextAsync(outputFile, cancellationToken);
            var existingPackages = JsonSerializer.Deserialize<IEnumerable<Dto.PackageV2>>(existingJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (existingPackages != null)
            {
                foreach (var package in packageV2List)
                {
                    var existingPackage = existingPackages.FirstOrDefault(p => p.PackageId == package.PackageId && p.Version == package.Version);
                    if (existingPackage != null)
                    {
                        package.LastUpdate = existingPackage.LastUpdate;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Could not deserialize existing file {outputFile}");
            }
        }

        var json = JsonSerializer.Serialize(packageV2List, new JsonSerializerOptions { WriteIndented = false });

        await File.WriteAllTextAsync(outputFile, json, cancellationToken);
        return packageV2List;
    }

    private static async Task WriteCsvFilesAsync(IEnumerable<Dto.PackageV2> packages, string outputFolder, CancellationToken cancellationToken = default)
    {
        using var csv1Writer = new StreamWriter(Path.Combine(outputFolder, "index.csv"), false, System.Text.Encoding.UTF8);
        using var csv2Writer = new StreamWriter(Path.Combine(outputFolder, "index.v2.csv"), false, System.Text.Encoding.UTF8);
        await csv1Writer.WriteAsync("\"PackageId\",\"Version\"\r\n");
        await csv2Writer.WriteAsync("\"PackageId\",\"Version\",\"Name\",\"LastUpdate\"\r\n");

        foreach (var package in packages)
        {
            await csv1Writer.WriteAsync($"\"{package.PackageId}\",\"{package.Version}\"\r\n");
            await csv2Writer.WriteAsync($"\"{package.PackageId}\",\"{package.Version}\",\"{package.Name}\",\"{package.LastUpdate:yyyy-MM-dd HH:mm:ssZ}\"\r\n");
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
        await csv1Writer.WriteLineAsync();
        await csv2Writer.WriteLineAsync();
        await csv1Writer.FlushAsync(cancellationToken);
        await csv2Writer.FlushAsync(cancellationToken);
        await csv1Writer.DisposeAsync();
        await csv2Writer.DisposeAsync();

    }

    private static async Task WriteGithubSummaryAsync(IEnumerable<Dto.PackageV2> packages, DateTimeOffset dateTimeOffset, CancellationToken cancellationToken = default)
    {
        var file = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (string.IsNullOrEmpty(file))
        {
            Console.WriteLine("GITHUB_STEP_SUMMARY environment variable is not set. Skipping summary generation.");
            return;
        }

        var updatedPackages = packages.Where(p => p.LastUpdate == dateTimeOffset).ToList();

        if (updatedPackages.Count == 0)
        {
            Console.WriteLine("No updated packages found. Skipping summary generation.");
            return;
        }

        using var githubStream = new Util.GithubFileStream(file, FileMode.Append);
        using var summaryWriter = new StreamWriter(githubStream, System.Text.Encoding.UTF8);
        await summaryWriter.WriteLineAsync("# Winget Index Generator\r\n");
        await summaryWriter.WriteLineAsync($"- Total packages `{packages.Count()}`");
        await summaryWriter.WriteLineAsync($"- Updated packages `{updatedPackages.Count}`");
        await summaryWriter.WriteLineAsync($"- Timestamp `{DateTime.UtcNow:yyyy-MM-dd HH:mm:ssZ}`\r\n");
        await summaryWriter.WriteLineAsync("### Changed packages\r\n");
        foreach (var package in updatedPackages)
        {
            await summaryWriter.WriteLineAsync($"- {package.PackageId} [{package.Version}]");
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
        await summaryWriter.FlushAsync(cancellationToken);
        await summaryWriter.DisposeAsync();

    }
}
