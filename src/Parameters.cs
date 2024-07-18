using System.CommandLine;
using System.CommandLine.Binding;

namespace GICutscenes
{
    internal sealed class DemuxArgsOptions
    {
        public required FileSystemInfo input;
        public required DirectoryInfo output;
        public byte[]? key1;
        public byte[]? key2;
        public bool merge;
        public string? engine;
        public bool subs;
        public bool noCleanup;
        public string? audioFormat;
        public string? videoFormat;
        public string? audioLang;
    }

    internal sealed class DemuxArgsOptionsBinder(
        Argument<FileSystemInfo> inputArg,
        Option<DirectoryInfo> outputOption,
        Option<byte[]> key1Option,
        Option<byte[]> key2Option,
        Option<string> mkvEngineOption,
        Option<bool> mergeOption,
        Option<bool> subsOption,
        Option<bool> noCleanupOption,
        Option<string> audioFormatOption,
        Option<string> videoFormatOption,
        Option<string> audioLangOption
    ) : BinderBase<DemuxArgsOptions>
    {
        private Argument<FileSystemInfo> InputArg { get; } = inputArg;
        private Option<DirectoryInfo> OutputOption { get; } = outputOption;
        private Option<byte[]> Key1Option { get; } = key1Option;
        private Option<byte[]> Key2Option { get; } = key2Option;
        private Option<string> MkvEngineOption { get; } = mkvEngineOption;
        private Option<bool> MergeOption { get; } = mergeOption;
        private Option<bool> SubsOption { get; } = subsOption;
        private Option<bool> NoCleanupOption { get; } = noCleanupOption;
        private Option<string> AudioFormatOption { get; } = audioFormatOption;
        private Option<string> VideoFormatOption { get; } = videoFormatOption;
        private Option<string> AudioLangOption { get; } = audioLangOption;

        protected override DemuxArgsOptions GetBoundValue(BindingContext bindingContext) =>
            new DemuxArgsOptions
            {
                input = bindingContext.ParseResult.GetValueForArgument(InputArg),
                output =
                    bindingContext.ParseResult.GetValueForOption(OutputOption)
                    ?? new DirectoryInfo("./output"),
                key1 = bindingContext.ParseResult.GetValueForOption(Key1Option),
                key2 = bindingContext.ParseResult.GetValueForOption(Key2Option),
                subs = bindingContext.ParseResult.GetValueForOption(SubsOption),
                engine = bindingContext.ParseResult.GetValueForOption(MkvEngineOption),
                merge = bindingContext.ParseResult.GetValueForOption(MergeOption),
                noCleanup = bindingContext.ParseResult.GetValueForOption(NoCleanupOption),
                audioFormat = bindingContext.ParseResult.GetValueForOption(AudioFormatOption),
                videoFormat = bindingContext.ParseResult.GetValueForOption(VideoFormatOption),
                audioLang = bindingContext.ParseResult.GetValueForOption(AudioLangOption)
            };
    }
}
