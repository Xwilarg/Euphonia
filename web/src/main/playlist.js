// module "playlist.js"

import { createPlaylist, isLoggedIn, removePlaylist } from "../common/api";
import { displayPlaylists, getAlbumImage, sanitize } from "./getMusics";

export function spawnPlaylistNode(id, name, json, nodeId, allowDeletion) {
    let template = document.getElementById("template-playlist");
    const node = template.content.cloneNode(true);

    node.querySelector(".playlist").id = `playlist-${id}`;

    // Set click callbacks
    node.querySelector(".card-image").onclick = () => {
        window.location=window.location.origin + window.location.pathname + `?playlist=${id}`;
    }

    node.querySelector(".playlist-archive").onclick = () => {
        if (json.musics.some(x => x.playlists.includes(id))) {
            if (confirm("This playlist contains songs, are you sure you want to delete it?"))
            {
                removePlaylist(id, () => { document.getElementById(`playlist-${id}`).remove(); }, () => {});
            }
        } else {
            removePlaylist(id, () => { document.getElementById(`playlist-${id}`).remove(); }, () => {});
        }
    }

    // Prepare images to display inside
    let mostPresents = {};
    let count = 0;
    for (let elem of json.musics) {
        if (((id === "default" && elem.playlists.length > 0) || (id !== "default" && !elem.playlists.includes(id))) && id !== "all") { // We filter by playlist (except if current playlist is "all")
            continue;
        }
        count++;
        if (elem.album === null) { // TODO: Should also check playlist path
            continue;
        }
        let img = getAlbumImage(elem);
        if (mostPresents[img] === undefined) {
            mostPresents[img] = 1;
        } else {
            mostPresents[img]++;
        }
    }
    let imgs = Object.keys(mostPresents).map(function(key) {
        return [key, mostPresents[key]];
    }).sort(function(a, b) {
        return b[1] - a[1];
    }).slice(0, 4);

    let htmlImgs = "";
    if (imgs.length === 0) {
        htmlImgs = `<img class="image" src="/img/CD.png"/ draggable="false">`;
    } else {
        for (let img of imgs) {
            htmlImgs += `<img class="image" src="${img[0]}"/ draggable="false">`;
        }
    }
    node.querySelector(".playlist-img-container").innerHTML = htmlImgs;

    node.querySelector(".card-content > p").innerHTML = `${sanitize(name)}<br>${count} songs`;

    if (!allowDeletion) {
        node.querySelector(".dropdown").remove();
    } else if (!isLoggedIn()) {
        node.querySelector(".dropdown").classList.add("is-hidden");
    }
    
    document.getElementById(nodeId).appendChild(node);
}

export function spawnNewPlaylistNode(nodeId, json) {
    let template = document.getElementById("template-playlist");
    const node = template.content.cloneNode(true);

    node.querySelector(".card-content > p").innerHTML = `Create new playlist`;
    node.querySelector(".card-image").onclick = () => { createNewPlaylist(nodeId, json); };
    node.querySelector(".card").classList.add("requires-admin");
    if (!isLoggedIn()) {
        node.querySelector(".card").classList.add("is-hidden");
    }

    node.querySelector(".dropdown").remove();

    document.getElementById(nodeId).appendChild(node);
}

function createNewPlaylist(nodeId, json)
{
    var playlistName = window.prompt("Enter new playlist name");
    if (playlistName) {
        document.getElementById(nodeId).innerHTML = "";
        createPlaylist(playlistName, () => {
            json.playlists[playlistName.toLowerCase()] = {
                name: playlistName,
                description: null
            };
            displayPlaylists(json.playlists, "playlistlist", "");
        }, () => {});
    }
}