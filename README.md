# Euphonia
An easy way to store and listen to your music

![Logo](android/app/src/main/res/mipmap-xxxhdpi/ic_launcher.webp)

## Installing Euphonia
Download the backend and the frontend then follow these steps:

### Installing the backend

#### Docker
Move the content of backend in a folder

We then need to create a folder to contains our data there, for that you'll need to create a `data/` folder

Once you're done, run `docker compose up -d`

### Installing the frontend
Just grab everything in the frontend folder and throw it in your server

#### Nginx
```nginx
server {
	root /home/web/euphonia;

	index index.php;

	server_name euphonia.zirk.eu;

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

	location ~ /(vendor|node_module)/ {
		deny all;
	}

	# Path to the data folder of your backend
	location /data/ {
    	alias /home/backend/euphonia/data/;
    }

	# Fix size limit with file upload
	client_max_body_size 100M;
}
```

### Create admin password
Once this is done, open your website and go to /tools.php and enter your password, then write the value returned in your data folder, in credentials.json

## Updating Euphonia
With docker, update the files and run `docker compose build --no-cache` then run `docker compose up -d` again

## How to add songs

### From the website
Login as an admin using the hamburger menu at the top left of the website \
Once his is done, a new button will appear at the top left, click on it and fill the form with your music info

### From the extension (Chrome)
Download the chrome extension and go on `chrome://extensions/` \
Toggle `Developer mode` on the top right then click on `Load unpacked` and select the folder of the extension

You can then click on the extension popup, first enter your website (since my website is `https://euphonia.zirk.eu/` I will write `euphonia.zirk.eu`) \
Then enter your admin token

From that you can go on any YouTube song and fields should be prefilled with the page data

## Website customization

### Configuration
You can change the following fields in `metadata.json`:
- name: Name displayed on the website
- readme: Additional information shown inside the side panel
- showGithub: Show link to the GitHub in the side panel
- showDebug: Show additional debug information in the side panel
- allowDownload: Allow for songs to be downloaded
- allowShare: Allow for songs to be shared
- showAllPlaylist: If true, when going to playlist selection, will show a playlist called "All"

### Styling
You can also change the website CSS in `customize.cs`

## Contributors
- Icon: Stephano Naressi
- Spanish translation: Paulina Araneda
