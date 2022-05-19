# GI-cutscenes
A C# tool able to demux .usm files, decrypt video and audio tracks and convert to .wav .hca files, from the anime-like game GI. 
In its final form, it will be able to transform the .usm files from the game into a .mkv container file.

## Motivations

I made a python tool a year ago (as a proof of concept) in order to be able to rewatch any cutscenes from the game. As a first C# program, I thought it would be a good idea to rewrite it. I haven't invented the code for HCA extraction and USM demuxing, I just readapted it in C# from other projects.

## Features (and roadmap)

- [x] Extract video and audio (and video decryption)
- [x] Audio (hca) decryption
- [x] HCA conversion to WAV (a more commonly readable format)
- [ ] Subtitles from [Dim's repository](https://github.com/Dimbreath/GenshinData/tree/master/Subtitle), reformatted from `.srt` to `.ass` and font included
- [ ] MKV merging with video, audio, subtitles and fonts (using mkvmerge)


## Command line usage

There are 3 different commands available :
- `demuxUsm` to demux a specific `.usm` file, extracting audio and video and convert extracted `.hca` files into `.wav`
- `batchDemux` to demux all `.usm` files into a specific folder
- `convertHca` to convert a `.hca` file into `.wav`

#### Examples
- `GICutscenes.exe -h` displays the help menu
- `GICutscenes.exe batchDemux "[Game directory]\Genshin Impact game\GenshinImpact_Data\StreamingAssets\VideoAssets\StandaloneWindows64" output/` will extract every `.usm` file into the `output` directory
- `GICutscenes.exe demuxUsm hello.usm -b 00112233 -a 44556677` decrypts the file `hello.usm` with `key1=00112233` and `key2=44556677`
- `GICutscenes.exe convertHca hello_0.hca` decodes the file and converts it into a `.wav` file

The video is extracted as an `.ivf` file (which makes codec detection (VP9) easier for mkvmerge). In order to watch it, you can open it into VLC or change the extension to `.m2v`.
