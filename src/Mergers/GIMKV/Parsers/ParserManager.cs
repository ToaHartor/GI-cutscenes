namespace GICutscenes.Mergers.GIMKV.Parsers
{
    internal class ParserManager
    {
        public readonly List<BaseParser> ParserList;
        private readonly List<BaseParser> _sameTimeParsers;

        public ParserManager()
        {
            ParserList = new List<BaseParser>();
            _sameTimeParsers = new List<BaseParser>();

        }

        public void AddParser(BaseParser parser) => ParserList.Add(parser);

        public bool HasRemainingData() => ParserList.Any(k => k.PeekTimestamp() != -1);

        public List<BaseParser> GetNextParsers()
        {
            _sameTimeParsers.Clear();
            int minTimestamp = -1;
            foreach (BaseParser parser in ParserList.OrderBy(p => p.PeekTimestamp()))
            {
                int curTimestamp = parser.PeekTimestamp();
                if (curTimestamp == -1) continue;
                // Getting minimum timestamp
                if (minTimestamp == -1) // first one
                {
                    minTimestamp = curTimestamp;
                    _sameTimeParsers.Add(parser);
                    continue;
                }

                if (curTimestamp == minTimestamp)  // The ones right after
                    _sameTimeParsers.Add(parser);
                else  // The first parser where the timestamp is different
                    break;
            }
            return _sameTimeParsers;
            //_parsers.Where(p => p.PeekTimestamp() != -1).MinBy(p => p.PeekTimestamp()) ?? throw new Exception("No parser has some remaining data left.");
        }

        public int GetParserIndex(BaseParser p) => ParserList.IndexOf(p);
    }
}