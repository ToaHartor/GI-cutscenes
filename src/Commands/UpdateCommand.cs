using System.CommandLine;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using GICutscenes.Utils;

namespace GICutscenes;

public class UpdateCommand : Command
{
    public UpdateCommand()
        : base("update", "Check and update to new versions of versions.json and GICutscenes")
    {
        var notOpenBrowser = CliOptions.NotOpenBrowser;
        var proxy = CliOptions.Proxy;

        notOpenBrowser.AddAlias("-nb");
        AddOption(notOpenBrowser);
        proxy.AddAlias("-p");
        AddOption(proxy);

        this.SetHandler(Execute, notOpenBrowser, proxy);
    }

    private async void Execute(bool notOpenBrowser, string proxy)
    {
        await UpdateAsync(notOpenBrowser, proxy);
    }

    [RequiresUnreferencedCode(
        "Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)"
    )]
    private static async Task UpdateAsync(bool notOpenBroswer, string? proxy)
    {
        var webProxy = new WebProxy(proxy);
        var client = new HttpClient(
            new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                Proxy = webProxy
            }
        );
        client.DefaultRequestHeaders.Add("User-Agent", "GICutscenes");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Update 'versions.json'...");
        Console.ResetColor();
        var versionsString = await client.GetStringAsync(
            "https://cdn.jsdelivr.net/gh/ToaHartor/GI-cutscenes@main/versions.json"
        );
        await File.WriteAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "versions.json"),
            versionsString
        );
        Console.WriteLine("'versions.json' has updated to the latest version.");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Check update for GICutscenes...");
        Console.ResetColor();
        var releaseString = await client.GetStringAsync(
            "https://api.github.com/repos/ToaHartor/GI-cutscenes/releases/latest"
        );
        var release = JsonSerializer.Deserialize<GithubRelease>(
            releaseString!,
            GithubJsonContext.Default.Options
        );
        var currentVersion = typeof(Program).Assembly.GetName().Version;
        if (System.Version.TryParse(release?.TagName?[1..], out var latestVersion))
        {
            if (latestVersion > currentVersion)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(
                    $"Latest version is '{release?.TagName}', GICutscenes needs to update."
                );
                Console.WriteLine($"Release page: {release?.HtmlUrl}");
                Console.ResetColor();
                if (!notOpenBroswer)
                {
                    if (!string.IsNullOrWhiteSpace(release?.HtmlUrl))
                    {
                        // What happens on macOS or Linux?
                        Process.Start(
                            new ProcessStartInfo(release.HtmlUrl) { UseShellExecute = true }
                        );
                    }
                }
            }
            else
            {
                Console.WriteLine("GICutscenes is already the latest version.");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(
                $"Cannot compare version, current version is '{currentVersion}', latest version is '{release?.TagName}'."
            );
            Console.ResetColor();
        }
    }
}
