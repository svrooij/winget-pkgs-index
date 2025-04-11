using System.CommandLine;

namespace WingetIndexGenerator;

public class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Winget Index Generator");

        rootCommand.AddCommand(new GenerateCommand());

        return await rootCommand.InvokeAsync(args);
    }
}