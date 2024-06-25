import requests
import os
import json
from pathlib import Path
import urllib.parse

def clean(text):
    return text.replace("<", "").replace(">", "").replace(":", "").replace("\"", "").replace("/", "").replace("\\", "").replace("|", "").replace("?", "").replace("*", "").strip()

if not os.path.exists('../web/data/info.json'):
    print("info JSON not found")
    exit(1)

Path("../web/data/icon").mkdir(exist_ok=True)
data = {}
with open('../web/data/info.json', 'r', encoding='utf-8') as fd:
    data = json.load(fd)

key = input("Enter your last.fm API key: ")

for m in data["musics"]:
    if m["album"] is None:
        r = requests.get(f"https://ws.audioscrobbler.com/2.0/?method=track.getInfo&api_key={key}&artist={urllib.parse.quote_plus(m["artist"])}&track={urllib.parse.quote_plus(m["name"])}&format=json")
        resp = json.loads(r.text)

        if "error" in resp:
            print(f"{m["name"]}: {resp["message"]}")
        else:
            track = resp["track"]

            if "album" in track:
                album = track["album"]["title"]
                dTarget = track["album"]["image"][-1]["#text"]

                if dTarget == "":
                    print(f"{m["name"]}: album URL empty")
                    continue

                path = f"{clean(m["artist"])}_{clean(album)}"
                ext = dTarget.split('.')[-1]
                r = requests.get(dTarget)
                open(f"../web/data/icon/{path}.{ext}", 'wb+').write(r.content)

                m["album"] = album
                if album not in data["albums"]:
                    data["albums"][album] = { "path": f"{path}.{ext}" }
                print(f"{m["name"]}: OK")
            else:
                print(f"{m["name"]}: no album available")


with open('../web/data/info.json', 'w', encoding='utf-8') as fd:
    json.dump(data, fd, ensure_ascii=False)

print("Done!")