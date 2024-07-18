using System.CommandLine;

namespace GICutscenes;

public class ResetCommand : Command
{
    public ResetCommand()
        : base("reset", "Reset 'appsettings.json' file to default")
    {
        this.SetHandler(Execute);
    }

    private void Execute()
    {
        const string str = """
            {
                "Settings": {
                "MkvMergePath": "",
                "FfmpegPath": "",
                "SubsFolder": "",
                "SubsStyle": "Style: Default,{fontname},12,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100.0,100.0,0.0,0.0,1,0,0.5,2,10,10,14,1"
                }
            }
            """;
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), str);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("'appsettings.json' has been reset to default.");
        Console.ResetColor();
    }
}
