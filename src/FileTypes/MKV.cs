namespace GICutscenes.FileTypes
{
    internal class MKV
    {
        public static readonly Dictionary<string, (string, string)> SubsLang = new()
        {
            {"CHS", ("chi-CN", "简体中文")},
            {"CHT", ("chi-TW", "繁體中文")},
            {"DE", ("ger", "Deutsch")},
            {"EN", ("eng", "English")},
            {"ES", ("spa", "Español")},
            {"FR", ("fre", "Français")},
            {"ID", ("ind", "Bahasa Indonesia")},
            {"JP", ("jpn", "日本語")},
            {"KR", ("kor", "한국어")},
            {"PT", ("por", "Português")},
            {"RU", ("rus", "Русский")},
            {"TH", ("tha", "ภาษาไทย")},
            {"VI", ("vie", "Tiếng Việt")}
        };

        public static readonly (string, string)[] AudioLang =
        {
            ("汉语", "chi"), // zh
            ("English", "eng"), // en
            ("日本語", "jpn"), // ja
            ("한국어", "kor")  // ko
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
            {"und", "und"},
            {"chi-CN", "zh"},
            {"chi-TW", "zh"}
        };
    }
}
