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

    internal sealed class DemuxOptionsBinder : BinderBase<DemuxOptions>
    {
        private Option<string> Key1Option { get; }
        private Option<string> Key2Option { get; }
        private Option<DirectoryInfo> OutputFolderOption { get; }
        private Option<string> MkvEngineOption { get; }
        private Option<bool> MergeOption { get; }
        private Option<bool> SubsOption { get; }
        private Option<bool> NoCleanupOption { get; }
        private Option<string> AudioFormatOption { get; }
        private Option<string> VideoFormatOption { get; }
        private Option<string> AudioLangOption { get; }

        public DemuxOptionsBinder(Option<string> key1Option, Option<string> key2Option, Option<DirectoryInfo> outputFolderOption, Option<string> mkvEngineOption, Option<bool> mergeOption, Option<bool> subsOption, Option<bool> noCleanupOption, Option<string> audioFormatOption, Option<string> videoFormatOption, Option<string> audioLangOption)
        {
            Key1Option = key1Option;
            Key2Option = key2Option;
            OutputFolderOption = outputFolderOption;
            MkvEngineOption = mkvEngineOption;
            MergeOption = mergeOption;
            SubsOption = subsOption;
            NoCleanupOption = noCleanupOption;
            AudioFormatOption = audioFormatOption;
            VideoFormatOption = videoFormatOption;
            AudioLangOption = audioLangOption;
        }

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

    //internal sealed class BatchDemuxOptions
    //{
    //    usmFolderArg,
    //            subsOption,
    //            mergeOption,
    //            mkvEngineOption,
    //            audioFormatOption,
    //            videoFormatOption,
    //            outputFolderOption,
    //            noCleanupOption,
    //            audioLangOption
    //}
}
