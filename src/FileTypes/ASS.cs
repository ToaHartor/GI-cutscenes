using System.Text.RegularExpressions;

namespace GICutscenes.FileTypes
{
    internal class ASS
    {
        public static readonly string[] SubsExtensions = {".ass", ".srt", ".txt"};
        private readonly string _srt;
        private readonly string _fontname;
        private readonly List<string> _dialogLines;

        public ASS(string srtFile, string lang)
        {
            if (Path.GetExtension(srtFile) is not (".srt" or ".txt" or ".ass")) throw new FileLoadException("Wrong subtitles file type, requiring SRT file...");
            _srt = srtFile;
            _fontname = (lang == "JP") ? "SDK_JP_Web" : "SDK_SC_Web";
            _dialogLines = new List<string>();
        }

        public bool IsAss()
        {
            return _srt.ToLower().EndsWith(".ass") && (File.ReadLines(_srt).First() == "[Script Info]"); // Lazy checkup
        }

        public void ParseSrt()
        {
            string subsLines = File.ReadAllText(_srt); // No worries about the encoding, this is smart enough
            string[] splitLines = subsLines.ReplaceLineEndings().Split(Environment.NewLine);
            /* Dialogue line:
             * 0 -> Line number
             * 1 -> Timing of sub start - end (00:00:50,358 --> 00:00:51,225)
             * 2 -> Subtitle text (there can be two lines of subtitles)
             */
            //if (splitLines.Length % 3 != 0) throw new Exception($"Line count is invalid, got {splitLines.Length}");
            for (uint i = 0; i < splitLines.Length; i++)
            {
                if (i + 2 >= splitLines.Length) break; // Case when the last block has no line and timings (hi Ambor_Readings), therefore we skip
                if (!int.TryParse(splitLines[i], out _)) // If the next line is not a number 
                {
                    if (string.IsNullOrEmpty(splitLines[i])) continue; // Well, if it's empty, then we just have to skip and try again (Issue #30)
                    else throw new Exception("Dialogue block doesn't start with a number");
                }
                MatchCollection m = Regex.Matches(splitLines[i + 1], @"-?\d\d:\d\d:\d\d,\d\d");
                // We skip this iteration if there isn't any match : dialogue line is empty
                if (m.Count != 2) // throw new Exception($"Start and stop times couldn't be correctly parsed: {splitLines[i+1]}");
                {
                    i += 3;
                    continue;
                }
                string dialogLine = "Dialogue: 0,";

                foreach (Match m2 in m)
                {
                    dialogLine += (m2.Value.Replace("-0", "0").Replace(",", ".") + ",")[1..]; // Formatting correctly the match
                }

                dialogLine += "Default,,0,0,0,," + splitLines[i + 2];
                i += 2;
                // In case the subtitle text takes two lines
                if ((i + 1 < splitLines.Length) && (!string.IsNullOrWhiteSpace(splitLines[i + 1])))
                {
                    i += 1;
                    dialogLine += "\\n" + splitLines[i];
                }
                _dialogLines.Add(dialogLine);
                i += 1;
            }
        }

        public string ConvertToAss() {

            string filename = Path.ChangeExtension(_srt, ".ass");
            string header =
                @$"[Script Info]
; This is an Advanced Sub Station Alpha v4+ script.
ScriptType: v4.00+
Collisions: Normal
ScaledBorderAndShadow: yes
PlayDepth: 0

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,{_fontname},12,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100.0,100.0,0.0,0.0,1,0,0.5,2,10,10,14,1

[Events]
Format: Layer, Start, End, Style, Actor, MarginL, MarginR, MarginV, Effect, Text" + Environment.NewLine;
            File.WriteAllText(filename, header);
            string content = string.Join(Environment.NewLine, _dialogLines);
            // Correcting styles
            content = Regex.Replace(content, @"<([ubi])>", @"{\${1}1}");
            content = Regex.Replace(content, @"</([ubi])>", @"{\${1}0}");
            content = Regex.Replace(content, @"<font\s+color=""?#(\w{2})(\w{2})(\w{2})""?>", @"{\c&H$3$2$1&}");
            content = Regex.Replace(content, @"</font>", "");

            File.AppendAllText(filename, content);
            Console.WriteLine($"{_srt} converted to ASS");
            File.Delete(_srt); // Can be commented if you don't want to delete the original
            return filename;
        }

        public static string? FindSubtitles(string basename, string subsFolder)
        {
            // Hardcoded match fixes, as there is simply no way to deduce it
            basename = basename switch
            {
                "Cs_4131904_HaiDaoChuXian_Boy" => "Cs_Activity_4001103_Summertime_Boy",
                "Cs_4131904_HaiDaoChuXian_Girl" => "Cs_Activity_4001103_Summertime_Girl",
                "Cs_200211_WanYeXianVideo" => "Cs_DQAQ200211_WanYeXianVideo",
                _ => basename
            };

            string pathAttempt = Path.Combine(Path.GetFullPath(subsFolder), "EN"); // Taking the EN folder for instance, could be any of them...
            string[] search = Directory.GetFiles(pathAttempt, basename + "_EN.*").Select(name => Path.GetFileNameWithoutExtension(name)).Distinct().ToArray();
            if (search.Length == 1) // If the subtitle file is exactly the same
                return search[0][..^3];  // Removing the suffix "_EN.ext"

            // In case the subtitles are the same regardless of the Traveler's gender
            search = Directory.GetFiles(pathAttempt, basename.Replace("_Boy", "").Replace("_Girl", "") + "_EN.*").Select(name => Path.GetFileNameWithoutExtension(name)).Distinct().ToArray();
            if (search.Length == 1)
                return search[0][..^3];
            // Maybe more cases will be needed to fix this
            return null;
        }

        public static void ConvertAllSrt(string subsFolder)
        {
            string? file = null;
            //file = "ID/Cs_Inazuma_EQ4002207_ShikishogunRecalling_Boy_ID.txt";
            if (file == null)
            {
                foreach (string langDir in Directory.EnumerateDirectories(subsFolder))
                {
                    foreach (string srtFile in Directory.GetFiles(langDir, "*.txt"))
                    {
                        ASS newAss = new(srtFile, Path.GetDirectoryName(langDir) ?? "unk");
                        newAss.ParseSrt();
                        newAss.ConvertToAss();
                    }
                }
            }
            else
            {
                ASS newAss = new(Path.Combine(subsFolder, file), Path.GetDirectoryName(file) ?? "unk");
                newAss.ParseSrt();
                newAss.ConvertToAss();
            }

            Environment.Exit(0);
        }
    }
}
