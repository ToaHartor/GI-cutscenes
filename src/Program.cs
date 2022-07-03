using Microsoft.Extensions.Configuration;
using System.CommandLine;
using GICutscenes.FileTypes;
using GICutscenes.Mergers;
using GICutscenes.Mergers.GIMKV;
using System.Diagnostics.CodeAnalysis;

namespace GICutscenes
{
    internal sealed class Settings
    {
        public string? MkvMergePath { get; set; }
        public string? SubsFolder { get; set; }
        public string? FfmpegPath { get; set; }

    }
    internal sealed class Program
    {
        public static Settings settings;

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Retrieves the configuration file")]
        private static Task<int> Main(string[] args)
        {
            
            // Loading config file
            // TODO: A LA MANO ?
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            settings = config.GetRequiredSection(nameof(Settings)).Get<Settings>();

            // CLI Options
            var demuxFileOption = new Argument<FileInfo?>(
                name: "Input file",
                description: "The file to read and display on the console.");

            var usmFolderArg = new Argument<DirectoryInfo>(
                name: "USM Folder",
                description: "Folder containing the .usm files to be demuxed.");

            var hcaInputArg = new Argument<FileSystemInfo>(
                name: "HCA Input",
                description: "File or directory to be processed.");

            var outputFolderOption = new Option<DirectoryInfo?>(
                name: "--output",
                description: "Output folder."
                );
            outputFolderOption.AddAlias("-o");

            var key1Option = new Option<string?>(
                name: "-a",
                description: "4 lower bytes of the key");

            var key2Option = new Option<string?>(
                name: "-b",
                description: "4 higher bytes of the key");

            var subsOption = new Option<bool>(
                name: "--subs",
                description: "Adds the subtitles to the MKV file."
            );
            subsOption.AddAlias("--subtitles");
            subsOption.AddAlias("-s");

            var noCleanupOption = new Option<bool>(
                name: "--no-cleanup",
                description: "Keeps the extracted files instead of removing them.");
            noCleanupOption.AddAlias("-nc");

            var mergeOption = new Option<bool>(
                name: "--merge",
                description: "Merges the extracted content into a MKV container file."
            );
            mergeOption.AddAlias("-m");

            var mkvEngineOption = new Option<string>(
                name: "--mkv-engine",
                description: "Merges the extracted content into a MKV container file."
            ).FromAmong(
                "mkvmerge",
                "internal",
                "ffmpeg");
            mkvEngineOption.SetDefaultValue("internal");
            mkvEngineOption.AddAlias("-e");


            var rootCommand = new RootCommand("A command line program playing with the cutscenes files (USM) from Genshin Impact.");
            
            rootCommand.AddGlobalOption(outputFolderOption);
            rootCommand.AddGlobalOption(noCleanupOption);

            var demuxUsmCommand = new Command("demuxUsm", "Demuxes a specified .usm file to a specified folder")
            {
                demuxFileOption,
                key1Option,
                key2Option,
                subsOption,
                mergeOption,
                mkvEngineOption
            };

            var batchDemuxCommand = new Command("batchDemux", "Tries to demux all .usm files in the specified folder")
            {   
                usmFolderArg,
                subsOption,
                mergeOption,
                mkvEngineOption
            };

            //var hcaDecrypt = new Command();

            var convertHcaCommand = new Command("convertHca", "Converts input .hca files into .wav files")
            {
                hcaInputArg
            };

            rootCommand.AddCommand(demuxUsmCommand);
            rootCommand.AddCommand(batchDemuxCommand);
            rootCommand.AddCommand(convertHcaCommand);


            // Command Handlers
            demuxUsmCommand.SetHandler(async (FileInfo file, string key1, string key2, DirectoryInfo? output, string engine, bool merge, bool subs, bool noCleanup) => 
                {
                    await DemuxUsmCommand(file, key1, key2, output, engine, merge, subs, noCleanup);
                }, demuxFileOption, key1Option, key2Option, outputFolderOption, mkvEngineOption, mergeOption, subsOption, noCleanupOption);


            batchDemuxCommand.SetHandler(async (DirectoryInfo inputDir, DirectoryInfo? outputDir, string engine, bool merge, bool subs, bool noCleanup) =>
            {
                var timer = System.Diagnostics.Stopwatch.StartNew();
                await BatchDemuxCommand(inputDir, outputDir, engine, merge, subs, noCleanup);
                timer.Stop();
                Console.WriteLine($"{timer.ElapsedMilliseconds}ms elapsed");
            },
                usmFolderArg, outputFolderOption, mkvEngineOption, mergeOption, subsOption, noCleanupOption);

            convertHcaCommand.SetHandler(async (FileSystemInfo input, DirectoryInfo? output, bool noCleanup) =>
            {
                await ConvertHcaCommand(input, output /*, noCleanup*/);
            }, 
                hcaInputArg, outputFolderOption, noCleanupOption);

            return Task.FromResult(rootCommand.InvokeAsync(args).Result);
        }

        private static async Task DemuxUsmCommand(FileInfo file, string key1, string key2, DirectoryInfo? output, string engine, bool merge, bool subs, bool noCleanup)
        {
            if (file == null) throw new ArgumentNullException("No file provided.");
            if (!file.Exists) throw new ArgumentException("File {0} does not exist.", file.Name);
            if (!file.Name.EndsWith(".usm"))
                throw new ArgumentException($"File {file.Name} provided isn't a .usm file.");
            if (key1!=null && key2!= null && (key1.Length != 8 || key2.Length != 8)) throw new ArgumentException("Keys are invalid.");
            string outputArg = output == null
                ? file.Directory.FullName
                : ((output.Exists) ? output.FullName : throw new ArgumentException("Output directory is invalid."));
            Console.WriteLine($"Output folder : {outputArg}");
            byte[] key1Arg = Convert.FromHexString(key1 ?? "");
            byte[] key2Arg = Convert.FromHexString(key2 ?? "");
            Demuxer.Demux(file.FullName, key1Arg, key2Arg, outputArg);
            if (merge)
            {
                MergeFiles(outputArg, Path.GetFileNameWithoutExtension(file.FullName), engine, subs);
                if (!noCleanup) CleanFiles(outputArg, Path.GetFileNameWithoutExtension(file.FullName));
            }
        }

        private static async Task BatchDemuxCommand(DirectoryInfo inputDir, DirectoryInfo? outputDir, string engine, bool merge, bool subs, bool noCleanup)
        {
            if (inputDir is not { Exists: true }) throw new DirectoryNotFoundException("Input directory is invalid.");
            string outputArg = (outputDir == null)
                ? inputDir.FullName
                : ((outputDir.Exists) ? outputDir.FullName : throw new ArgumentException("Output directory is invalid."));
            Console.WriteLine($"Output folder : {outputArg}");
            foreach (string f in Directory.EnumerateFiles(inputDir.FullName, "*.usm"))
            {
                Demuxer.Demux(f, Array.Empty<byte>(), Array.Empty<byte>(), outputArg);
                if (!merge) continue;

                MergeFiles(outputArg, Path.GetFileNameWithoutExtension(f), engine, subs);
                if (!noCleanup) CleanFiles(outputArg, Path.GetFileNameWithoutExtension(f));
            }
        }

        private static async Task ConvertHcaCommand(FileSystemInfo input, DirectoryInfo? output)
        {
            if (!input.Exists) throw new ArgumentException("No file or directory given.");
            string outputArg = (output == null)
                ? input.FullName
                : ((output.Exists) ? output.FullName : throw new ArgumentException("Output directory is invalid."));
            Console.WriteLine($"Output folder : {outputArg}");
            switch (input)
            {
                case FileInfo f:
                    // TODO add keys :shrug:
                    if (Path.GetExtension(f.Name) == ".hca") throw new ArgumentException("File provided is not a .hca file.");
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

        private static void MergeFiles(string outputPath, string basename, string engine, bool subs)
        {
            Merger merger;
            switch (engine)
            {
                case "internal":
                    Console.WriteLine("Merging using the internal engine.");
                    // Video track is already added
                    merger = new GIMKV(basename, outputPath, "GI-Cutscenes v0.4.0", Path.Combine(outputPath, basename + ".ivf"));
                    break;
                case "mkvmerge":
                    Console.WriteLine("Merging using mkvmerge.");
                    merger = File.Exists(
                            Path.GetFileNameWithoutExtension(settings.MkvMergePath) == "mkvmerge" ? settings.MkvMergePath : "")
                        ? new Mkvmerge(Path.Combine(outputPath, basename + ".mkv"), settings.MkvMergePath) : new Mkvmerge(Path.Combine(outputPath, basename + ".mkv"));
                    merger.AddVideoTrack(Path.Combine(outputPath, basename + ".ivf"));
                    break;
                case "ffmpeg":
                    Console.WriteLine("Merging using ffmpeg.");
                    merger = File.Exists(
                        Path.GetFileNameWithoutExtension(settings.FfmpegPath) == "ffmpeg" ? settings.FfmpegPath : "") 
                        ? new FFMPEG(Path.Combine(outputPath, basename + ".mkv"), settings.FfmpegPath) : new FFMPEG(Path.Combine(outputPath, basename + ".mkv"));
                    merger.AddVideoTrack(Path.Combine(outputPath, basename + ".ivf"));
                    break;
                default:
                    throw new ArgumentException("Not implemented");
            }
            
            foreach (string f in Directory.EnumerateFiles(outputPath, $"{basename}_*.wav"))
            {
                if (!int.TryParse(Path.GetFileNameWithoutExtension(f)[^1..], out int language)) // Extracting language number from filename
                    throw new FormatException($"Unable to parse the language code from the file {Path.GetFileName(f)}.");
                merger.AddAudioTrack(f, language);
            }

            if (subs)
            {
                string subsFolder = settings.SubsFolder ?? throw new ArgumentException("Configuration value is not set for the key SubsFolder.");
                subsFolder = Path.GetFullPath(subsFolder);
                if (!Directory.Exists(subsFolder))
                    throw new ArgumentException(
                        "Path value for the key SubsFolder is invalid : Directory does not exist.");

                string subName = ASS.FindSubtitles(basename, subsFolder) ?? "";//throw new FileNotFoundException($"Subtitles could not be found for file {basename}");
                if (!string.IsNullOrEmpty(subName)) // Sometimes a cutscene has no subtitles (ChangeWeather), so we cna skip that part
                {
                    subName = Path.GetFileNameWithoutExtension(subName);
                    Console.WriteLine($"Subtitles name found: {subName}");
                    foreach (string d in Directory.EnumerateDirectories(subsFolder))
                    {
                        string lang = Path.GetFileName(d) ?? throw new DirectoryNotFoundException();
                        string[] search = Directory.GetFiles(d, $"{subName}_{lang}.*");
                        switch (search.Length)
                        {
                            case 0:
                                Console.WriteLine($"No subtitle for {subName} could be found for the language {lang}, skipping...");
                                break;
                            case 1:
                                ASS sub = new(search[0], lang);
                                string subFile = search[0];
                                if (!sub.IsAss())
                                {
                                    sub.ParseSrt();
                                    subFile = sub.ConvertToAss();
                                }

                                merger.AddSubtitlesTrack(subFile, lang);
                                break;
                            case 2:
                                string res = Array.Find(search, name => Path.GetExtension(name) == ".ass") ??
                                             throw new FileNotFoundException(
                                                 $"No ASS file could be found for the subs {subName}, but two files were matched previously, please report this case.");
                                merger.AddSubtitlesTrack(res, lang);
                                break;
                            default:
                                throw new Exception($"Too many results ({search.Length}), please report this case");
                        }
                    }

                    // Adding attachments
                    if (File.Exists("ja-jp.ttf")) merger.AddAttachment("ja-jp.ttf", "Japanese Font"); else Console.WriteLine("ja-jp.ttf font not found, skipping...");
                    if (File.Exists("zh-cn.ttf")) merger.AddAttachment("zh-cn.ttf", "Chinese Font"); else Console.WriteLine("zh-cn.ttf font not found, skipping...");
                }
                else Console.WriteLine($"No subtitles found for cutscene {basename}");
            }
            // Merging the file
            merger.Merge();
        }

        private static void CleanFiles(string outputPath, string basename)
        {
            string basePath = Path.Combine(outputPath, basename);
            // Removing corresponding video file
            if (File.Exists(basePath + ".ivf")) File.Delete(basePath + ".ivf");
            // Removing audio files
            foreach (string f in Directory.EnumerateFiles(outputPath, $"{basename}_*.hca")) File.Delete(f);
            foreach (string f in Directory.EnumerateFiles(outputPath, $"{basename}_*.wav")) File.Delete(f);
        }
    }
}
