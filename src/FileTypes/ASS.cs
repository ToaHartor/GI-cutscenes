using System.Text.RegularExpressions;

namespace GICutscenes.FileTypes
{
    internal class ASS
    {
        private readonly string _srt;
        private readonly string _fontname;
        private List<string> _dialogLines;

        public ASS(string srtFile, string fontname)
        {
            if (!srtFile.EndsWith(".srt") && !srtFile.EndsWith(".txt")) throw new FileLoadException("Wrong subtitles file type, requiring SRT file...");
            _srt = srtFile;
            _fontname = fontname;
            _dialogLines = new List<string>();
        }

        public void ParseSrt()
        {
            string subsLines = File.ReadAllText(_srt); // No worries about the encoding, this is smart enough
            string[] splitLines = subsLines.ReplaceLineEndings().Split(Environment.NewLine).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();  // Removing useless empty lines
            /* Dialogue line:
             * 0 -> Line number
             * 1 -> Timing of sub start - end (00:00:50,358 --> 00:00:51,225)
             * 2 -> Subtitle text (there can be two lines of subtitles)
             */
            //if (splitLines.Length % 3 != 0) throw new Exception($"Line count is invalid, got {splitLines.Length}");
            for (uint i = 0; i < splitLines.Length; i++)
            {
                if (!int.TryParse(splitLines[i], out _)) throw new Exception("Dialogue block doesn't start with a number");
                MatchCollection m = Regex.Matches(splitLines[i + 1], @"-?\d\d:\d\d:\d\d,\d\d");
                if (m.Count != 2) throw new Exception($"Start and stop times couldn't be correctly parsed: {splitLines[i+1]}");
                string dialogLine = "Dialogue: 0,";

                foreach (Match m2 in m)
                {
                    dialogLine += (m2.Value.Replace("-0", "0").Replace(",", ".") + ",")[1..]; // Formatting correctly the match
                }

                dialogLine += "Default,,0,0,0,," + splitLines[i + 2];
                i += 2;
                // In case the subtitle text takes two lines
                if ((i + 1 < splitLines.Length) && (!int.TryParse(splitLines[i + 1], out _)))
                {
                    i += 1;
                    dialogLine += "\\n" + splitLines[i];
                }
                _dialogLines.Add(dialogLine);
            }
        }

        public void ConvertToAss()
        {
            string filename = _srt.Substring(0, _srt.Length - 4) + ".ass";
            string header =
                @$"[Script Info]
; This is an Advanced Sub Station Alpha v4+ script.
ScriptType: v4.00+
Collisions: Normal
ScaledBorderAndShadow: yes
PlayDepth: 0

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,{_fontname},18,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100.0,100.0,0.0,0.0,1,0,0.5,2,10,10,20,1

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
            //File.Delete(_srt);
        }
    }
}
