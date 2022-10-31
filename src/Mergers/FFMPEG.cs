using System.Diagnostics;
using GICutscenes.FileTypes;

namespace GICutscenes.Mergers
{
    internal class FFMPEG : Merger
    {
        private readonly string _ffmpeg;
        private string _command;
        private readonly string _output;
        private int _videoCount;
        private int _audioCount;
        private int _subsCount;
        private int _attachmentCount;
        private readonly List<string> _inputOptions;
        private readonly List<string> _mapOptions;
        private readonly List<string> _metadataOptions;

        public FFMPEG(string output)
        {
            // If not provided, ffmpeg binary might directly be in the PATH
            string ffmpegPath;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    ffmpegPath = "ffmpeg.exe";
                    break;
                case PlatformID.MacOSX: // might work like Unix
                case PlatformID.Unix:
                    ffmpegPath = "ffmpeg";  // Provided the binary is in the PATH
                    break;
                default:
                    throw new PlatformNotSupportedException("Default ffmpeg path for this OS isn't registered...");
            }
            _ffmpeg = ffmpegPath;

            _output = output;
            _videoCount = 0;
            _audioCount = 0;
            _subsCount = 0;
            _attachmentCount = 0;
            _inputOptions = new List<string>();
            _mapOptions = new List<string>();
            _metadataOptions = new List<string>();
            _command = "-y -loglevel quiet -nostats";

        }

        public FFMPEG(string output, string ffmpegPath)
        {
            if (!File.Exists(ffmpegPath))
                throw new FileNotFoundException("ffmpeg couldn't be found in the given path");
            _ffmpeg = ffmpegPath;
            _output = output;
            _videoCount = 0;
            _audioCount = 0;
            _subsCount = 0;
            _attachmentCount = 0;
            _inputOptions = new List<string>();
            _mapOptions = new List<string>();
            _metadataOptions = new List<string>();
            _command = "-y -loglevel quiet -nostats";
        }

        public void AddAttachment(string attachment, string description)
        {
            if (!File.Exists(attachment)) throw new FileNotFoundException($"Attachment file {attachment} not found.");
            _inputOptions.Add($" -attach \"{attachment}\"");
            _metadataOptions.Add($" -metadata:s:t:{_attachmentCount} mimetype=application/x-truetype-font -metadata:s:t:{_attachmentCount} description=\"{description}\"");
            _attachmentCount++;
        }

        public void AddAudioTrack(string audioFile, int lang)
        {
            if (!File.Exists(audioFile)) throw new FileNotFoundException($"Audio file {audioFile} not found.");
            if (!Enumerable.Range(0, 4).Contains(lang)) throw new Exception($"Language number {lang} not supported");
            _inputOptions.Add($" -i \"{audioFile}\"");
            _mapOptions.Add($" -map {_videoCount + _audioCount + _subsCount}");
            _metadataOptions.Add($" -metadata:s:a:{_audioCount} language={MKV.AudioLang[lang].Item2} -metadata:s:a:{_audioCount} title=\"{MKV.AudioLang[lang].Item1}\"");
            _audioCount++;
        }

        public void AddSubtitlesTrack(string subFile, string language)
        {
            if (!MKV.SubsLang.ContainsKey(language)) throw new Exception($"Language code {language} isn't supported...");
            _inputOptions.Add($" -i \"{subFile}\"");
            _mapOptions.Add($" -map {_videoCount + _audioCount + _subsCount}");
            _metadataOptions.Add($" -metadata:s:s:{_subsCount} language={MKV.SubsLang[language].Item1} -metadata:s:s:{_subsCount} title=\"{MKV.SubsLang[language].Item2}\"");
            _subsCount++;
        }

        public void AddVideoTrack(string videoFile)
        {
            if (!File.Exists(videoFile)) throw new FileNotFoundException($"Video file {videoFile} not found.");
            string name = Path.GetFileNameWithoutExtension(videoFile);
            _inputOptions.Add($" -i \"{videoFile}\"");
            _mapOptions.Add($" -map {_videoCount + _audioCount + _subsCount}");
            _metadataOptions.Add($" -metadata:s:v:{_videoCount} language=und -metadata:s:v:{_videoCount} title=\"{name}\"");
            _videoCount++;
        }

        public void Merge()
        {
            _command += string.Join(" ", _inputOptions) + string.Join(" ", _mapOptions) + string.Join(" ", _metadataOptions);
            _command += $" -c copy \"{_output}\"";
            //Console.WriteLine(_ffmpeg + _command);
            Process process = Process.Start(_ffmpeg, _command);
            process.WaitForExit();
        }

        public void Merge(string audioFormat, string videoFormat)
        {
            _command += string.Join(" ", _inputOptions) + string.Join(" ", _mapOptions) + string.Join(" ", _metadataOptions);
            _command += $" -c:a \"{(string.IsNullOrWhiteSpace(audioFormat) ? "copy" : audioFormat)}\" -c:v \"{(string.IsNullOrWhiteSpace(videoFormat) ? "copy" : videoFormat)}\" \"{_output}\"";
            //Console.WriteLine(_ffmpeg + _command);
            Process process = Process.Start(_ffmpeg, _command);
            process.WaitForExit();
        }
    }
}
