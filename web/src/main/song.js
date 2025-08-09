// module "song.js"

import { archiveSong, repairSong, updateSong } from "../common/api";
import { getSongKey, prepareShuffle, updateSingleSongDisplay } from "./getMusics";
import { isMinimalistMode } from "./settings";

let isRepairOngoing;

function cleanPath(name) {
    const arr = ['<', '>', ':', '\\', '/', '"', '|', '?', '*', '#', '&', '%'];
    for (let a of arr) {
        name = name.replace(a, "");
    }
    return name;
}

export function spawnSongNode(json, curr, id) {
    let template = document.getElementById("template-song");
    const node = template.content.cloneNode(true);

    let idContainer = `${curr.id}-${id}`;
    node.querySelector(".song").id = idContainer;
    // If we are in minimalist mode, we allow click anywhere on the song since only the text part is displayed
    node.querySelector(".song-img").onclick = () => {
        if (!isMinimalistMode()) {
            prepareShuffle(curr.id);
        }
    };
    // If we are not, we only allow click on the image so user can copy text without starting song
    node.querySelector(".song").onclick = () => {
        if (isMinimalistMode()) {
            prepareShuffle(curr.id);
        }
    }
    let target = document.getElementById("edit-content");
    let form = target.querySelector("form");

    node.querySelector(".song-edit").addEventListener("click", () => {
        form.dataset.curr = getSongKey(curr);
        if (!target.classList.contains("is-hidden"))
        {
            target.classList.add("is-hidden");
            return;
        }
        target.classList.remove("is-hidden");
        form.reset();

        for (var i = 0, len = form.elements.length; i < len; ++i) {
            form.elements[i].disabled = false;
        }

        if (json.playlists) {
            const editPlaylist = document.getElementById("edit-playlist");
            for (let [key, value] of Object.entries(json.playlists)) {
                editPlaylist.innerHTML += `<option value="${key}"${curr.playlists.includes(key) ? " selected" : ""}>${value.name}</option>`;
            }
        }
        document.getElementById("edit-source").value = curr.source;
        document.getElementById("edit-name").value = curr.name;
        document.getElementById("edit-artist").value = curr.artist;
        if (curr.album) {
            document.getElementById("edit-album-name").value = json.albums[curr.album].name ?? curr.album;
            document.getElementById("edit-album-url").value = json.albums[curr.album].source;
        }
    });
    node.querySelector(".song-repair").addEventListener("click", () => {
        if (isRepairOngoing) {
            alert("A repair is already ongoing")
        } else {
            let res = confirm("Are you sure you want to repair this song? This will redownload the whole audio file");
            if (res) {
                isRepairOngoing = true;
                repairSong(getSongKey(curr), () => {
                    isRepairOngoing = false;
                }, () => {
                    isRepairOngoing = false;
                });
            }
        }
    });
    const editPlaylist = document.getElementById("edit-playlist");
    document.getElementById("song-playlist-reset").addEventListener("click", (e) => {
        e.preventDefault();
        editPlaylist.selectedIndex = -1;
    });
    node.querySelector(".song-archive").addEventListener("click", () => {
        // TODO: Dupplicated code from getMusics.js
        let res = confirm("Are you sure you want to archive this song?");
        if (res) {
            archiveSong(getSongKey(curr), () => {
                curr.isArchived = true;
                json.musics = json.musics.filter(x => !x.isArchived);
                alert("The song was archived");
            }, () => { });
        }
    });

    updateSingleSongDisplay(node, curr);
    document.getElementById(id).appendChild(node);
    updateSingleSongDisplay(document.getElementById(idContainer), curr);
}