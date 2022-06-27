namespace GICutscenes.FileTypes
{
    internal class MKV
    {
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
            ("Chinese", "chi"), // zh
            ("English", "eng"), // en
            ("Japanese", "jpn"), // ja
            ("Korean", "kor")  // ko
        };
        // Or Lang to IETF Lang
        public static readonly Dictionary<string, string> IsoToBcp47 = new()
        {
            {"chi", "zh"},
            {"ger", "de"},
            {"eng", "en"},
            {"spa", "es"},
            {"fre", "fr"},
            {"ind", "id"},
            {"jpn", "ja"},
            {"kor", "ko"},
            {"por", "pt"},
            {"rus", "ru"},
            {"tha", "th"},
            {"vie", "vi"},
            {"und", "und"}
        };
    }
}
