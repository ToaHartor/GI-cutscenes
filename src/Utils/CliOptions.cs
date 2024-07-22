using System.Buffers.Binary;
using System.CommandLine;

namespace GICutscenes;

public static class CliOptions
{
    public static Option<DirectoryInfo> Output = new Option<DirectoryInfo>(
        name: "--output",
        description: "Output folder",
        getDefaultValue: () => new DirectoryInfo("./output")
    );

    public static Option<byte[]> HexKey = new Option<byte[]>(
        name: "--key",
        description: "USM encryption key (hexadecimal and number format are supported). Overrides options '-a' and '-b'",
        parseArgument: result =>
        {
            string strKey = result.Tokens.Single().Value;
            // If number only || If hex (0x before or letters A-F)
            if (
                ulong.TryParse(strKey, out ulong numKey)
                || ulong.TryParse(
                    strKey.StartsWith("0x") ? strKey.Substring(2) : strKey,
                    System.Globalization.NumberStyles.HexNumber,
                    null,
                    out numKey
                )
            )
            {
                byte[] byteKey = new byte[8];
                BitConverter.GetBytes(numKey).CopyTo(byteKey, 0);
                return byteKey;
            }
            throw new ArgumentException("Argument --key <key> does not have the right format");
        }
    );

    public static Option<byte[]> Key1 = new Option<byte[]>(
        name: "-a",
        description: "4 lower bytes of the key (hex format)",
        parseArgument: result =>
        {
            // Entry is a hex string
            return Convert.FromHexString(result.Tokens.Single().Value);
        }
    );

    public static Option<byte[]> Key2 = new Option<byte[]>(
        name: "-b",
        description: "4 higher bytes of the key (hex format)",
        parseArgument: result =>
        {
            // Entry is a hex string
            return Convert.FromHexString(result.Tokens.Single().Value);
        }
    );

    public static Option<bool> Subs = new Option<bool>(
        name: "--subs",
        description: "Adds the subtitles to the MKV file.",
        getDefaultValue: () => false
    );

    public static Option<bool> NoCleanup = new Option<bool>(
        name: "--no-cleanup",
        description: "Keeps the extracted files instead of removing them.",
        getDefaultValue: () => false
    );

    public static Option<string> AudioLang = new Option<string>(
        name: "--audio-lang",
        description: $"Select audio languages that you wish to include in MKV file.{Environment.NewLine}Example \"eng,jpn\")",
        getDefaultValue: () => "chi,eng,jpn,kor"
    );

    public static Option<bool> Merge = new Option<bool>(
        name: "--merge",
        description: "Merges the extracted content into a MKV container file.",
        getDefaultValue: () => false
    );

    public static Option<string> MkvEngine = new Option<string>(
        name: "--mkv-engine",
        description: "Merges the extracted content into a MKV container file.",
        getDefaultValue: () => "internal"
    ).FromAmong("mkvmerge", "internal", "ffmpeg");

    public static Option<string> AudioFormat = new Option<string>(
        name: "--audio-format",
        description: "Audio encode format in MKV file, the original is PCM."
    );

    public static Option<string> VideoFormat = new Option<string>(
        name: "--video-format",
        description: "Video encode format in MKV file, the original is VP9."
    );

    public static Option<bool> NotOpenBrowser = new Option<bool>(
        name: "--no-browser",
        description: "Do not open browser if there's new version."
    );

    public static Option<string> Proxy = new Option<string>(
        name: "--proxy",
        description: "Specifies a proxy server for the request."
    );
}
