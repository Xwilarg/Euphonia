// module "getMusics.js"

import * as wanakana from 'wanakana';
import { registerNowPlayingAsync, registerScrobbleAsync } from "./lastfm"
import { deleteCookie } from "./cookie"
import { archiveSong, getApiToken, isLoggedIn, logOff, repairSong, updateSong, validateIntegrity } from '../common/api';

let json;
let metadataJson;

// ID of the current playlist
let currentPlaylist = null;
// JSON data of the song being played
let currSong = null;
// Current time we listened to the song
let timeListened = 0;
// Every update of timeListened we store the last index in song duration
let lastTimeUpdate = 0;
// Actual song duration, updated when metadata are loaded
let trackDuration = 0;
let timeStarted;

let isRepairOngoing;

// Next songs to play
let playlist = [];
let playlistIndex;

let replayMode = 0;

// #region Music management

// Start playing if audio player is paused, else pause it
function togglePlay() {
    let player = document.getElementById("player");
    if (player.paused) {
        player.play();
    } else {
        player.pause();
    }
}

function previousSong() {
    // If we are more than 2 secondes into the song we just restart it
    if (player.currentTime >= 2.0) {
        player.currentTime = 0.0;
    } else if (playlistIndex > 1) {
        playlistIndex -= 2;
        nextSong();
    }
}

function updateSongHighlightColor() {
    if (playlistIndex === undefined) {
        return;
    }

    // Color the currently played song
    var elems = document.getElementsByClassName("current");
    while (elems.length > 0) {
        elems[0].classList.remove("current");
    }
    for (let c of document.getElementsByClassName("song")) {
        if (c.dataset.name == json.musics[playlist[playlistIndex]].name) {
            c.classList.add("current");
        }
    }
}

// Play the next song of the playlist
function nextSong() {
    updateScrobblerAsync();

    // Displayer player if not here
    document.getElementById("full-player").classList.remove("is-hidden");

    // Update playlist text
    let playlistSize = playlist.length - playlistIndex - 1;
    document.getElementById("playlist").classList.remove("is-hidden");
    document.getElementById("playlist-title").innerHTML =
    `<h3>${playlistSize} song${playlistSize > 1 ? 's' : ''} queued:</h3>`;
    let playlistElems = document.getElementsByClassName("next-song");
    for (let i = 0; i < playlistElems.length; i++) {
        if (playlistIndex + i + 1 >= playlist.length) {
            break;
        }
        const curr = json.musics[playlist[playlistIndex + i + 1]];
        playlistElems[i].innerHTML = sanitize(curr.name) + " by " + sanitize(curr.artist);
    }

    // Select current song and move playlist forward
    if (replayMode !== 2) { // We are not in "repeat current song" mode
        playlistIndex++;
        if (playlistIndex === playlist.length) { // Replay mode is in loop playlist mode
            if (replayMode === 1) {
                playlistIndex = 0;
            } else {
                player.pause();
            }
        }
    }

    let elem = json.musics[playlist[playlistIndex]];
    console.log(`[Song] Playing ${elem.name} by ${elem.artist} from ${elem.album}`);
    currSong = elem;
    timeListened = 0;
    lastTimeUpdate = 0;
    trackDuration = 0;
    timeStarted = Math.floor(Date.now() / 1000);

    // Load song and play it
    let player = document.getElementById("player");
    player.src = `./data/normalized/${elem.path}`;
    player.play();

    // Display song data
    document.getElementById("currentImage").src = `${getAlbumImage(elem)}`;
    document.getElementById("currentSong").innerHTML = `${elem.name}<br/>by ${elem.artist}`;

    // Set media session
    navigator.mediaSession.metadata = new MediaMetadata({
        title: elem.name,
        artist: elem.artist,
        artwork: [
            { src: `${getAlbumImage(elem)}` }
        ]
    });

    updateSongHighlightColor();
}

// Create a random playlist
function prepareWholeShuffle() {
    playlist = [];
    playlistIndex = -1;

    playlist = Array.from(Array(json.musics.length).keys())
        .map((value) => ({ value, sort: Math.random() }))
        .sort((a, b) => a.sort - b.sort)
        .map(({ value }) => value);

    nextSong();
}

// Create a random playlist with the parameter as the first song
function prepareShuffle(index) {
    playlist = [];
    playlistIndex = -1;
    playlist.push(index);

    let indexes = [...Array(json.musics.length).keys()];
    indexes.splice(index, 1);

    // https://stackoverflow.com/a/46545530/6663248
    playlist = playlist.concat(indexes
        .map((value) => ({ value, sort: Math.random() }))
        .sort((a, b) => a.sort - b.sort)
        .map(({ value }) => value)
    );

    nextSong();
}

// Play a single song, used when using the share function
function playSingleSong(index) {
    playlist = [];
    playlistIndex = -1;
    playlist.push(index);

    nextSong();
}

// Sanitize a name so the user can't inject HTML with the title
function sanitize(text) {
    if (text === null) return "";
    return text
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll('\'', '&#39;');
}

// #endregion

// #region On page load

function getAlbumImage(elem) {
    if (elem.album === null || elem.album === undefined || !(elem.album in json.albums)) {
        return "/img/CD.png";
    }
    return "/data/icon/" + json.albums[elem.album].path;
}

function getPlaylistHtml(id, name) {
    let mostPresents = {};
    let count = 0;
    for (let elem of json.musics) {
        if (elem.playlist !== id && id !== "all") { // We filter by playlist (except if current playlist is "all")
            continue;
        }
        count++;
        if (elem.album === null) {
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

    return `
    <div class="song card" onclick="window.location=window.location.origin + window.location.pathname + '?playlist=${id}';">
        <div class="is-flex is-flex-wrap-wrap is-gap-0 playlist-img-container card-image">
        ${htmlImgs}
        </div>
        <div class="card-content has-text-centered">
            <p>
                ${sanitize(name)}<br/>
                ${count} songs
            </p>
        </div>
    </div>
    `;
}

function displayPlaylists(playlists, id, filter) {
    let html = "";
    if (metadataJson.showAllPlaylist && json.musics.length > 0) {
        html += getPlaylistHtml("all", "All");
    }
    for (let elem in playlists) {
        const p = playlists[elem];
        if (filter === "" || sanitize(p.name).toLowerCase().includes(filter)) {
            html += getPlaylistHtml(elem, p.name);
        }
    }
    if (json.musics.some(x => x.playlist === "default") && (filter === "" || sanitize("Unnamed").toLowerCase().includes(filter))) {
        html += getPlaylistHtml("default", "Unnamed");
    }
    document.getElementById(id).innerHTML = html;
}

/// Update the song HTML of the element given in parameter
function updateSingleSongDisplay(node, elem) {
    let selectTags = "";
    let currentTags = "";
    if (json.tags)
    {
        for (let tag of json.tags)
        {
            selectTags += `<option value="${tag}">${tag}</option>`;
        }
    }
    if (elem.tags)
    {
        for (let tag of elem.tags)
        {
            currentTags += `<span class="tag">${tag}</span>`;
        }
    }

    if (node.classList !== undefined && node.classList.contains("song")) node.dataset.name = elem.name;
    else node.querySelector(".song").dataset.name = elem.name;
    let albumImg = getAlbumImage(elem);
    node.querySelector("img").src = albumImg;
    node.querySelector("p").innerHTML = `${sanitize(elem.name)}<br/>${sanitize(elem.artist)}`;
    node.querySelector(".tags-container").innerHTML = currentTags;
    node.querySelector("select").innerHTML = selectTags;
    if (isMinimalist)
    {
        node.querySelector(".song-img").classList.add("is-hidden");
    }
    else
    {
        node.querySelector(".song-img").classList.remove("is-hidden");
    }
    if (isLoggedIn())
    {
        node.querySelector("button").classList.remove("is-hidden");
    }
    else
    {
        node.querySelector("button").classList.add("is-hidden");
    }
}

/// Return song unique identifier
function getSongKey(song) {
    if (song.key) return song.key;
    return `${song.name}_${song.artist ?? ""}_${song.type ?? ""}`;
}

/// Update displayed songs
/// @params musics: List of songs to take from
/// @params id: id of the div to update in the HTML
/// @params filter: text to filter on
/// @params doesSort: do we sort the songs by character order
/// @params doesShuffle: do we randomize the position of the songs BEFORE slicing them (meaning the n songs taken are random)
function displaySongs(musics, id, filter, doesSort, doesShuffle, count) {
    if (filter === "") {
        if (doesShuffle) {
            musics = musics
            .map(value => ({ value, sort: Math.random() }))
            .sort((a, b) => a.sort - b.sort)
            .map(({ value }) => value);
        }
        musics = musics.slice(0, count);
    } else {
        res = []
        for (const elem of musics)
        {
            if ((elem.tags && elem.tags.some(x => x.toLowerCase().includes(filter))) ||
            elem.name.toLowerCase().includes(filter) ||
            (elem.artist != null && elem.artist.toLowerCase().includes(filter)) ||
            wanakana.toRomaji(elem.name).toLowerCase().includes(filter) ||
            (elem.artist != null && wanakana.toRomaji(elem.artist).toLowerCase().includes(filter)))
            {
                res.push(elem);
                if (res.length == count) break;
            }
        }
        musics = res;
    }
    if (doesSort) {
        musics.sort((a, b) => a.name.localeCompare(b.name));
    }

    if (filter !== "" && musics.length == 0) {
        document.getElementById(id).innerHTML = "<b>No song name or artist is matching your search</b>";
    } else {
        document.getElementById(id).innerHTML = "";
        for (let elem of musics) {

            const curr = elem;
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
            node.querySelector(".song-edit").addEventListener("click", () => {
                target.hidden = !target.hidden;
                if (!target.hidden) {
                    form.reset();

                    for (var i = 0, len = form.elements.length; i < len; ++i) {
                        form.elements[i].disabled = false;
                    }
                    form.getElementsByClassName("edit-source")[0].value = elem.source;
                }
            });
            node.querySelector(".song-repair").addEventListener("click", () => {
                if (isRepairOngoing) {
                    alert("A repair is already ongoing")
                } else {
                    let res = confirm("Are you sure you want to repair this song?");
                    if (res) {
                        isRepairOngoing = true;
                        repairSong(getSongKey(elem), () => {
                            isRepairOngoing = false;
                        }, () => {
                            isRepairOngoing = false;
                        });
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

                    if (data["Tags"] !== undefined) {
                        curr.tags = data["Tags"];
                    }

                    updateSingleSongDisplay(document.getElementById(idContainer), curr);
                }, () => {
                    target.hidden = true;
                });
            });

            updateSingleSongDisplay(node, curr);
            document.getElementById(id).appendChild(node);
            updateSingleSongDisplay(document.getElementById(idContainer), curr);
        }

        // Playlist changed, maybe there is a song we should highlight now
        updateSongHighlightColor();
    }
}

function addZero(nb) {
    if (nb <= 9) {
        return "0" + nb;
    }
    return nb;
}

let oldRanges = "";

async function updateScrobblerAsync() {
    // last.fm documentation says a song can be srobbled if we listended for more than its halve, or more than 4 minutes
    console.log(`[Song] Last song was listened for a duration of ${timeListened} out of ${trackDuration} seconds`);
    if (currSong !== null && trackDuration != 0 && (timeListened > trackDuration / 2 || timeListened > 240))
    {
        registerScrobbleAsync(currSong.name, currSong.artist, currSong.album, trackDuration, timeStarted);
    }
}

function loadPage() {
    const url = new URL(window.location.href);

    document.getElementById("back").addEventListener("click", () => {
        window.location=window.location.origin + window.location.pathname;
    });

    // Set media session
    navigator.mediaSession.setActionHandler('previoustrack', previousSong);
    navigator.mediaSession.setActionHandler('nexttrack', nextSong);

    let player = document.getElementById("player");

    // Set player buttons
    document.getElementById("repeat").addEventListener("click", (_) => {
        replayMode++;
        if (replayMode === 3) replayMode = 0;

        let elem = document.getElementById("repeat");
        if (replayMode === 0)
        {
            elem.innerHTML = `<span class="material-symbols-outlined unactive">repeat</span>`;
        }
        else if (replayMode === 1)
        {
            elem.innerHTML = `<span class="material-symbols-outlined">repeat</span>`;
        }
        else
        {
            elem.innerHTML = `<span class="material-symbols-outlined">repeat_one</span>`;
        }
    });
    document.getElementById("previous").addEventListener("click", previousSong);
    document.getElementById("skip").addEventListener("click", nextSong);
    document.getElementById("togglePlay").addEventListener("click", togglePlay);
    document.getElementById("share")?.addEventListener("click", (_) => {
        const playlist = url.searchParams.get("playlist");
        const newUrl = window.location.origin + window.location.pathname + `?playlist=${playlist}&song=${encodeURIComponent(`${currSong.name}_${currSong.artist}`)}`;
        if (navigator.share)
        {
            navigator.share({url: newUrl});
        }
        else
        {
            window.prompt("Copy to share", newUrl)
        }
    });
    document.getElementById("download")?.addEventListener("click", (_) => {
        let b = document.getElementById("download");
        b.disabled = true;
        fetch(`/data/normalized/${currSong.path}`)
        .then(resp => resp.ok ? resp.blob() : Promise.reject(`Code ${resp.status}`))
        .then(blob => {
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.display = 'none';
            a.href = url;
            a.download = currSong.path;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            b.disabled = false;
        })
        .catch((err) => {
            document.getElementById("error-log").innerHTML += `<div class="error">Download "${currSong.name}" by "${currSong.artist}" failed: ${err}</div>`;
            b.disabled = false;
        });
    });
    document.getElementById("archive")?.addEventListener("click", (_) => {
        let res = confirm("Are you sure you want to archive this song?");
        if (res) {
            archiveSong(getSongKey(currSong), () => {
                currSong.isArchived = true;
                json.musics = json.musics.filter(x => !x.isArchived);
                nextSong();
            }, () => { });
        }
    });
    document.getElementById("volume").addEventListener("change", (_) => {
        player.volume = document.getElementById("volume").value / 100;
    });
    document.getElementById("durationSlider").addEventListener("change", (_) => {
        player.currentTime = document.getElementById("durationSlider").value;
    });

    function toggleHide(id) {
        if (document.getElementById(id).classList.contains("is-hidden"))
        {
            document.getElementById(id).classList.remove("is-hidden");
        }
        else
        {
            document.getElementById(id).classList.add("is-hidden");
        }
    }
    document.getElementById("random-title").addEventListener("click", _ => {
        toggleHide("songlist");
    });
    document.getElementById("highlight-title").addEventListener("click", _ => {
        toggleHide("highlightlist");
    });
    document.getElementById("latest-title").addEventListener("click", _ => {
        toggleHide("latestlist");
    });

    document.getElementById("check-integrity")?.addEventListener("click", _ => {
        validateIntegrity();
    });

    // Player callbacks
    player.addEventListener('volumechange', (_) => {
        document.getElementById("volume").value = player.volume * 100;
    });
    player.addEventListener('play', (_) => {
        document.getElementById("togglePlay").innerHTML = '<span class="material-symbols-outlined">pause</span>';
    });
    player.addEventListener('pause', (_) => {
        document.getElementById("togglePlay").innerHTML = '<span class="material-symbols-outlined">play_arrow</span>';
    });
    player.addEventListener('loadedmetadata', (_) => {
        document.getElementById("maxDuration").innerHTML = Math.trunc(player.duration / 60) + ":" + addZero(Math.trunc(player.duration % 60));
        document.getElementById("durationSlider").max = player.duration;

        // A song was played
        registerNowPlayingAsync(currSong.name, currSong.artist, currSong.album, player.duration);
        trackDuration = player.duration;
    });
    player.addEventListener('timeupdate', (_) => {
        let html = "";
        for (let i = 0; i < player.buffered.length; i++) {
            const start = player.buffered.start(i) / player.duration * 100;
            const width = (player.buffered.end(i) / player.duration * 100) - start;
            html += `<div style="margin-left:${start}%;width:${width}%;"></div>`;
        }
        if (oldRanges !== html)
        {
            oldRanges = html;
            document.getElementById("progressRanges").innerHTML = html;
        }
        document.getElementById("currDuration").innerHTML = Math.trunc(player.currentTime / 60) + ":" + addZero(Math.trunc(player.currentTime % 60));
        document.getElementById("durationSlider").value = player.currentTime;
        if (player.currentTime - lastTimeUpdate < 1) // If it's more than 1s, we can assume the user moved the cursor elsewhere in the song
        {
            timeListened += player.currentTime - lastTimeUpdate;
        }
        lastTimeUpdate = player.currentTime;
    });
    document.getElementById("currentImage").addEventListener("error", (err) => {
        document.getElementById("error-log").innerHTML += `<div class="error">Loading "${currSong.name}" by "${currSong.artist}" thumbnail failed: ${err.target.src}</div>`;
    });
    player.addEventListener("error", (err) => {
        document.getElementById("error-log").innerHTML += `<div class="error">Loading "${currSong.name}" by "${currSong.artist}" music failed: ${err.target.src}</div>`;
        nextSong();
    });

    // Audio player config
    player.volume = 0.5; // Base volume is too loud
    // When song end, we start the next one
    player.addEventListener('ended', function() {
        // Play next song if playlist isn't empty
        if (playlistIndex < playlist.length) {
            nextSong();
        }
    });

    // Display songs
    if (json.musics !== undefined)
    {
        if (json.musics.length > 5) {
            displaySongs(json.musics, "songlist", "", false, true, 5);
            document.getElementById("random").classList.remove("is-hidden");
        } else {
            document.getElementById("random").classList.add("is-hidden");
        }
        if (json.highlight !== undefined && json.highlight.length > 0) {
            document.getElementById("highlight").hidden = false;
            displaySongs(json.musics.filter(x => json.highlight.includes(x.name) && (x.type === undefined || x.type === null)), "highlightlist", "", false, false, 5);
        }
        displaySongs(json.musics.slice(-15).reverse(), "latestlist", "", false, false, 15);

        lookForSong(url);
    }
}

function lookForSong(url)
{
    const song = url.searchParams.get("song");
    if (song !== null) {
        let found = false;
        for (let i = 0; i < json.musics.length; i++) {
            const s = json.musics[i];
            if (song === `${s.name}_${s.artist}`)
            {
                playSingleSong(i);
                found = true;
                break;
            }
        }
        if (!found) {
            console.warn(`Impossible to find song matching ${song}`);
        }
    }
}

// #endregion

// #region onclick events

function random() {
    prepareWholeShuffle();
}

function refresh() {
    displaySongs(json.musics, "songlist", "", false, true, 5);
}

let isMinimalist = false;
function toggleMinimalistMode() {
    isMinimalist = !isMinimalist;

    if (isMinimalist) {
        document.getElementById("currentImage").classList.add("is-hidden");
        for (let e of document.querySelectorAll(".song-img"))
        {
            e.classList.add("is-hidden");
        }
    } else {
        document.getElementById("currentImage").classList.remove("is-hidden");
        for (let e of document.querySelectorAll(".song-img"))
        {
            e.classList.remove("is-hidden");
        }
    }
}

function chooseDisplay() {
    // Get ?playlist parameter
    const url = new URL(window.location.href);
    let playlist = url.searchParams.get("playlist");

    // If parameter is not set or set to a wrong value
    if ((!metadataJson.showAllPlaylist || playlist !== "all") && (playlist === null || playlist === undefined || json["playlists"] === undefined || json["playlists"][playlist] === undefined)) {
        // If there is no playlist we just display the default one
        if (json.musics !== undefined && json.musics.some(x => x.playlist !== "default" && x.playlist !== null && x.playlist !== undefined)) {
            if (playlist === "default") {
                playlist = "default";
            } else {
                playlist = null;
            }
        } else {
            playlist = "default";
        }
    }

    currentPlaylist = playlist;
    if (playlist === null) { // Display playlist
        document.getElementById("pageStateReady").hidden = true;
        document.getElementById("pageStatePlaylist").hidden = false;
        document.getElementById("back").hidden = true;
        displayPlaylists(json.playlists, "playlistlist", "");
    } else { // Display songs of corresponding playlist
        document.getElementById("pageStateReady").hidden = false;
        document.getElementById("pageStatePlaylist").hidden = true;
        document.getElementById("back").hidden = false;
        if (json.musics !== undefined)
        {
            json.musics = json.musics.filter(x => playlist === "all" || x.playlist === playlist || (playlist === "default" && (x.playlist === undefined || x.playlist === null)));
            for (let id in json.musics) {
                json.musics[id].id = id;
            }
        }
    }
    loadPage();
}

export async function musics_initAsync() {
    // Buttons
    document.getElementById("refresh-btn").addEventListener("click", refresh);
    document.getElementById("random-btn").addEventListener("click", random);
    document.getElementById("minimalistMode")?.addEventListener("click", toggleMinimalistMode);

    deleteCookie("sessionToken");
    document.getElementById("toggleAdmin").addEventListener("click", () => {
        if (isLoggedIn()) {
            logOff();
        } else {
            var pwd = window.prompt("Enter admin password");
            getApiToken(pwd, () => {
                alert("You are now logged as an admin");
            }, () => {
                alert("Login failed");
            })
        }
    });

    json = JSON.parse(document.getElementById("data").innerText);
    metadataJson = JSON.parse(document.getElementById("metadata").innerText);
    if (json.musics) {
        json.musics = json.musics.filter(x => !x.isArchived);
    }

    if (json.playlists) {
        for (let [key, value] of Object.entries(json.playlists)) {
            document.getElementById("upload-playlist").innerHTML += `<option value="${key}">${value.name}</option>`;
        }
    }

    // Filter text bar
    document.getElementById("filter").addEventListener("input", (e) => {
        let filterValue = e.target.value.toLowerCase();
        document.getElementById("refresh-btn").disabled = filterValue !== "";
        if (currentPlaylist === null) {
            displayPlaylists(json.playlists, "playlistlist", filterValue);
        } else {
            displaySongs(json.musics, "songlist", filterValue, true, true, filterValue !== "" ? 15 : 5);
        }
    });
    document.getElementById("filter").value = "";

    chooseDisplay();
}

window.onkeydown = function(e){
    if (e.key === ' ' && document.activeElement.type !== "text") { // Spacebar switch play/pause
        e.preventDefault(); // Prevent page from scrolling down
        togglePlay();
    }
    else if (e.key === 'ArrowLeft') {
        let player = document.getElementById("player");
        player.currentTime -= 5.0; // TODO: Somehow only move with a step of 1
    }
    else if (e.key === 'ArrowRight') {
        let player = document.getElementById("player");
        player.currentTime += 5.0;
    }
    else if (e.key == 'Enter') {
        e.preventDefault();
        e.target.blur();
    }
}

/*
window.onbeforeunload = async function() {
    //TODO
    //await updateScrobblerAsync();
}
*/