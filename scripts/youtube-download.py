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

id = str(uuid4())

if not os.path.isdir("tmp"):
    os.mkdir("tmp")

subprocess.run(["youtube-dl", url, "-o", "tmp/" + id], stderr=sys.stderr, stdout=sys.stdout)
newName = [x for x in glob.glob("tmp/" + id + "*") if is_audio(x)][0]
subprocess.run(["ffmpeg", "-i", newName, "../data/" + name + ".wav"], capture_output=True)
os.remove(newName)

data["musics"].append({
    "name": name,
    "path": path + ".wav",
    "artist": artist,
    "album": album,
    "youtube": url
})

if album not in data["albums"]:
    print("Missing album data, you'll need to add the image manually")
    data["albums"][album] = {
        "path": "TODO"
    }

with open('../data/info.json', 'w', encoding='utf-8') as fd:
    json.dump(data, fd, ensure_ascii=False)

print("Done!")