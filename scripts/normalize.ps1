$ErrorActionPreference = "Stop"

Get-ChildItem "../data/raw/" -Filter *.mp3 | 
Foreach-Object {
    ffmpeg-normalize "../data/raw/$($_)" -pr -ext mp3 -of ../data/normalized -c:a libmp3lame
}