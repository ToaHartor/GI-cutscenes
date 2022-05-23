using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace GICutscenes.FileTypes
{
    internal class MKV
    {
        private readonly string _mkvmerge;
        private string _command;
        public static readonly Dictionary<string, (string, string)> SubsLang = new()
        {
            {"CHS", ("chi", "Chinese (Simplified)")},
            {"CHT", ("chi", "Chinese (Traditional)")},
            {"DE", ("ger", "German")},
            {"EN", ("eng", "English")},
            {"ES", ("spa", "Spanish")},
            {"FR", ("fre", "French")},
            {"ID", ("ind", "Indonesian")},
            {"JP", ("jpn", "Japanese")},
            {"KR", ("kor", "Korean")},
            {"PT", ("por", "Portuguese")},
            {"RU", ("rus", "Russian")},
            {"TH", ("tha", "Thai")},
            {"VI", ("vie", "Vietnamese")}
        };

        public static readonly (string, string)[] AudioLang = 
        {
            ("Chinese", "chi"), 
            ("English", "eng"), 
            ("Japanese", "jpn"), 
            ("Korean", "kor")
        };

        public MKV(string output)
        {
            string mkvmergePath;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    mkvmergePath = Path.GetFullPath(@"C:\Program Files\MKVToolNix\mkvmerge.exe");
                    if (!File.Exists(mkvmergePath))
                        throw new FileNotFoundException("mkvmerge.exe couldn't be found in the default installation path");
                    break;
                case PlatformID.MacOSX: // might work like Unix
                case PlatformID.Unix:
                    mkvmergePath = "mkvmerge";  // Provided the binary is in the PATH
                    break;
                case PlatformID.Other:
                default:
                    throw new PlatformNotSupportedException("Default mkvmerge path for this OS isn't registered...");
            }
            _mkvmerge = mkvmergePath;
            if (!output.EndsWith(".mkv")) throw new ArgumentException("Output file provided to mkvmerge isn't valid.");
            _command = $@"-q -o ""{output}""";  // -q is for quiet mode
        }

        public MKV(string output, string mkvmergePath)
        {
            _mkvmerge = mkvmergePath;
            if (!output.EndsWith(".mkv")) throw new ArgumentException("Output file provided to mkvmerge isn't valid.");
            _command = $@"-q -o ""{output}""";
        }


        public void AddVideoTrack(string videoFile)
        {
            if (!File.Exists(videoFile)) throw new FileNotFoundException($"Video file {videoFile} not found.");
            string name = Path.GetFileNameWithoutExtension(videoFile);
            _command += $@" --track-name 0:""{name}"" --default-track 0:0 --forced-track 0:0 -d 0 -A -S ""{videoFile}""";
        }

        public void AddAudioTrack(string audioFile, int lang)
        {
            if (!File.Exists(audioFile)) throw new FileNotFoundException($"Audio file {audioFile} not found.");
            if (!Enumerable.Range(0, 4).Contains(lang)) throw new Exception($"Language number {lang} not supported");
            _command += $@" --track-name 0:""{AudioLang[lang].Item1}"" --language 0:{AudioLang[lang].Item2} --default-track 0:0 --forced-track 0:0 -D -a 0 -S ""{audioFile}""";
        }

        public void AddSubtitlesTrack(string subFile, string giLanguage)
        {
            if (!SubsLang.ContainsKey(giLanguage)) throw new Exception($"Language code {giLanguage} isn't supported...");
            _command += $@" --track-name 0:""{SubsLang[giLanguage].Item2}"" --language 0:{SubsLang[giLanguage].Item1} --default-track 0:0 --forced-track 0:0 -D -A -s 0 ""{subFile}""";
        }

        public void AddAttachment(string attachement, string description)
        {
            if (!File.Exists(attachement)) throw new FileNotFoundException($"Attachment file {attachement} not found.");
            _command += $@" --attachment-description ""{description}"" --attach-file ""{attachement}""";
        }

        public void Merge()
        {
            //Console.WriteLine(_command);
            Process process = Process.Start(_mkvmerge, _command);
            process.WaitForExit();
        }
    }
}
