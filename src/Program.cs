using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using CRIDemuxer.FileTypes;

namespace CRIDemuxer
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
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
                description: "Output folder"
                );
            outputFolderOption.AddAlias("-o");

            var key1Option = new Option<string?>(
                name: "-a",
                description: "4 lower bytes of the key");

            var key2Option = new Option<string?>(
                name: "-b",
                description: "4 higher bytes of the key");

            var rootCommand = new RootCommand("Sample app for System.CommandLine");
            rootCommand.AddGlobalOption(outputFolderOption);


            var demuxUsmCommand = new Command("demuxUsm", "Demuxes a specified .usm file to a specified folder")
            {
                demuxFileOption,
                key1Option,
                key2Option
            };

            var batchDemuxCommand = new Command("batchDemux", "Tries to demux all .usm files in the specified folder")
            {   
                usmFolderArg
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

            demuxUsmCommand.SetHandler(async (FileInfo file, string key1, string key2, DirectoryInfo output) =>
            {
                await DemuxUsmCommand(file, key1, key2, output);
            },
            demuxFileOption, key1Option, key2Option, outputFolderOption);

            batchDemuxCommand.SetHandler(async (DirectoryInfo inputDir, DirectoryInfo? outputDir) =>
            {
                await BatchDemuxCommand(inputDir, outputDir);
            }, usmFolderArg, outputFolderOption);

            convertHcaCommand.SetHandler(async (FileSystemInfo input, DirectoryInfo? output) =>
            {
                await ConvertHcaCommand(input, output);
            }, hcaInputArg, outputFolderOption);

            return rootCommand.InvokeAsync(args).Result;
        }

        private static async Task DemuxUsmCommand(FileInfo file, string key1, string key2, DirectoryInfo output)
        {
            if (file == null) throw new ArgumentNullException("No file provided.");
            if (!file.Exists) throw new ArgumentException("File {0} does not exist.", file.Name);
            if (key1!=null && key2!= null && (key1.Length != 8 || key2.Length != 8)) throw new ArgumentException("Keys are invalid.");
            string outputArg = (output == null)
                ? file.Directory.FullName
                : ((output.Exists) ? output.FullName : throw new ArgumentException("Output directory is invalid."));
            Console.WriteLine($"Output folder : {outputArg}");
            byte[] key1Arg = Convert.FromHexString(key1 ?? "");
            byte[] key2Arg = Convert.FromHexString(key2 ?? "");
            Demuxer.Demux(file.FullName, key1Arg, key2Arg, outputArg);
        }

        private static async Task BatchDemuxCommand(DirectoryInfo inputDir, DirectoryInfo? outputDir)
        {
            if (inputDir == null || !inputDir.Exists) throw new ArgumentNullException("Input directory is invalid.");
            string outputArg = (outputDir == null)
                ? inputDir.FullName
                : ((outputDir.Exists) ? outputDir.FullName : throw new ArgumentException("Output directory is invalid."));
            Console.WriteLine($"Output folder : {outputArg}");
            foreach (string f in Directory.EnumerateFiles(inputDir.FullName, "*.usm"))
            {
                Demuxer.Demux(f, Array.Empty<byte>(), Array.Empty<byte>(), outputArg);
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
                    if (f.Name.EndsWith(".hca")) throw new ArgumentException("File provided is not a .hca file.");
                    HCA file = new(f.FullName);
                    file.ConvertToWAV(outputArg);
                    break;
                case DirectoryInfo directory:
                    foreach (string f in Directory.EnumerateFiles(directory.FullName, "*.hca"))
                    {
                        HCA singleFile = new(f);
                        singleFile.ConvertToWAV(outputArg);
                    }
                    break;
                default:
                    Console.WriteLine("Not a valid file or directory name.");
                    break;
            }
        }
    }
}
