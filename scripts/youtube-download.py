import json
import os
import subprocess
import sys
from uuid import uuid4

def is_audio(name):
    return name.endswith(".wav") or name.endswith(".mp3") or name.endswith("webm") or name.endswith(".mp4") or name.endswith(".mkv")

if not os.path.exists('data'):
    os.makedirs('data')
if not os.path.exists('data/raw'):
    os.makedirs('data/raw')

data = {}

with open('../data/info.json', 'r', encoding='utf-8') as fd:
    data = json.load(fd)

name = input("Enter the song name: ")

url = input("Enter the YouTube URL: ")
if any(x["youtube"] == url for x in data["musics"]):
    print("There is already a song with the same URL")
    exit(1)

artist = input("Enter the artist name: ")
album = input("Enter the album name or None: ")
if album == "None":
    album = None

path = name
songType = input("Enter song type (cover, acoustic...) or None: ")
if songType == "None":
    songType = None
else:
    path = name + " " + songType + " by " + artist

if any((("type" in x and x["type"] == songType) or ("type" not in x and songType == None)) and x["name"] == name for x in data["musics"]):
    print("There is always a song with that name")

id = str(uuid4())

if not os.path.isdir("tmp"):
    os.mkdir("tmp")

subprocess.run(["youtube-dl", url, "-o", "../data/raw/" + path + ".%(ext)s", "-x", "--audio-format", "wav"], stderr=sys.stderr, stdout=sys.stdout)

data["musics"].append({
    "name": name,
    "path": path + ".wav",
    "artist": artist,
    "album": album,
    "youtube": url,
    "type": songType
})

if album is not None and album not in data["albums"]:
    print("Missing album data, you'll need to add the image manually")
    data["albums"][album] = {
        "path": album + ".jpg"
    }

with open('../data/info.json', 'w', encoding='utf-8') as fd:
    json.dump(data, fd, ensure_ascii=False)

print("Done!")