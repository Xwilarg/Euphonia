# Euphonia
An easy way to store and listen to your music

## How to use this project
- Clone the repository
- Inside the folder run `npm i` and `npm run build` then move its content to your web server
- Add your songs

## How to add songs

### YouTube
First you will need to download the following:
 - [yt-dlp](https://github.com/yt-dlp/yt-dlp)
 - [ffmpeg-normalize](https://github.com/slhck/ffmpeg-normalize)
and place them in your path

You can then use the software inside downloader/

### Import songs
You'll need to download the following:
- [ffmpeg-normalize](https://github.com/slhck/ffmpeg-normalize)
You can then run the following scripts:
- import.py: Import all your songs given a path
- normalize.sh/ps1: Normalize all your audio to make sure they are at the same volume
- album.py: Automatically download album images from last.fm (requires an API key)

## Listen to my music
[!TODO]: To write
Website, discord bot, android app

## Some of my songs are missing/are corrupted!
If you're missing some of your songs, you can run `integrity.py` who is located inside the scripts/ folder

## Website customization
You can change the colors of the website in web/css/customize.cs
