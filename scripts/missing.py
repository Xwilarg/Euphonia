import json
import subprocess
import os

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
    if not os.path.exists(f'../web/data/raw/{m["path"]}'):
        if m["source"] is None or (not m["source"].startswith("https://youtu.be/") and not m["source"].startswith("https://youtube.com/")):
            print(f"Skipping {m["name"]}: no valid source")
            continue
        print(f"Downloading {m["name"]}")
        result = subprocess.run(["yt-dlp", m["source"], "-o", "../web/data/raw/" + m["name"] + ".%(ext)s", "-x", "--audio-format", "mp3"], capture_output = True, text = True)
        if result.returncode != 0:
            print(f"Error:\n{result.stderr}")
            exit(1)

print("Operation completed!")