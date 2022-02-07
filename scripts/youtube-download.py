import json
import os
import subprocess
import sys
import glob
from uuid import uuid4

def is_audio(name):
    return name.endswith(".wav") or name.endswith(".mp3") or name.endswith("webm") or name.endswith(".mp4") or name.endswith(".mkv")

data = {}

with open('../data/info.json', 'r', encoding='utf-8') as fd:
    data = json.load(fd)

name = input("Enter the song name: ")
path = name
if any(x["name"] == name for x in data["musics"]):
    print("There is always a song with that name")
    exit(1)

url = input("Enter the YouTube URL: ")
if any(x["youtube"] == url for x in data["musics"]):
    print("There is already a song with the same URL")
    exit(1)

artist = input("Enter the artist name: ")
album = input("Enter the album name: ")
if album is "None":
    album = None

id = str(uuid4())

if not os.path.isdir("tmp"):
    os.mkdir("tmp")

subprocess.run(["youtube-dl", url, "-o", "../data/" + name + ".%(ext)s", "-x", "--audio-format", "wav"], stderr=sys.stderr, stdout=sys.stdout)

data["musics"].append({
    "name": name,
    "path": path + ".wav",
    "artist": artist,
    "album": album,
    "youtube": url
})

if album is not None and album not in data["albums"]:
    print("Missing album data, you'll need to add the image manually")
    data["albums"][album] = {
        "path": album + ".jpg"
    }

with open('../data/info.json', 'w', encoding='utf-8') as fd:
    json.dump(data, fd, ensure_ascii=False)

print("Done!")