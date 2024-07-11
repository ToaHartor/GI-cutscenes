using System;
using System.Collections.Generic;
using System.CommandLine.Binding;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GICutscenes
{
    internal sealed class DemuxOptions
    {
        public string? key1;
        public string? key2;
        public DirectoryInfo? output;
        public string? engine;
        public bool? merge;
        public bool? subs;
        public bool? noCleanup;
        public string? audioFormat;
        public string? videoFormat;
        public string? audioLang;
    }

    internal sealed class DemuxOptionsBinder(Option<string> key1Option, Option<string> key2Option, Option<DirectoryInfo> outputFolderOption, Option<string> mkvEngineOption, Option<bool> mergeOption, Option<bool> subsOption, Option<bool> noCleanupOption, Option<string> audioFormatOption, Option<string> videoFormatOption, Option<string> audioLangOption) : BinderBase<DemuxOptions>
    {
        private Option<string> Key1Option { get; } = key1Option;
        private Option<string> Key2Option { get; } = key2Option;
        private Option<DirectoryInfo> OutputFolderOption { get; } = outputFolderOption;
        private Option<string> MkvEngineOption { get; } = mkvEngineOption;
        private Option<bool> MergeOption { get; } = mergeOption;
        private Option<bool> SubsOption { get; } = subsOption;
        private Option<bool> NoCleanupOption { get; } = noCleanupOption;
        private Option<string> AudioFormatOption { get; } = audioFormatOption;
        private Option<string> VideoFormatOption { get; } = videoFormatOption;
        private Option<string> AudioLangOption { get; } = audioLangOption;

        protected override DemuxOptions GetBoundValue(BindingContext bindingContext) =>
            new DemuxOptions
            {
                key1 = bindingContext.ParseResult.GetValueForOption(Key1Option),
                key2 = bindingContext.ParseResult.GetValueForOption(Key2Option),
                output = bindingContext.ParseResult.GetValueForOption(OutputFolderOption),
                engine = bindingContext.ParseResult.GetValueForOption(MkvEngineOption),
                merge = bindingContext.ParseResult.GetValueForOption(MergeOption),
                subs = bindingContext.ParseResult.GetValueForOption(SubsOption),
                noCleanup = bindingContext.ParseResult.GetValueForOption(NoCleanupOption),
                audioFormat = bindingContext.ParseResult.GetValueForOption(AudioFormatOption),
                videoFormat = bindingContext.ParseResult.GetValueForOption(VideoFormatOption),
                audioLang = bindingContext.ParseResult.GetValueForOption(AudioLangOption)
            };
    }

    internal sealed class BatchDemuxOptions
    {
        public DirectoryInfo? output;
        public bool? subs;
        public bool? merge;
        public string? engine;
        public string? audioFormat;
        public string? videoFormat;
        public bool? noCleanup;
        public string? audioLang;
    }

    internal sealed class BatchDemuxOptionsBinder(Option<DirectoryInfo> outputFolderOption, Option<string> mkvEngineOption, Option<bool> mergeOption, Option<bool> subsOption, Option<bool> noCleanupOption, Option<string> audioFormatOption, Option<string> videoFormatOption, Option<string> audioLangOption) : BinderBase<BatchDemuxOptions> {
        private Option<DirectoryInfo> OutputFolderOption { get; } = outputFolderOption;
        private Option<bool> SubsOption { get; } = subsOption;
        private Option<bool> MergeOption { get; } = mergeOption;
        private Option<string> MkvEngineOption { get; } = mkvEngineOption;
        private Option<string> AudioFormatOption { get; } = audioFormatOption;
        private Option<string> VideoFormatOption { get; } = videoFormatOption;
        private Option<bool> NoCleanupOption { get; } = noCleanupOption;
        private Option<string> AudioLangOption { get; } = audioLangOption;

        protected override BatchDemuxOptions GetBoundValue(BindingContext bindingContext) =>
            new BatchDemuxOptions
            {
                output = bindingContext.ParseResult.GetValueForOption(OutputFolderOption),
                engine = bindingContext.ParseResult.GetValueForOption(MkvEngineOption),
                merge = bindingContext.ParseResult.GetValueForOption(MergeOption),
                subs = bindingContext.ParseResult.GetValueForOption(SubsOption),
                noCleanup = bindingContext.ParseResult.GetValueForOption(NoCleanupOption),
                audioFormat = bindingContext.ParseResult.GetValueForOption(AudioFormatOption),
                videoFormat = bindingContext.ParseResult.GetValueForOption(VideoFormatOption),
                audioLang = bindingContext.ParseResult.GetValueForOption(AudioLangOption)
            };
    }
}
