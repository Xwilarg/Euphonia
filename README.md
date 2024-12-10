# Euphonia
An easy way to store and listen to your music

## Installing Euphonia
Download the backend and the frontend then follow these steps:

### Installing the frontend
Just grab everything in the frontend folder and throw it in your server

Configure your webserver to have your website ready and the backend on /api/
Example (replace "/home/web/example" and "example.org"):

#### Nginx
```
server {
	root /home/web/example;

	index index.php;

	server_name example.org;

	location / {
		try_files $uri $uri/ =404;
	}
	location /api/ {
        	proxy_pass http://localhost:5000;
	}

	location ~ \.php$ {
		include snippets/fastcgi-php.conf;
		fastcgi_pass unix:/run/php/php8.3-fpm.sock;
	}

	location ~ (/(vendor|node_module)/|\.json$) {
		deny all;
	}
}
```

If you use docker you need to add the following


### Installing the backend

#### Docker
Move the content of backend in a folder

You'll then need to create a link between the data folder in your web folder, to the folder that will contains data in your backend (default is backend/data/website)
(Example with Nginx)
```
location /data/ {
	alias /home/backend/example/data/example.org/;
	autoindex on;
}
```
Run `docker compose up -d`

#### Systemctl
Throw your backend somewhere and create a systemctl file to keep it running:
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

Then you will need to download the following and place them in your path:
 - [yt-dlp](https://github.com/yt-dlp/yt-dlp/wiki/Installation) (YouTube only)
 - [ffmpeg](https://www.ffmpeg.org/download.html) (YouTube only)
 - [ffmpeg-normalize](https://github.com/slhck/ffmpeg-normalize?tab=readme-ov-file#installation)

### Create admin password
Once this is done, go to [your website]/tools.php and enter your password, then write the value returned in data/credentials.json

## How to add songs

### Add songs
Login as an admin using the hamburger menu at the top left of the website \
Once his is done, a new button will appear at the top left, click on it and fill the form with your music info

## Website customization
You can change some metadata of your website in `web/data/metadata.json` \
You can also change its style on `web/css/customize.cs`
