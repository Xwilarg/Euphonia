$ErrorActionPreference = "Stop"

Get-ChildItem "../web/data/raw/" -Filter *.mp3 |
Foreach-Object {
    ffmpeg-normalize "../web/data/raw/$($_)" -pr -ext mp3 -of ../web/data/normalized -c:a libmp3lame
}