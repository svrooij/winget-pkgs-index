using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
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

    internal GenerateCommand() : base(CommandName, CommandDescription)
    {
        AddOption(_urlOption);
        this.SetHandler((context) => 
        {
            var uri = context.ParseResult.GetValueForOption(_urlOption)!;
            var keepDb = context.ParseResult.GetValueForOption(_keepDatabase);
            return commandHandler(uri, !keepDb, context);
        });
    }

    private static async Task<int> commandHandler(Uri uri, bool removeDatabase, InvocationContext context) {
        var cancellationToken = context.GetCancellationToken();

        // Download the source and extract the .db file to a temporary location
        var databaseFilePath = await DownloadAndExtractSourceAsync(uri, cancellationToken);
        try
        {
            // Create a new WingetContext using the temporary database file path
            using var db = new Models.WingetContext($"Data Source='{databaseFilePath}';Pooling=True;");

            // Perform operations with the context, e.g., querying the database
            var packages = await db.Packages
                .Include(p => p.Tags)
                .ToListAsync(cancellationToken);
            foreach (var package in packages)
            {
                Console.WriteLine($"Package: {package.Id}, Version: {package.LatestVersion}");
            }
        }
        finally
        {
            if (removeDatabase)
            {
                // Clean up the temporary file after use
                File.Delete(databaseFilePath);
            } else {
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
    internal static async Task<string> DownloadAndExtractSourceAsync(Uri uri, CancellationToken cancellationToken = default) {
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
}