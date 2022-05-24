# GI-cutscenes
A command line program playing with the cutscenes files (USM) from Genshin Impact.
It is able to demux USM files, decrypt video and audio tracks, convert HCA files to WAV, convert SRT subtitles into ASS and merge all these extracted files into a single MKV file.

## Motivations

I made a python tool a year ago (as a proof of concept) in order to be able to rewatch any cutscenes from the game. As a first C# program, I thought it would be a good idea to rewrite it. I haven't invented the code for HCA conversion and USM demuxing, I just readapted it in C# from other projects.

## Features (and roadmap)

- [x] Extract video and audio (and video decryption)
- [x] Audio (hca) decryption
- [x] HCA conversion to WAV (a more commonly readable format)
- [x] Subtitles from [Dim's repository](https://github.com/Dimbreath/GenshinData/tree/master/Subtitle), reformatted from `.srt` to `.ass` and font included
- [x] MKV merging with video, audio, subtitles and fonts (using mkvmerge)
- [ ] Attempt to multithreading (one thread per file) when batch extracting (USM and HCA)

## Configuration file

`appsettings.json` contains a configuration sample with the following keys :
- "MkvMergePath" : The path where mkvmerge is installed. Leave it empty if you installed mkvtoolnix (the package/program providing mkvmerge) in the default path. However, change it to the path of the mkvmerge file in case you're using a different installation path or you're using the portable MKVToolNix version.
- "SubsFolder" : The path of the folder containing the subtitles of the video divided into language folders. Default is "./GenshinData/Subtitle", the right folder if you copy [this repository](https://github.com/Dimbreath/GenshinData) in the same folder than the tool. You can follow the next section to clone the repository with the right path.

#### Clone the subtitles repository

Execute the following lines in the directory where the tool is :

```
git clone --depth 1 --filter=blob:none --sparse https://github.com/Dimbreath/GenshinData.git
cd GenshinData
git sparse-checkout set Subtitle
```

#### Managing the font files

No font file is provided in this repository, so you will have to get them in the game files.
You should find them in `[Game Directory]\Genshin Impact game\GenshinImpact_Data\StreamingAssets\MiHoYoSDKRes\HttpServerResources\font`.
You can then copy these two TTF files without renaming them in the tool's directory.

## Command line usage

There are 3 different commands available :
- `demuxUsm` to demux a specific USM file, extracting audio and video and convert extracted HCA files into WAV
- `batchDemux` to demux all USM files into a specific folder
- `convertHca` to convert a HCA file into WAV

Several options are available for most of the commands :
`--output` allows to choose the output folder
`--merge` adds a merging step, putting the video, the audio (and the subtitles if the `--subs` option is also there) into a single MKV file
`--no-cleanup` disables the suppression of the extracted files after merging

#### Examples
- `GICutscenes.exe -h` displays the help menu
- `GICutscenes.exe batchDemux "[Game directory]\Genshin Impact game\GenshinImpact_Data\StreamingAssets\VideoAssets\StandaloneWindows64" --output "./output" --merge --subs --no-cleanup ` will extract every USM file into the `output` directory, merge them with subtitles in a MKV file and will not cleanup the extracted files
- `GICutscenes.exe demuxUsm hello.usm -b 00112233 -a 44556677` decrypts the file `hello.usm` with `key1=00112233` and `key2=44556677`
- `GICutscenes.exe convertHca hello_0.hca` decodes the file and converts it into a WAV file

The video is extracted as an IVF file (which makes codec detection (VP9) easier for mkvmerge). In order to watch it, you can open it into VLC or change the extension to `.m2v`.
