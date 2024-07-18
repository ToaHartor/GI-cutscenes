using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace GICutscenes
{
    internal sealed class Settings
    {
        public string? MkvMergePath { get; set; }
        public string? SubsFolder { get; set; }
        public string? FfmpegPath { get; set; }
        public string? SubsStyle { get; set; }
    }

    internal sealed class Program
    {
        public static Settings? settings;

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "Retrieves the configuration file"
        )]
        private static int Main(string[] args)
        {
            Option<bool> stackTraceOption = new Option<bool>(
                name: "--stack-trace",
                description: "Show stack trace when throw exception."
            );
            stackTraceOption.AddAlias("-st");

            RootCommand rootCommand = new RootCommand(
                "A command line program playing with the cutscenes files (USM) from Genshin Impact."
            );

            rootCommand.AddGlobalOption(stackTraceOption);

            rootCommand.AddCommand(new DemuxCommand());
            rootCommand.AddCommand(new ConvertHcaCommand());
            rootCommand.AddCommand(new UpdateCommand());
            rootCommand.AddCommand(new ResetCommand());

            return new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler(
                    (ex, context) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        if (context.ParseResult.GetValueForOption(stackTraceOption))
                        {
                            Console.Error.WriteLine(ex);
                        }
                        else
                        {
                            Console.Error.WriteLine($"{ex.GetType()}: {ex.Message}");
                        }
                        Console.ResetColor();
                    }
                )
                .Build()
                .Invoke(args);
        }

        [RequiresUnreferencedCode(
            "Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Get<T>()"
        )]
        public static void ReadSetting()
        {
            if (settings is null)
            {
                // Loading config file
                // TODO: A LA MANO ?
                IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
                    .AddEnvironmentVariables()
                    .Build();

                settings = config?.GetSection(nameof(Settings)).Get<Settings>();
                if (settings is null)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(
                        "File 'appsettings.json' has error section, use command 'reset' to reset it to default."
                    );
                    Console.ResetColor();
                    Environment.Exit(1);
                }
            }
        }
    }
}
