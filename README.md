# GI-cutscenes

A command line program playing with the cutscenes files (USM) from Genshin Impact.

Able to extract the USM files, decrypt the tracks and convert them into readable formats, then merge them into a single MKV file.
The final MKV file can then be played like a small movie, with the subtitles correctly formatted like in the game.
Sometimes, subtitles can be desynchronized with the audio, but that's also the case in game (and not this program's fault).

#### Cutscenes from version 1.0 to 6.0 can be decrypted.
*Also includes CBT3, which has the same files than the live version*

If you want to extract newer cutscenes but the `versions.json` in the released zip is outdated, simply download the updated file in the project tree ([here](https://raw.githubusercontent.com/ToaHartor/GI-cutscenes/main/versions.json)) and replace the file.
This file will be updated with the version key every time a new version drops.

If some keys are not available yet, please check the pull requests to see if someone has already submitted them.

### Feel free to make a pull request if you have some keys unavailable in the versions file, any help is welcome on that part.

## Motivations

I made a python tool a year ago (as a proof of concept) in order to be able to rewatch any cutscenes from the game. 
As a first C# program, I thought it would be a good idea to rewrite it. I haven't invented the code for HCA conversion and USM demuxing, I just readapted it in C# from other projects.

## Features (and roadmap)

- [x] Extract video and audio (and video decryption)
- [x] Audio (hca) decryption
- [x] HCA conversion to WAV (a more commonly readable format)
- [x] Subtitles support from the gamedata, reformatted from `.srt` to `.ass` and font included in the default style (repositories available in Issues)
- [x] MKV merging with video, audio, subtitles and fonts without any additional software (but also supports the use of mkvmerge and FFMPEG)
- [x] Multithreaded audio decoding

## Build

This program uses the .NET framework version 6.0, so you will need the .NET SDK.
You can open this project in Visual Studio 2022 and build the solution, or use the dotnet CLI : `dotnet publish -c Release -r [platform]`.
Otherwise, you can also modify the script `build-all.sh` with the desired runtimes.

## Usage

You can follow the next steps to use this program :

### 1. Download

Grab the latest release for your platform from the [release page](https://github.com/ToaHartor/GI-cutscenes/releases/latest), download the ZIP file and extract it.
For each platform (Windows, Linux, MacOS), two binaries are available: 
- a standalone build (self-contained executable) which can be run without dotnet installed
- a framework dependant build (if you already have dotnet installed on your machine), much lighter but requires the dotnet runtime

You can also get a GUI version in this [repository](https://github.com/SuperZombi/GICutscenesUI) (thanks to [SuperZombi](https://github.com/SuperZombi))


Starting from version **0.4.0**, an merging solution was integrated without relying on external programs.
However, if you wish to use other merging solutions than the one integrated, you can install MKVToolNix (which provides mkvmerge) or FFMPEG.


### 2. Configuration

`appsettings.json` contains a configuration sample with the following keys :
- "MkvMergePath" : The path where mkvmerge is installed. Leave it empty if you installed mkvtoolnix (the package/program providing mkvmerge) in the default path. However, change it to the path of the mkvmerge file in case you're using a different installation path or you're using the portable MKVToolNix version.
- "FfmpegPath" : The path to the ffmpeg binary. Leave it empty if the binary is in the PATH of your operating system.
- "SubsFolder" : The path of the folder containing the subtitles of the video divided into language folders. Default is "./GenshinData/Subtitle", the right folder if you copy [this repository](https://gitlab.com/Dimbreath/AnimeGameData) in the same folder than the tool. You can follow the next section to clone the repository with the right path.
- "SubsStyle" : The style of the subtitles, according to the SubStation Alpha file format. If you need to modify the size, color or position, you can modify the parameters of it.

#### Clone the subtitles repository

Execute the following lines in the directory where the tool is :

```
git clone --depth 1 --filter=blob:none --sparse [repository URL]
cd GenshinData
git sparse-checkout set Subtitle
```

If you don't have git installed, you can download the zip file of the repository and unzip it in the program folder.

#### Managing the font files

No font file is provided in this repository, so you will have to get them in the game files.
You should find them in `[Game Directory]\Genshin Impact game\GenshinImpact_Data\StreamingAssets\MiHoYoSDKRes\HttpServerResources\font`.
You can then copy these two TTF files without renaming them in the tool's directory.

### 3. Commands

There are 3 different commands available to be used on the files :
- `demuxUsm` to demux a specific USM file, extracting audio and video and convert extracted HCA files into WAV
- `batchDemux` to demux all USM files into a specific folder
- `convertHca` to convert a HCA file into WAV

Several options are available for most of the commands :
- `--output` allows to choose the output folder
- `--merge` adds a merging step, putting the video, the audio (and the subtitles if the `--subs` option is also there) into a single MKV file. Subtitles will be automatically converted to the SSA format and stored in the `Subs` folder in the output directory if the `--no-cleanup` option is entered.
- `--no-cleanup` disables the suppression of the extracted files after merging
- `--mkv-engine` specifies the merging program used (either `internal`, `mkvmerge` or `ffmpeg`, using the internal method by default)
- `--audio-format` and `--video-format` can be used to select codecs. If at least one option is chosen, **the merging engine is changed to FFMPEG**.
- `--audio-lang` allow to specify audio track language in the output, allowed values are `[chi,eng,jpn,kor]`

Maintenance commands and options:
- `update` retrieves the latest `versions.json` file from the repository and checks if a new version has to be downloaded. It can take several optional parameters as follows :
	- `--no-browser` to not automatically open the browser if a new version is available
	- `--proxy <uri>` to specify a web proxy for the requests
- `reset` resets the configuration file (`appsettings.json`) to its default state
- `--stack-trace` enable the stack trace print of errors in the terminal

### Examples
- `GICutscenes -h` displays the help menu
- `GICutscenes batchDemux "[Game directory]\Genshin Impact game\GenshinImpact_Data\StreamingAssets\VideoAssets\StandaloneWindows64" --output "./output" --merge --subs --no-cleanup` will extract every USM file into the `output` directory, merge them with subtitles in a MKV file and will not cleanup the extracted files
- `GICutscenes batchDemux cutscenes/ -o ./output -m -s -e ffmpeg` will extract every USM file into the output directory, merging the files (`-m`) and the subs (`-s`) using FFMPEG (`-e`).
- `GICutscenes demuxUsm hello.usm -b 00112233 -a 44556677` decrypts the file `hello.usm` with `key1=00112233` and `key2=44556677` and extracts the tracks.
- `GICutscenes convertHca hello_0.hca` decodes the file and converts it into a WAV file
- `GICutscenes demuxUsm "[Path to .usm file]" --merge --subs --audio-lang "jpn,eng"` convert single USM file, include subtitles, include only JPN and ENG audio tracks

The video is extracted as an IVF file (which makes codec detection (VP9) easier for mkvmerge). In order to watch it, you can open it into VLC or change the extension to `.m2v`.
