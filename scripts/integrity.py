import json
import subprocess
import os
import re

if not os.path.exists('../web/data/info.json'):
    print("info JSON not found")
    exit(1)

data = {}
with open('../web/data/info.json', 'r', encoding='utf-8') as fd:
    data = json.load(fd)

musicCount = len(data["musics"])
files = len(next(os.walk("../web/data/raw"))[2])

diff = musicCount - files
print(f'At least {diff} songs will be downloaded')

_ = input("Press enter to continue")

for m in data["musics"]:
    if not os.path.exists(f'../web/data/raw/{m["path"]}'): # Song doesn't exists
        if m["source"] is None or (not m["source"].startswith("https://youtu.be/") and not m["source"].startswith("https://youtube.com/") and not m["source"].startswith("https://www.youtube.com/")):
            print(f"Skipping {m["name"]}: no valid source")
            continue
        print(f"Downloading {m["name"]}")
        result = subprocess.run(["yt-dlp", m["source"], "-o", f"../web/data/raw/{m["path"]}", "-x", "--audio-format", "mp3"], capture_output = True, text = True)
        if result.returncode != 0:
            print(f"Error:\n{result.stderr}")
            exit(1)
    elif os.path.exists(f'../web/data/normalized/{m["path"]}'): # Song exists but we verify if it's correct
        regex = "size=N\\/A time=([0-9]+:[0-9]+:[0-9]+\\.[0-9]+) bitrate=N\\/A speed"

        stdout = subprocess.getoutput(f'ffmpeg -i "../web/data/normalized/{m["path"]}" -f null -')
        r = re.search(regex, stdout)
        oNorm = r.group(1)

        stdout = subprocess.getoutput(f'ffmpeg -i "../web/data/raw/{m["path"]}" -f null -')
        r = re.search(regex, stdout)
        oRaw = r.group(1)

        if oNorm == oRaw:
            print(f"{m["name"]} is valid")
        else:
            print(f"{m["name"]} must be normalized again")
            break

print("Operation completed!")