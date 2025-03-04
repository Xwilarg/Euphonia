// module "playlist.js"

import { getAlbumImage, sanitize } from "./getMusics";

export function spawnPlaylistNode(id, name, json, nodeId) {
    
    let template = document.getElementById("template-playlist");
    const node = template.content.cloneNode(true);
    node.querySelector(".playlist").id = id;

    // Set click callback
    // For some wizardry just assigning onclick doesn't work
    node.querySelector(".card-image").setAttribute("onclick", `window.location=window.location.origin + window.location.pathname + '?playlist=${id}';`)

    // Prepare images to display inside
    let mostPresents = {};
    let count = 0;
    for (let elem of json.musics) {
        if (!elem.playlists.includes(id) && id !== "all") { // We filter by playlist (except if current playlist is "all")
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

    node.querySelector(".playlist-archive").addEventListener("click", (e) => {
        e.preventDefault();
        console.log("aaaaa");
    });
    
    document.getElementById(nodeId).appendChild(node);
}