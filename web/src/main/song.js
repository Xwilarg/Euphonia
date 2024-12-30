// module "song.js"

import { repairSong, updateSong } from "../common/api";
import { getSongKey, prepareShuffle, updateSingleSongDisplay } from "./getMusics";

let isRepairOngoing;

function cleanPath(name) {
    const arr = ['<', '>', ':', '\\', '/', '"', '|', '?', '*', '#', '&', '%'];
    for (let a of arr) {
        name = name.replace(a, "");
    }
    return name;
}

function generateKey(artist, album) {
    return `${cleanPath(artist?.trim() ?? "unknown")}_${cleanPath(album.trim())}`;
}

export function spawnSongNode(json, curr, id, isMinimalist) {
    let template = document.getElementById("template-song");
    const node = template.content.cloneNode(true);

    let idContainer = `${curr.id}-${id}`;
    node.querySelector(".song").id = idContainer;
    // If we are in minimalist mode, we allow click anywhere on the song since only the text part is displayed
    node.querySelector(".song-img").onclick = () => {
        if (!isMinimalist) {
            prepareShuffle(curr.id);
        }
    };
    // If we are not, we only allow click on the image so user can copy text without starting song
    node.querySelector(".song").onclick = () => {
        if (isMinimalist) {
            prepareShuffle(curr.id);
        }
    }
    let target = node.querySelector(".edit-content");
    let form = node.querySelector("form");

    // We if edit album stuff, we need to take some stuff into consideration...
    let editName = node.querySelector(".edit-album-name");
    let editUrl = node.querySelector(".edit-album-url");
    let editKey = node.querySelector(".edit-album-key");
    let editWarn = node.querySelector(".edit-album-url-warning");
    let keyWarn = node.querySelector(".edit-album-key-warn");
    function onAlbumChange() {
        editWarn.classList.add("is-hidden");
        editUrl.classList.remove("is-warning");
        editWarn.innerHTML = "";

        let namePlaceholder = null;
        let haveChanges = false;

        // We warn users if unexpected things might happen due to name/URL changes
        if (curr.album) { // There was an album before
            const isSameName = editName.value === (json.albums[curr.album].name ?? curr.album);
            const isSameUrl = editUrl.value === (json.albums[curr.album].source ?? "");

            haveChanges = !isSameName || !isSameUrl;

            if (!isSameUrl && isSameName) {
                editWarn.classList.remove("is-hidden");
                editUrl.classList.add("is-warning");
                editWarn.innerHTML = "Only changing the URL can affect<br>others songs sharing the album name";
            } else if (editName.value === "" && editUrl.value !== "") {
                editWarn.classList.remove("is-hidden");
                editUrl.classList.add("is-warning");
                editWarn.innerHTML = "Changing the URL without adding<br>a name will assign a random one";
            }

            if (editName.value === "") { // User removed the name
                namePlaceholder = crypto.randomUUID();
            }
        }
        else
        {
            haveChanges = editName.value !== "" || editUrl.value !== "";

            if (editUrl.value !== "" && editName.value === "") { // Used added a URL but name is still empty
                namePlaceholder = crypto.randomUUID();
            }
        }
        if (editKey.value !== "" && editKey.value !== curr.album) haveChanges = true;

        editName.placeholder = namePlaceholder ?? "Name of the album";

        // Generate a key in case user didn't add one
        let key;
        if (editName.value === "" && editUrl.value === "") {
            key = "";
        } else if (!haveChanges && curr.album) {
            key = curr.album;
        } else {
            key = generateKey(curr.artist, editName.value === "" ? namePlaceholder : editName.value);
        }

        editKey.placeholder = key;

        const currKey = editKey.value === "" ? key : editKey.value;
        if (haveChanges && currKey in json.albums) {
            keyWarn.classList.remove("is-hidden");
            editKey.classList.add("is-warning");

            editName.disabled = true;
            editUrl.disabled = true;
        } else {
            keyWarn.classList.add("is-hidden");
            editKey.classList.remove("is-warning");

            editName.disabled = false;
            editUrl.disabled = false;
        }
    }

    editName.addEventListener("change", (_) => { onAlbumChange(); });
    editUrl.addEventListener("change", (_) => { onAlbumChange(); });
    editKey.addEventListener("change", (_) => { onAlbumChange(); });

    node.querySelector(".song-edit").addEventListener("click", () => {
        target.hidden = !target.hidden;
        if (!target.hidden) {
            form.reset();

            for (var i = 0, len = form.elements.length; i < len; ++i) {
                form.elements[i].disabled = false;
            }
            form.getElementsByClassName("edit-source")[0].value = curr.source;
            form.getElementsByClassName("edit-name")[0].value = curr.name;
            form.getElementsByClassName("edit-artist")[0].value = curr.artist;
            if (curr.album) {
                form.getElementsByClassName("edit-album-name")[0].value = json.albums[curr.album].name ?? curr.album;
                form.getElementsByClassName("edit-album-url")[0].value = json.albums[curr.album].source;
                form.getElementsByClassName("edit-album-key")[0].placeholder = curr.album;
            }
        }
    });
    node.querySelector(".song-repair").addEventListener("click", () => {
        if (isRepairOngoing) {
            alert("A repair is already ongoing")
        } else {
            if (elem.source === "localfile") {
                alert("This feature isn't available for local files");
            } else {
                let res = confirm("Are you sure you want to repair this song? This will redownload the whole audio file");
                if (res) {
                    isRepairOngoing = true;
                    repairSong(getSongKey(elem), () => {
                        isRepairOngoing = false;
                    }, () => {
                        isRepairOngoing = false;
                    });
                }
            }
        }
    })
    form.addEventListener("submit", (e) => {
        e.preventDefault();

        const data = new FormData(e.target);
        data.append("Key", getSongKey(curr));

        for (var i = 0, len = form.elements.length; i < len; ++i) {
            form.elements[i].disabled = true;
        }
        updateSong(data, () => {
            target.hidden = true;

            if (data.get("Tags") !== undefined) {
                curr.tags = data.get("Tags");
            }
            curr.name = data.get("Name");
            curr.artist = data.get("Artist");

            updateSingleSongDisplay(document.getElementById(idContainer), curr);
        }, () => {
            target.hidden = true;
        });
    });

    updateSingleSongDisplay(node, curr);
    document.getElementById(id).appendChild(node);
    updateSingleSongDisplay(document.getElementById(idContainer), curr);
}