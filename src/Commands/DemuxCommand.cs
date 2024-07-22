using System.CommandLine;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using GICutscenes.FileTypes;
using GICutscenes.Mergers;
using GICutscenes.Mergers.GIMKV;

namespace GICutscenes;

public class DemuxCommand : Command
{
    public DemuxCommand()
        : base("demux", "Demuxes USM files from GI in accordance to the known keys")
    {
        // CLI Options
        Argument<FileSystemInfo> demuxInputArg = new Argument<FileSystemInfo>(
            name: "Input file or folder",
            description: "The file to read and display on the console."
        );
        AddArgument(demuxInputArg);

        CliOptions.Output.AddAlias("-o");
        AddOption(CliOptions.Output);
        AddOption(CliOptions.HexKey);
        AddOption(CliOptions.Key1);
        AddOption(CliOptions.Key2);
        CliOptions.Subs.AddAlias("--subtitles");
        CliOptions.Subs.AddAlias("-s");
        AddOption(CliOptions.Subs);
        CliOptions.NoCleanup.AddAlias("-nc");
        AddOption(CliOptions.NoCleanup);
        CliOptions.AudioLang.AddAlias("-al");
        AddOption(CliOptions.AudioLang);
        CliOptions.Merge.AddAlias("-m");
        AddOption(CliOptions.Merge);
        CliOptions.MkvEngine.AddAlias("-e");
        AddOption(CliOptions.MkvEngine);
        CliOptions.AudioFormat.AddAlias("-af");
        AddOption(CliOptions.AudioFormat);
        CliOptions.VideoFormat.AddAlias("-vf");
        AddOption(CliOptions.VideoFormat);

        DemuxArgsOptionsBinder demuxArgsOptions = new DemuxArgsOptionsBinder(
            demuxInputArg,
            CliOptions.Output,
            CliOptions.HexKey,
            CliOptions.Key1,
            CliOptions.Key2,
            CliOptions.MkvEngine,
            CliOptions.Merge,
            CliOptions.Subs,
            CliOptions.NoCleanup,
            CliOptions.AudioFormat,
            CliOptions.VideoFormat,
            CliOptions.AudioLang
        );

        this.SetHandler(Execute, demuxArgsOptions);
    }

    [RequiresUnreferencedCode("Calls GICutscenes.Program.ReadSetting()")]
    private static void Execute(DemuxArgsOptions demuxArgsOptions)
    {
        Program.ReadSetting();
        FileSystemInfo input = demuxArgsOptions.input;
        // Check given arguments and options
        if (!input.Exists)
            throw new ArgumentNullException($"Input path does not exist: {input.FullName}");

        DirectoryInfo output = demuxArgsOptions.output;
        if (!output.Exists)
            output.Create();

        byte[] key1 = demuxArgsOptions.key1 ?? Array.Empty<byte>();
        byte[] key2 = demuxArgsOptions.key2 ?? Array.Empty<byte>();

        if (demuxArgsOptions.hexKey != null)
        {
            key1 = demuxArgsOptions.hexKey[..4];
            key2 = demuxArgsOptions.hexKey[4..];
        }

        bool merge = demuxArgsOptions.merge;
        bool includeSubs = demuxArgsOptions.subs;
        bool noCleanup = demuxArgsOptions.noCleanup;

        string audioFormat = demuxArgsOptions.audioFormat;
        string videoFormat = demuxArgsOptions.videoFormat;
        string audioLang = demuxArgsOptions.audioLang;
        // TODO : Manage engine depending on the output type
        string engine = demuxArgsOptions.engine;

        Stopwatch timer;
        // Check input: file or folder
        switch (input)
        {
            case FileInfo inputFile:
                if (!input.Name.EndsWith(".usm"))
                    throw new ArgumentException($"File {input.Name} isn't a .usm file.");
                timer = Stopwatch.StartNew();
                DemuxUsm(
                    input: inputFile,
                    output,
                    key1,
                    key2,
                    doMerge: merge,
                    mkvEngine: engine,
                    includeSubs,
                    noCleanup,
                    audioFormat,
                    videoFormat,
                    audioLang
                );
                timer.Stop();
                break;
            case DirectoryInfo inputDir:
                // if (!IsDirectory(output)) throw new ArgumentNullException($"Output is not a folder: {output.FullName}");

                // Output and input are directories, use the same command as demuxing in bash
                timer = Stopwatch.StartNew();
                foreach (FileInfo f in inputDir.EnumerateFiles("*.usm"))
                {
                    DemuxUsm(
                        input: f,
                        output,
                        key1,
                        key2,
                        doMerge: merge,
                        mkvEngine: engine,
                        includeSubs,
                        noCleanup,
                        audioFormat,
                        videoFormat,
                        audioLang
                    );
                }
                timer.Stop();

                break;
            default:
                // Should not happen
                // TODO : Throw exception here
                Console.WriteLine("Input is not a valid file or directory name.");
                return;
        }
        Console.WriteLine($"{timer.ElapsedMilliseconds}ms elapsed");
    }

    private static void DemuxUsm(
        FileInfo input,
        DirectoryInfo output,
        byte[] key1,
        byte[] key2,
        bool doMerge,
        string mkvEngine,
        bool includeSubs,
        bool noCleanup,
        string audioFormat,
        string videoFormat,
        string audioLang
    )
    {
        bool demuxed = Demuxer.Demux(input, key1, key2, output);
        // No merge if demux is unsuccessful or if merge not wanted
        if (!demuxed || !doMerge)
            return;

        string basename = Path.GetFileNameWithoutExtension(input.Name);

        MergeFiles(output, basename, mkvEngine, includeSubs, audioFormat, videoFormat, audioLang);
        if (!noCleanup)
            CleanExtractedFiles(output.FullName, basename);

        string subsFolderPath = Path.Combine(output.FullName, "Subs");
        if (!noCleanup && Directory.Exists(subsFolderPath))
            Directory.Delete(subsFolderPath, true);
    }

    private static void MergeFiles(
        DirectoryInfo output,
        string basename,
        string? engine,
        bool subs,
        string audioFormat,
        string videoFormat,
        string audioLang
    )
    {
        Settings settings = Program.settings;
        Merger merger;
        string outputPath = output.FullName;
        // If either audio or video format is defined, use ffmpeg
        if (!string.IsNullOrWhiteSpace(audioFormat) || !string.IsNullOrWhiteSpace(videoFormat))
        {
            engine = "ffmpeg";
        }
        switch (engine)
        {
            case "internal":
                Console.WriteLine("Merging using the internal engine.");
                // Video track is already added
                merger = new GIMKV(
                    basename,
                    outputPath,
                    "GI-Cutscenes v0.5.0",
                    Path.Combine(outputPath, basename + ".ivf")
                );
                break;
            case "mkvmerge":
                Console.WriteLine("Merging using mkvmerge.");
                merger = File.Exists(
                    Path.GetFileNameWithoutExtension(settings?.MkvMergePath)?.ToLower()
                    == "mkvmerge"
                        ? settings?.MkvMergePath
                        : ""
                )
                    ? new Mkvmerge(
                        Path.Combine(outputPath, basename + ".mkv"),
                        settings!.MkvMergePath!
                    )
                    : new Mkvmerge(Path.Combine(outputPath, basename + ".mkv"));
                merger.AddVideoTrack(Path.Combine(outputPath, basename + ".ivf"));
                break;
            case "ffmpeg":
                Console.WriteLine("Merging using ffmpeg.");
                merger = File.Exists(
                    Path.GetFileNameWithoutExtension(settings?.FfmpegPath)?.ToLower() == "ffmpeg"
                        ? settings?.FfmpegPath
                        : ""
                )
                    ? new FFMPEG(Path.Combine(outputPath, basename + ".mkv"), settings!.FfmpegPath!)
                    : new FFMPEG(Path.Combine(outputPath, basename + ".mkv"));
                merger.AddVideoTrack(Path.Combine(outputPath, basename + ".ivf"));
                break;
            default:
                throw new ArgumentException("Not implemented");
        }

        foreach (string f in Directory.EnumerateFiles(outputPath, $"{basename}_*.wav"))
        {
            if (!int.TryParse(Path.GetFileNameWithoutExtension(f)[^1..], out int language)) // Extracting language number from filename
                throw new FormatException(
                    $"Unable to parse the language code from the file {Path.GetFileName(f)}."
                );

            string[] langs = audioLang.Split(',');
            if (language > 3 || !langs.Contains(MKV.AudioLang[language].Item2))
                continue;

            merger.AddAudioTrack(f, language);
        }

        if (subs)
        {
            string subsFolder =
                settings?.SubsFolder
                ?? throw new ArgumentException(
                    "Configuration value is not set for the key SubsFolder."
                );
            subsFolder = Path.GetFullPath(subsFolder);
            if (!Directory.Exists(subsFolder))
                throw new ArgumentException(
                    "Path value for the key SubsFolder is invalid : Directory does not exist."
                );

            string subName = ASS.FindSubtitles(basename, subsFolder) ?? ""; //throw new FileNotFoundException($"Subtitles could not be found for file {basename}");
            if (!string.IsNullOrEmpty(subName)) // Sometimes a cutscene has no subtitles (ChangeWeather), so we cna skip that part
            {
                subName = Path.GetFileNameWithoutExtension(subName);
                Console.WriteLine($"Subtitles name found: {subName}");
                foreach (string d in Directory.EnumerateDirectories(subsFolder))
                {
                    string lang = Path.GetFileName(d) ?? throw new DirectoryNotFoundException();
                    string[] search = Directory
                        .GetFiles(d, $"{subName}_{lang}.*")
                        .OrderBy(f => f)
                        .ToArray(); // Sorting by name
                    switch (search.Length)
                    {
                        case 0:
                            Console.WriteLine(
                                $"No subtitle for {subName} could be found for the language {lang}, skipping..."
                            );
                            break;
                        case 1:
                        // Could be either the presence of both .srt and .txt files (following the 3.0 release), but also .ass
                        case 2:
                        // Might be .srt+.txt+.ass
                        case 3:
                            // The "search" array is sorted by name, which means that the file order would be ASS > SRT > TXT
                            string res =
                                Array.Find(
                                    search,
                                    name => ASS.SubsExtensions.Contains(Path.GetExtension(name))
                                )
                                ?? throw new FileNotFoundException(
                                    $"No valid file could be found for the subs {subName} while the files corresponding to the name is {search.Length}"
                                );
                            ;
                            Console.WriteLine($"Using subs file {Path.GetFileName(res)}");
                            ASS sub = new(res, lang);
                            string subFile = search[0];
                            if (!sub.IsAss())
                            {
                                sub.ParseSrt();
                                subFile = sub.ConvertToAss(outputPath);
                            }

                            merger.AddSubtitlesTrack(subFile, lang);
                            break;
                        default:
                            throw new Exception(
                                $"Too many results ({search.Length}), please report this case"
                            );
                    }
                }

                // Adding attachments
                if (File.Exists("ja-jp.ttf"))
                    merger.AddAttachment("ja-jp.ttf", "Japanese Font");
                else
                    Console.WriteLine("ja-jp.ttf font not found, skipping...");
                if (File.Exists("zh-cn.ttf"))
                    merger.AddAttachment("zh-cn.ttf", "Chinese Font");
                else
                    Console.WriteLine("zh-cn.ttf font not found, skipping...");
            }
            else
                Console.WriteLine($"No subtitles found for cutscene {basename}");
        }
        // Merging the file
        if (!string.IsNullOrWhiteSpace(audioFormat) || !string.IsNullOrWhiteSpace(videoFormat))
        {
            merger.Merge(audioFormat, videoFormat);
        }
        else
        {
            merger.Merge();
        }
    }

    private static void CleanExtractedFiles(string outputPath, string basename)
    {
        string basePath = Path.Combine(outputPath, basename);
        // Removing corresponding video file
        if (File.Exists(basePath + ".ivf"))
            File.Delete(basePath + ".ivf");
        // Removing audio files
        foreach (string f in Directory.EnumerateFiles(outputPath, $"{basename}_*.hca"))
            File.Delete(f);
        foreach (string f in Directory.EnumerateFiles(outputPath, $"{basename}_*.wav"))
            File.Delete(f);
    }
}
