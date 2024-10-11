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

Then you will need to download the following and place them in your path:
 - [yt-dlp](https://github.com/yt-dlp/yt-dlp) (YouTube only)
 - [ffmpeg](https://www.ffmpeg.org/) (YouTube only)
 - [ffmpeg-normalize](https://github.com/slhck/ffmpeg-normalize)

Once this is done, send a POST request to [your website]/api/auth/hash with as body your password as JSON string, the API will return you your hashed password, write it inside data/credentials.json

## How to add songs

### Add songs
Login as an admin using the hamburger menu at the top left of the website \
Once his is done, a new button will appear at the top left, click on it and fill the form with your music info

## Listen to my music
[!TODO]: To write

## Website customization
You can change some metadata of your website in `web/data/metadata.json` \
You can also change its style on `web/css/customize.cs`
