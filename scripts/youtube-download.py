import json
import os
import subprocess
import sys
import requests
from uuid import uuid4
from PIL import Image
from io import BytesIO

def is_audio(name):
    return name.endswith(".wav") or name.endswith(".mp3") or name.endswith("webm") or name.endswith(".mp4") or name.endswith(".mkv")

def clean(text):
    return text.replace("<", "").replace(">", "").replace(":", "").replace("\"", "").replace("/", "").replace("\\", "").replace("|", "").replace("?", "").replace("*", "").strip()

if not os.path.exists('../web/data'):
    os.makedirs('../web/data')
if not os.path.exists('../web/data/raw'):
    os.makedirs('../web/data/raw')
if not os.path.exists('../web/data/icon'):
    os.makedirs('../web/data/icon')

data = {}
if os.path.exists('../web/data/info.json'):
    with open('../web/data/info.json', 'r', encoding='utf-8') as fd:
        data = json.load(fd)
else:
    data["musics"] = []
    data["albums"] = {}

name = input("Enter the song name: ")
name = name

url = input("Enter the YouTube URL: ")
if any(x["source"] == url for x in data["musics"]):
    print("There is already a song with the same URL")
    exit(1)

artist = input("Enter the artist name: ")
album = input("Enter the album name or None: ")
if album == "None":
    album = None

cover = None
if album is not None and album not in data["albums"]:
    coverUrl = input("Enter the album cover url: ")
    cover = requests.get(coverUrl, stream=True).content
    img = Image.open(BytesIO(cover))
    rgbImg = img.convert('RGB')
    album = clean(album)
    rgbImg.save('../web/data/icon/' + album + '.jpg')

playlist = "default"
if "playlists" in data:
    print("Choose a playlist or None:")
    i = 0
    for value in data["playlists"].values():
        print(str(i) + ": " + value["name"])
        i += 1
    playlist = input()
    if playlist.lower() == "none":
        playlist = "default"
    else:
        playlist = list(data["playlists"].keys())[int(playlist)]

path = name
path = clean(path)
songType = input("Enter song type (cover, acoustic...) or None: ")
if songType.lower() == "none":
    songType = None
else:
    path = name + " " + songType + " by " + artist

if any((("type" in x and x["type"] == songType) or ("type" not in x and songType == None)) and x["name"] == name for x in data["musics"]):
    print("There is always a song with that name")

id = str(uuid4())

if not os.path.isdir("tmp"):
    os.mkdir("tmp")

subprocess.run(["yt-dlp", url, "-o", "../web/data/raw/" + path + ".%(ext)s", "-x", "--audio-format", "mp3"], stderr=sys.stderr, stdout=sys.stdout)

data["musics"].append({
    "name": name,
    "path": path + ".mp3",
    "artist": artist,
    "album": album,
    "playlist": playlist,
    "source": url,
    "type": songType
})

if album is not None and album not in data["albums"]:
    data["albums"][album] = {
        "path": album + ".jpg"
    }

with open('../web/data/info.json', 'w', encoding='utf-8') as fd:
    json.dump(data, fd, ensure_ascii=False)

print("Done!")
