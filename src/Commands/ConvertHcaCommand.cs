using System.CommandLine;
using GICutscenes.FileTypes;

namespace GICutscenes;

public class ConvertHcaCommand : Command
{
    public ConvertHcaCommand()
        : base("convert-hca", "Converts input .hca files into .wav files")
    {
        Argument<FileSystemInfo> inputArg = new Argument<FileSystemInfo>(
            name: "Input .hca file or folder",
            description: "The file to read and display on the console."
        );

        var output = CliOptions.Output;

        output.AddAlias("-o");
        AddOption(output);
        this.SetHandler(Execute, inputArg, output);
    }

    private void Execute(FileSystemInfo input, DirectoryInfo output)
    {
        output.Create();
        ConvertHca(input, output);
    }

    private static void ConvertHca(FileSystemInfo input, DirectoryInfo? output)
    {
        if (!input.Exists)
            throw new ArgumentException("No file or directory given.");
        string outputArg =
            (output == null)
                ? input.FullName
                : (
                    (output.Exists)
                        ? output.FullName
                        : throw new ArgumentException("Output directory is invalid.")
                );
        Console.WriteLine($"Output folder : {outputArg}");
        switch (input)
        {
            case FileInfo f:
                // TODO add keys :shrug:
                if (Path.GetExtension(f.Name) == ".hca")
                    throw new ArgumentException("File provided is not a .hca file.");
                Hca file = new(f.FullName);
                file.ConvertToWAV(outputArg);
                break;
            case DirectoryInfo directory:
                foreach (string f in Directory.EnumerateFiles(directory.FullName, "*.hca"))
                {
                    Hca singleFile = new(f);
                    singleFile.ConvertToWAV(outputArg);
                }
                break;
            default:
                Console.WriteLine("Not a valid file or directory name.");
                break;
        }
    }
}
