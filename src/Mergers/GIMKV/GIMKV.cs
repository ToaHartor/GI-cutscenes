using System.Text;
using GICutscenes.Mergers.GIMKV.MKV.Elements;
using GICutscenes.Mergers.GIMKV.MKV.Elements.Attachment;
using GICutscenes.Mergers.GIMKV.MKV.Elements.Cluster;
using GICutscenes.Mergers.GIMKV.MKV.Elements.Cue;
using GICutscenes.Mergers.GIMKV.MKV.Elements.Segment;
using GICutscenes.Mergers.GIMKV.MKV.Elements.Tag;
using GICutscenes.Mergers.GIMKV.MKV.Elements.Tracks;
using GICutscenes.Mergers.GIMKV.MKV.Generics;
using GICutscenes.Mergers.GIMKV.Parsers;
using GICutscenes.FileTypes;

namespace GICutscenes.Mergers.GIMKV
{
    internal class GIMKV: Merger
    {
        // MKV utils
        public static readonly uint SegmentOffset = 0x1037;
        public static readonly uint TimestampScale = 1000000;
        public static readonly uint ClusterTimeLength = 3000; // In milliseconds
        public static readonly uint ClusterSize = 3000000; // In bytes
        public static readonly ulong[] unkLength = { 0x7F, 0x3FFF, 0x1FFFFF, 0xFFFFFFF, 0x7FFFFFFFF, 0x3FFFFFFFFFF, 0x1FFFFFFFFFFFF };


        private readonly Random _random;
        private List<string> _hexUIDs;
        private FileStream _memoryMkv;
        private ParserManager _parserManager;

        // Top elements
        private Header _header;

        private Segment _segment;
        private VoidElement _voidElement;
        private SegmentInfo _segmentInfo;
        private Tracks _tracks;
        private Attachment _attachments;
        private List<Cluster> _clusters;
        private Cues _cues;
        private Tags _tags;

        // Fields
        private readonly string _appName;

        private readonly DateTime _mergeTime;
        private readonly string _basename;

        public GIMKV(string basename, string outputPath, string appName, string videoPath)
        {
            // Utils declaration
            _parserManager = new ParserManager();
            _random = new Random();
            _hexUIDs = new List<string>();
            _mergeTime = DateTime.UtcNow;
            _basename = basename;
            _memoryMkv = new FileStream(Path.Combine(outputPath, _basename + ".mkv.out"), FileMode.Create);
            _appName = appName;

            // Header creation
            _header = new Header();

            // Creating tracks field
            _tracks = new Tracks();

            // Segment info creation
            byte[] segmentUid = new byte[0x10];
            _random.NextBytes(segmentUid);
            // Retrieving directly the duration from the first videoTrack
            AddVideoTrack(videoPath);
            _segmentInfo = new SegmentInfo(_appName, _parserManager.ParserList[0].Duration, _mergeTime.ToFileTimeUtc(), segmentUid);

            _attachments = new Attachment();

            // Cluster list
            _clusters = new List<Cluster>();
            CreateNewCluster(0); // Create the first cluster

            // Cues
            _cues = new Cues();

            // Tags
            _tags = new Tags();
        }

        // TODO: overload for stream integration
        public void AddVideoTrack(string videoFile)
        {
            if (!File.Exists(videoFile)) throw new FileNotFoundException($"Video file {videoFile} not found.");
            IVFParser video = new(Path.GetFullPath(videoFile));

            AddTrack("video", new string(video.IVFHeader.codec), Path.GetFileNameWithoutExtension(videoFile), lang: "und", fps: video.FPS, specSettings: new VideoSettings(video.IVFHeader.width, video.IVFHeader.height), codecPrivate: new byte[] { 0x01, 0x01, 0x00, 0x03, 0x01, 0x08, 0x04, 0x01, 0x01 });
            _parserManager.AddParser(video);
        }

        public void AddAudioTrack(string audioFile, int lang)
        {
            if (!File.Exists(audioFile)) throw new FileNotFoundException($"Audio file {audioFile} not found.");
            if (!Enumerable.Range(0, 4).Contains(lang)) throw new Exception($"Language number {lang} not supported");

            WAVParser audio = new(Path.GetFullPath(audioFile));
            AddTrack("audio", "WAVE", FileTypes.MKV.AudioLang[lang].Item1, lang: FileTypes.MKV.AudioLang[lang].Item2, fps: WAVParser.FPS, specSettings: new AudioSettings(audio.WavHeader.samplingRate, audio.WavHeader.channelCount, audio.WavHeader.bitCount));
            _parserManager.AddParser(audio);
        }

        public void AddSubtitlesTrack(string subFile, string giLanguage)
        {
            if (!FileTypes.MKV.SubsLang.ContainsKey(giLanguage)) throw new Exception($"Language code {giLanguage} isn't supported...");
            ASSParser subs = new(Path.GetFullPath(subFile));
            AddTrack("subtitle", "ASS", FileTypes.MKV.SubsLang[giLanguage].Item2, lang: FileTypes.MKV.SubsLang[giLanguage].Item1, codecPrivate: Encoding.UTF8.GetBytes(subs.Header));
            _parserManager.AddParser(subs);
        }

        private void AddTrack(string type, string codec, string name, string lang, uint? fps = null, byte[]? codecPrivate = null, MKVContainerElement? specSettings = null)
        {
            codec = codec switch
            {
                "VP90" => "V_VP9",
                "WAVE" => "A_PCM/INT/LIT",
                "ASS" => "S_TEXT/ASS",
                _ => throw new NotSupportedException($"Codec {codec} not supported...")
            };
            ulong? duration = TimestampScale * 1000 / fps;
            string langIETF = FileTypes.MKV.IsoToBcp47[lang];
            _tracks.Containers.Add(new TrackEntry(_tracks.Containers.Count + 1, GenerateUID(), type, codec, name, langIETF, duration, lang, codecPrivate, specSettings));
        }

        public void AddAttachment(string filePath, string description) =>
            _attachments.AddAttachment(filePath, description);

        public TrackEntry GetTrack(int index)
        {
            if (index < 0 || index >= _tracks.Containers.Count) throw new IndexOutOfRangeException($"Track index ({index}) exceeds the maximum index ({_tracks.Containers.Count})");
            return _tracks.Containers[index] as TrackEntry ?? throw new Exception($"The track at the index {index} is null");
        }

        // Returns bytes written
        private long WriteTracks()
        {
            _memoryMkv.Write(_tracks.ToBytes());
            return _memoryMkv.Position;
        }

        private long WriteSegmentInfo()
        {
            _memoryMkv.Seek(SegmentOffset, SeekOrigin.Begin);
            _memoryMkv.Write(_segmentInfo.ToBytes());
            return _memoryMkv.Position;
        }

        private long WriteCues()
        {
            _memoryMkv.Write(_cues.ToBytes());
            return _memoryMkv.Position;
        }

        private long WriteTags()
        {
            _memoryMkv.Write(_tags.ToBytes());
            return _memoryMkv.Position;
        }

        private void WriteAttachments() => _attachments.WriteBytes(_memoryMkv);
        /*
        public void WriteAllDataInStream()
        {
            long trackOffset = WriteSegmentInfo();
            long attachmentOffset = WriteTracks();
            // if attachment, write them here
            _memoryMkv.Write(new VoidElement(1591L).VoidBytes());  // Fixed size here

            if (_attachments.Count() == 0) attachmentOffset = 0; // No attachments then, set to zero for the Segment generation
            else WriteAttachments();
            int cueIndex = 0;

            for (int i = 0; i < _clusters.Count; i++) // 2.9s
            {
                for (int j = cueIndex; j < _cueWrappers.Count; j++)
                {
                    if (_cueWrappers[j].ClusterIndex == i)
                    {
                        _cues.Containers.Add(_cueWrappers[j].GenerateCuePoint(_memoryMkv.Position - 0x34));
                        cueIndex++;
                    }
                    else break;
                }
                _memoryMkv.Write(_clusters[i].ToBytes());
            }
            long cueOffset = _memoryMkv.Position;
            long tagOffset = WriteCues();
            GenerateTags();
            WriteTags();
            long fileSize = _memoryMkv.Position;

            // Writing header and the beginning of the file
            _memoryMkv.Position = 0;
            _memoryMkv.Write(_header.ToBytes());
            _segment = new Segment(_memoryMkv.Position, fileSize, trackOffset, cueOffset, tagOffset, attachmentOffset);
            _memoryMkv.Write(_segment.ToBytes());
            _voidElement = new VoidElement(SegmentOffset - _memoryMkv.Position);
            _memoryMkv.Write(_voidElement.VoidBytes());

        }*/

        public void Merge()
        {
            long trackOffset = WriteSegmentInfo();
            long attachmentOffset = WriteTracks();
            // if attachment, write them here
            _memoryMkv.Write(new VoidElement(1591L).VoidBytes());  // Fixed size here

            if (_attachments.Count() == 0) attachmentOffset = 0; // No attachments then, set to zero for the Segment generation
            else WriteAttachments();

            // Creating and writing clusters
            WriteClusters();

            long cueOffset = _memoryMkv.Position;
            long tagOffset = WriteCues();
            GenerateTags();
            WriteTags();
            long fileSize = _memoryMkv.Position;

            // Writing header and the beginning of the file
            _memoryMkv.Position = 0;
            _memoryMkv.Write(_header.ToBytes());
            _segment = new Segment(_memoryMkv.Position, fileSize, trackOffset, cueOffset, tagOffset, attachmentOffset);
            _memoryMkv.Write(_segment.ToBytes());
            _voidElement = new VoidElement(SegmentOffset - _memoryMkv.Position);
            _memoryMkv.Write(_voidElement.VoidBytes());

            Console.WriteLine("Finished writing MKV file");
            _memoryMkv.Close();

            // Free the files
            FreeStreams();

            string dest = _memoryMkv.Name[..^4];
            // Renaming file name to confirm the completion
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(_memoryMkv.Name, dest);
        }

        private void FreeStreams()
        {
            foreach (BaseParser parser in _parserManager.ParserList) parser.FreeParser();
        }
        public void CreateCue(int track, int timestamp, long clusterIndex, uint indexInCluster, int? duration = null) => _cues.Containers.Add(new CuePoint(timestamp, track, clusterIndex, indexInCluster, duration));

        public Cluster GetLatestCluster() => _clusters.Last();

        public Cluster CreateNewCluster(int timestamp)
        {
            Cluster newC = new(timestamp);
            _clusters.Add(newC);
            return newC;
        }

        private void GenerateTags()
        {
            TrackEntry track;
            // Parsers are sorted in the same order than tracks
            for (int i = 0; i < _parserManager.ParserList.Count; i++)
            {
                track = GetTrack(i);
                BaseParser currentParser = _parserManager.ParserList[i];
                ulong numberOfBytes = currentParser.TotalDataBytes;
                uint numberOfFrames = currentParser.FramesRead;
                ulong duration = currentParser.Duration;
                ulong bps = (ulong)Math.Floor(numberOfBytes * 8.0 / duration * 1000);

                _tags.Containers.Add(new Tag(track.GetTrackUID(), bps, duration, numberOfFrames, numberOfBytes, _appName, _mergeTime));
            }
        }

        public byte[] GenerateUID()
        {
            byte[] uid = new byte[0x08];
            _random.NextBytes(uid);
            // Generating unique ID
            while (_hexUIDs.Contains(Convert.ToHexString(uid))) _random.NextBytes(uid);
            return uid;
        }

        public static byte[] TrimZeroes(byte[] arr)
        {
            int lim = 0;
            foreach (byte b in arr) { if (b != 0) break; lim++; }
            return lim == arr.Length ? new byte[] { 0x00 } : arr[lim..]; // In case we have only a zero
        }

        public static byte[] FieldLength(uint length)
        {
            // Avoiding lengths to take a value corresponding to an unknown field
            bool unkContains = false;
            if (unkLength.Contains(length))
            {
                length += 1;
                unkContains = true;
            }
            // VarInt shenanigans
            uint len = length switch
            {
                < 0x80 => length + 0x80,
                < 0x4000 => length + 0x4000,
                < 0x200000 => length + 0x200000,
                < 0x10000000 => length + 0x10000000,
                _ => throw new Exception($"Number {length} is too big for the VarInt specification")
            };
            if (unkContains) len -= 1;
            byte[] arr = BitConverter.GetBytes(len);
            Array.Reverse(arr);  // Field length is written in BE
            return TrimZeroes(arr);
        }
        
        private void AddSingleBlockToCluster(int timestamp, BaseParser p, ParserManager parsers, Cluster? curCluster = null)
        {
            curCluster ??= GetLatestCluster();
            switch (p)
            {
                case IVFParser parser:
                    {
                        // If it doesn't fit, the current parser is a newly created one
                        if (!curCluster.CanBeAdded((int)Math.Round(timestamp + parser.FrameDuration),
                                parser.PeekLength(), videoFrames: 1))
                        {
                            // NEW: Add writing the last cluster
                            _memoryMkv.Write(curCluster.ToBytes());
                            curCluster = CreateNewCluster(timestamp);
                        }

                        bool isAKeyframe = parser.IsKeyframe();
                        int trackNumber = parsers.GetParserIndex(parser) + 1;
                        uint blockPosInCluster = curCluster.AddSimpleBlock(trackNumber, timestamp, (byte)(isAKeyframe ? 0x80 : 0x00), parser.ReadBlock());
                        if (isAKeyframe)
                            CreateCue(trackNumber, timestamp, _memoryMkv.Position - 0x34, blockPosInCluster);

                        break;
                    }
                case WAVParser parser: // TODO : check, like for more than one parser the frames that can be in it (in this case, there is only one wav parser, otherwise we would have been in the other block, as all audio blocks are synced)
                    {
                        int frameNumber = GetAudioFrameNumber(curCluster, timestamp, parsers); // Gets how many frames we can add in a single block without exceeding the maximum conditions of a cluster

                        // If zero, we can't add a single frame of audio, so we have to create a new cluster
                        if (frameNumber == 0)
                        {
                            // NEW: Add writing the last cluster
                            _memoryMkv.Write(curCluster.ToBytes());

                            curCluster = CreateNewCluster(timestamp);
                            frameNumber = parser.PossibleFullFramesNumber(WAVParser.MaxFramesInSameBlock);// Checking again, and if it's zero again, then we reached the last block (which isn't full)
                        }
                        // TODO: there might be another way to get the indexes
                        int trackNumber = parsers.GetParserIndex(parser) + 1;  // Flags = 0x00 if in BlockGroup, 0x80 in singleblock if one frame, else 0x84

                        if (frameNumber > 0)
                        {
                            curCluster.AddSimpleBlock(trackNumber, timestamp,
                                frameNumber != 1 ? (byte)0x84 : (byte)0x80, parser.ReadMultipleBlocks(frameNumber), frameNumber == 1 ? null : (byte)frameNumber);
                        }
                        else
                        {
                            byte[] data = parser.ReadBlock();
                            int duration = (int)Math.Round((float)data.Length / parser.FrameSize * parser.FrameDuration);
                            curCluster.AddBlockGroup(trackNumber, timestamp, data, duration);
                        }

                        //if (!curCluster.CanBeAdded((int)Math.Round(timestamp + parser.FrameDuration),
                        //        parser.FrameSize * (uint)WAVParser.MaxFramesInSameBlock, audioFrames: WAVParser.MaxFramesInSameBlock))
                        //    curCluster = mkv.CreateNewCluster(timestamp);
                        //bool isAKeyframe = parser.IsKeyframe();    // Never
                        //curCluster.AddSimpleBlock(trackNumber, timestamp, 0x84, parser.ReadBlock(), (byte)+WAVParser.MaxFramesInSameBlock); // Fill with frames number
                        break;
                    }
                case ASSParser parser:
                    {
                        if (!curCluster.CanBeAdded(timestamp, parser.GetDialogueLength(), subsFrames: 1))
                        {
                            // NEW: Add writing the last cluster
                            _memoryMkv.Write(curCluster.ToBytes());
                            curCluster = CreateNewCluster(timestamp);
                        }

                        //bool isAKeyframe = parser.IsKeyframe();  // Always true though
                        int trackNumber = parsers.GetParserIndex(parser) + 1;
                        int endTimestamp = parser.PeekEndTimestamp();

                        uint blockPosInCluster = curCluster.AddBlockGroup(trackNumber, timestamp, parser.ReadBlock(), endTimestamp - timestamp);
                        //if (isAKeyframe)
                        //{
                        CreateCue(trackNumber, timestamp, _memoryMkv.Position - 0x34, blockPosInCluster, endTimestamp - timestamp);
                        //}
                        break;
                    }
                default:
                    throw new Exception("This parser type isn't supported...");
            }
        }

        // Returns the maximum audio frame number
        private int GetAudioFrameNumber(Cluster cluster, int timestamp, ParserManager parsers)
        {
            // Because the frame number of reference can decrease in the next function, we should just check the true frame number instead
            //WAVParser audioP = parsers.ParserList.Find(p => p.GetType() == typeof(WAVParser)) as WAVParser ?? throw new Exception("We tried to find an audio parser (and we verified that there was one before), but we couldn't........."); // Just get one of them
            // We always know that if there is one wav parser, it is located right after the video track (as there is ALWAYS a video track)
            WAVParser audioP = parsers.ParserList[1] as WAVParser ?? throw new Exception("It should've been a wav parser, please report this bug....");

            int frameNumber = audioP.PossibleFullFramesNumber(WAVParser.MaxFramesInSameBlock);
            float frameDuration = audioP.FrameDuration;

            int endTimestamp, videoFrames, audioFrames, subsFrames;
            uint totalLength;

            while (frameNumber > 0)
            {
                endTimestamp = timestamp + (int)Math.Round(frameNumber * frameDuration);// The timestamp before which all the potential frames have to be added in the same cluster
                totalLength = ComputeNextFramesTotalLength(parsers, endTimestamp, frameNumber, out videoFrames, out audioFrames, out subsFrames);
                if (cluster.CanBeAdded(endTimestamp, totalLength, videoFrames, audioFrames, subsFrames)) break;
                frameNumber--; // When it can't be added, we try again without
            }

            //uint totalLength = ComputeNextFramesTotalLength(parsers, endTimestamp, ref frameNumber, out int videoFrames, out int audioFrames, out int subsFrames);
            return frameNumber;
        }

        public void WriteClusters()
        {
            Dictionary<Type, List<BaseParser>> parserTypes = new();

            // Making clusters
            while (_parserManager.HasRemainingData()) // We verify that a next block can be read in any parser
            {
                //var p = parsers.GetParser();
                var p = _parserManager.GetNextParsers();  // Getting the list of parsers having the same timestamp
                int timestamp = p[0].PeekTimestamp();
                //int trackNumber = parsers.GetParserIndex(p) + 1;

                // Check how many parsers have the same next timestamp
                // If more than one : check if they all fit in the cluster
                // If yes : Check how they can fit Insert all of them
                // If not : Are they only audios
                // If yes : try to reduce the frame number
                // // If not : Create a new cluster
                // else : Check if it fits in the cluster
                //      If yes, insert
                //      else, create a new one

                if (p.Count == 1)
                {
                    AddSingleBlockToCluster(timestamp, p[0], _parserManager);
                }
                else if (p.Count > 1) // More than one parser for the same timestamp
                {
                    Cluster curCluster = GetLatestCluster();
                    foreach (var par in p) // Getting type numbers
                    {
                        if (parserTypes.ContainsKey(par.GetType()))
                            parserTypes[par.GetType()].Add(par);
                        else
                            parserTypes.TryAdd(par.GetType(), new List<BaseParser> { par });
                    }

                    if (parserTypes.ContainsKey(typeof(WAVParser)))  // If there are wavparsers, all tracks there are synced
                    {
                        //// If no audio frame can fit, we create another cluster and we add all the audio frames, then we let the loop iterate for the others parsers
                        //// because we already confirmed that everything would fit
                        int frameNumber = GetAudioFrameNumber(curCluster, timestamp, _parserManager); // Gets how many frames we can add in a single block without exceeding the maximum conditions of a cluster

                        // If zero, we can't add a single frame of audio, so we have to create a new cluster
                        if (frameNumber == 0)
                        {
                            // NEW: Add writing the last cluster
                            _memoryMkv.Write(curCluster.ToBytes());
                            curCluster = CreateNewCluster(timestamp);
                            frameNumber = (parserTypes[typeof(WAVParser)][0] as WAVParser).PossibleFullFramesNumber(WAVParser.MaxFramesInSameBlock);// Checking again, and if it's zero again, then we reached the last block (which isn't full)
                        }
                        // TODO: If there is a video frame, insert it before the audio tracks
                        if (parserTypes.ContainsKey(typeof(IVFParser))) AddSingleBlockToCluster(timestamp, parserTypes[typeof(IVFParser)][0], _parserManager, curCluster);

                        // TODO: there might be another way to get the indexes
                        foreach (WAVParser parser in parserTypes[typeof(WAVParser)])
                        {
                            int trackNumber = _parserManager.GetParserIndex(parser) + 1;  // Flags = 0x00 if in BlockGroup, 0x80 in singleblock if one frame, else 0x84
                            if (frameNumber > 0)
                            {
                                curCluster.AddSimpleBlock(trackNumber, timestamp,
                                    frameNumber != 1 ? (byte)0x84 : (byte)0x80, parser.ReadMultipleBlocks(frameNumber), frameNumber == 1 ? null : (byte)frameNumber);
                            }
                            else
                            {
                                byte[] data = parser.ReadBlock();
                                int duration = (int)Math.Round((float)data.Length / parser.FrameSize * parser.FrameDuration);
                                curCluster.AddBlockGroup(trackNumber, timestamp, data, duration);
                            }
                        }
                    }
                    else // Usual behavior, we check if all parsers fit
                    {
                        uint totalLength = 0;
                        int videoFrames = 0;
                        int subsFrames = 0;
                        foreach (var par in p)
                        {
                            switch (par)
                            {
                                case IVFParser vid:
                                    totalLength += vid.PeekLength();
                                    videoFrames++;
                                    break;

                                case ASSParser sub:
                                    totalLength += sub.PeekLength();
                                    subsFrames++;
                                    break;

                                default:
                                    throw new Exception(
                                            "Reminder that we shouldn't have any WAVParser in this block");
                            }
                        }
                        // Manage new cluster creation
                        if (!curCluster.CanBeAdded(timestamp, totalLength, videoFrames: videoFrames,
                                subsFrames: subsFrames))
                        {
                            // NEW: Add writing the last cluster
                            _memoryMkv.Write(curCluster.ToBytes());
                            curCluster = CreateNewCluster(timestamp);
                        }

                        // iterate over the parser list of the same timestamp and add all of them to the cluster
                        // function with the content of the single attribution, and call it here
                        foreach (var par in p) AddSingleBlockToCluster(timestamp, par, _parserManager, curCluster);
                    }
                }
                else
                    throw new Exception(
                        "This exception should never be risen, the loop might have some sort of disfunction...");

                parserTypes.Clear();
            }
        }

        public uint ComputeNextFramesTotalLength(ParserManager parsers, int endTimestamp, int audiotrackFrames, out int videoFrames, out int audioFrames, out int subsFrames)
        {
            uint totalLength = 0;
            videoFrames = 0; audioFrames = 0; subsFrames = 0;
            foreach (var parser in parsers.ParserList)
            {
                switch (parser)
                {
                    case IVFParser p:
                        {
                            totalLength += p.DataSizeUntilTimestamp(endTimestamp, out int newFrames);
                            videoFrames += newFrames;
                            break;
                        }
                    case WAVParser p:
                        {
                            totalLength += p.NextFullAudioFramesLength(audiotrackFrames);
                            audioFrames += audiotrackFrames;
                            break;
                        }
                    case ASSParser p:
                        {
                            totalLength += p.DataSizeUntilTimestamp(endTimestamp, out int newFrames);
                            subsFrames += newFrames;
                            break;
                        }
                    default:
                        throw new Exception($"Parser type ${parser.GetType().Name} isn't supported...");
                }
            }
            return totalLength;
            // Iterate through all parsers and get bytes until timestamp
        }
    }
}