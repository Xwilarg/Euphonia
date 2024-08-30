# Euphonia
An easy way to store and listen to your music

## Installing Euphonia
Download the backend and the frontend then follow these steps:

### Installing the backend
Install the backend somewhere on your server and keep it running, here is an example with systemctl:
```
[Unit]
Description=Euphonia backend
After=network-online.target

[Service]
ExecStart=dotnet [Path to backend]/Euphonia.API.dll
WorkingDirectory=[Path to backend]
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

### Installing the frontend
Just grab everything in the frontend folder and throw it in your web server

### Configure
Configure your webserver to have your website ready and the backend on /api/
Example:

#### Caddy
```
example.org {
	reverse_proxy /api/* localhost:5000

	root * base_folder_on_your_frontend
	php_fastcgi unix//run/php/php8.1-fpm.sock
	file_server {
		hide node_modules/
		hide vendor/
	}
	@blocked {
		path *.json
	}
	respond @blocked 403
}
```

## How to add songs

### Add songs
First you will need to download the following:
 - [yt-dlp](https://github.com/yt-dlp/yt-dlp) (YouTube only)
 - [ffmpeg](https://www.ffmpeg.org/) (YouTube only)
 - [ffmpeg-normalize](https://github.com/slhck/ffmpeg-normalize)

And place them in your path

You can then use the software inside downloader/ to either download from YouTube or import songs from a folder

## Listen to my music
[!TODO]: To write
Website, discord bot, android app

## Website customization
You can change some metadata of your website in `web/data/metadata.json` \
You can also change its style on `web/css/customize.cs`
