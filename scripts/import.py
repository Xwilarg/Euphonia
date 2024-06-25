import json
import os
import re
from pathlib import Path
import shutil

Path("../web/data/raw").mkdir(exist_ok=True)
data = {}
if os.path.exists('../web/data/info.json'):
    with open('../web/data/info.json', 'r', encoding='utf-8') as fd:
        data = json.load(fd)
else:
    data = {
        "musics": [],
        "albums": {}
    }

path = input("Enter the folder containing the music you want to import: ")
if not os.path.exists(path):
    print("The given path doesn't exists")
    exit(1)

regex = input("Input filter REGEX: ")

nameGroup = input("Input name group: ")
artistGroup = input("Input artist group: ")

filenames = next(os.walk(path), (None, None, []))[2]
for f in filenames:
    name = None
    artist = None

    currPath = Path(f)
    r = re.search(regex, currPath.stem)
    if r:
        name = r.group(int(nameGroup)).strip()
    else:
        name = currPath.stem

    r = re.search(regex, currPath.stem)
    if r:
        artist = r.group(int(artistGroup)).strip()
    else:
        artist = currPath.stem

    curr = {
        "name": name,
        "path": currPath.stem + ".mp3",
        "artist": artist,
        "album": None,
        "playlist": "default",
        "source": "file",
        "type": None
    }
    data["musics"].append(curr)

    #shutil.copy(path + "/" + f, "../web/data/raw")
    print(f"{name} by {artist}")

with open('../web/data/info.json', 'w', encoding='utf-8') as fd:
    json.dump(data, fd, ensure_ascii=False)

print("Done!")