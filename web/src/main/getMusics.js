// module "getMusics.js"

/*
 * Core module, manage everything related to musics
 */

import * as wanakana from 'wanakana';
import { archiveSong, createPlaylist, favoriteSong, getApiToken, getDownloadProcess, isLoggedIn, logOff, validateIntegrity } from '../common/api';
import { spawnSongNode } from './song';
import { modal_askPassword, modal_showNotification } from './modal';
import { doesUseRawAudio, isMinimalistMode } from './settings';
import { spawnNewPlaylistNode, spawnPlaylistNode } from './playlist';

let json;
let metadataJson;

// ID of the current playlist
let currentPlaylist = null;
// JSON data of the song being played
let currSong = null;

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
    // Displayer player if not here
    document.getElementById("full-player").classList.remove("is-hidden");

    // Update playlist text
    if (!isReduced) {
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

    // Update player
    if (!isReduced) {
        document.getElementById("favorite-icon").innerHTML = elem.isFavorite ? "heart_minus" : "heart_plus";
    }

    // Load song and play it
    let player = document.getElementById("player");
    player.src = `./data/${doesUseRawAudio() ? "raw" : "normalized"}/${elem.path}`;
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
export function prepareShuffle(index) {
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
export function sanitize(text) {
    if (text === null) return "";
    return text
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll('\'', '&#39;');
}

// #endregion

// #region On page load

export function getAlbumImage(elem) {
    if (elem.album === null || elem.album === undefined || !(elem.album in json.albums) || json.albums[elem.album].path === null) {
        return "/img/CD.png";
    }
    return "/data/icon/" + json.albums[elem.album].path;
}

export function displayPlaylists(playlists, id, filter) {
    if (metadataJson.showAllPlaylist && json.musics.length > 0) {
        spawnPlaylistNode("all", "All", json, id);
    }
    for (let elem in playlists) {
        const p = playlists[elem];
        if (filter === "" || sanitize(p.name).toLowerCase().includes(filter)) {
            spawnPlaylistNode(elem, p.name, json, id);
        }
    }
    if (!metadataJson.showAllPlaylist && json.musics.some(x => x.playlists.length === 0) && (filter === "" || sanitize("Unnamed").toLowerCase().includes(filter))) {
        spawnPlaylistNode("default", "Unnamed", json, id);
    }
    spawnNewPlaylistNode(id, json);
}


/// Update the song HTML of the element given in parameter
export function updateSingleSongDisplay(node, elem) {
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
    if (isMinimalistMode())
    {
        node.querySelector(".song-img").classList.add("is-hidden");
    }
    else
    {
        node.querySelector(".song-img").classList.remove("is-hidden");
    }
    if (isLoggedIn())
    {
        node.querySelector(".requires-admin").classList.remove("is-hidden");
    }
    else
    {
        node.querySelector(".requires-admin").classList.add("is-hidden");
    }
}

/// Return song unique identifier
export function getSongKey(song) {
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
            spawnSongNode(json, elem, id);
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
function loadPage() {
    const url = new URL(window.location.href);

    if (!isReduced) {
        document.getElementById("back").addEventListener("click", () => {
            window.location=window.location.origin + window.location.pathname + "?playlist=none";
        });
    
        // Set media session
        navigator.mediaSession.setActionHandler('previoustrack', previousSong);
        navigator.mediaSession.setActionHandler('nexttrack', nextSong);
    }

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
    document.getElementById("togglePlay").addEventListener("click", togglePlay);
    document.getElementById("share")?.addEventListener("click", (_) => {
        const newUrl = window.location.origin + window.location.pathname + `?song=${encodeURIComponent(getSongKey(currSong))}`;
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
        fetch(`/data/${doesUseRawAudio() ? "raw" : "normalized"}/${currSong.path}`)
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
    if (!isReduced) {
        document.getElementById("previous").addEventListener("click", previousSong);
        document.getElementById("skip").addEventListener("click", nextSong);
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
        document.getElementById("favorite").addEventListener("click", (_) => {
            favoriteSong(getSongKey(currSong), !currSong.isFavorite, () => {
                currSong.isFavorite = !currSong.isFavorite;
                document.getElementById("favorite-icon").innerHTML = currSong.isFavorite ? "heart_minus" : "heart_plus";
                updateFavorites();
            }, () => {});
        });
    }
    document.getElementById("display-more").addEventListener("click", (_) => {
        const target = document.getElementById("advanced-controls");
        if (target.classList.contains("is-hidden")) {
            target.classList.remove("is-hidden");
            document.getElementById("display-more-icon").innerHTML = "expand_circle_up";
        } else {
            target.classList.add("is-hidden");
            document.getElementById("display-more-icon").innerHTML = "expand_circle_down";
        }
    });
    document.getElementById("volume").addEventListener("change", (_) => {
        const val = document.getElementById("volume").value / 100;
        player.volume = val;
        localStorage.setItem("volume", val);
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
    if (!isReduced) {
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
    }

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
    });

    document.getElementById("currentImage").addEventListener("error", (err) => {
        document.getElementById("error-log").innerHTML += `<div class="error">Loading "${currSong.name}" by "${currSong.artist}" thumbnail failed: ${err.target.src}</div>`;
    });
    player.addEventListener("error", (err) => {
        document.getElementById("error-log").innerHTML += `<div class="error">Loading "${currSong.name}" by "${currSong.artist}" music failed: ${err.target.src}</div>`;
        nextSong();
    });

    // Read volume from local storage
    const volume = JSON.parse(localStorage.getItem("volume") ?? 0.5);
    player.volume = volume;
    document.getElementById("volume").value = volume * 100;

    // Preferences
    if (!isReduced) {
        
    }

    // Audio player config
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
        if (!isReduced) {
            if (json.musics.length > 5) {
                displaySongs(json.musics, "songlist", "", false, true, 5);
                document.getElementById("random").classList.remove("is-hidden");
            } else {
                document.getElementById("random").classList.add("is-hidden");
            }
            updateFavorites();
            displaySongs(json.musics.slice(-15).reverse(), "latestlist", "", false, false, 15);
        }

        // Check "?song=" parameter when we share a song
        lookForSong(url);
    }
}



function updateFavorites()
{
    displaySongs(json.musics.filter(x => x.isFavorite), "highlightlist", "", false, false, 5);
}

function lookForSong(url)
{
    const song = url.searchParams.get("song");
    if (song !== null) {
        let found = false;
        for (let i = 0; i < json.musics.length; i++) {
            const s = json.musics[i];
            if (song === getSongKey(s))
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

function chooseDisplay() {
    // Get ?playlist parameter
    const url = new URL(window.location.href);
    let playlist = url.searchParams.get("playlist");

    // If parameter is not set or set to a wrong value
    if (playlist != "none" && (!metadataJson.showAllPlaylist || playlist !== "all") && (playlist === null || playlist === undefined || json["playlists"] === undefined || json["playlists"][playlist] === undefined)) {
        // If there is no playlist we just display the default one
        if (json.musics !== undefined && json.musics.some(x => x.playlists.length > 0)) {
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
    if (playlist === null || playlist === "none") { // Display playlist
        document.getElementById("pageStateReady").hidden = true;
        document.getElementById("pageStatePlaylist").hidden = false;
        document.getElementById("back").classList.add("is-hidden");
        displayPlaylists(json.playlists, "playlistlist", "");
    } else { // Display songs of corresponding playlist
        document.getElementById("pageStateReady").hidden = false;
        document.getElementById("pageStatePlaylist").hidden = true;
        document.getElementById("back").classList.remove("is-hidden");
        if (json.musics !== undefined)
        {
            json.musics = json.musics.filter(x => playlist === "all" || x.playlists.includes(playlist) || (playlist === "default" && x.playlists.length === 0));
            for (let id in json.musics) {
                json.musics[id].id = id;
            }
        }
    }
    loadPage();
}

export async function musics_initAsync() {
    json = JSON.parse(document.getElementById("data").innerText);
    metadataJson = JSON.parse(document.getElementById("metadata").innerText);
    if (json.musics) {
        json.musics = json.musics.filter(x => !x.isArchived);
    }

    if (isReduced) {
        loadPage();
        return;
    }

    // Buttons
    document.getElementById("refresh-btn").addEventListener("click", refresh);
    document.getElementById("random-btn").addEventListener("click", random);

    document.getElementById("toggleAdmin").addEventListener("click", () => {
        if (isLoggedIn()) {
            logOff();
        } else {
            modal_askPassword((pwd) => {
                getApiToken(pwd, () => {
                    modal_showNotification("You are now logged in", true);
                }, () => {
                    modal_showNotification("Login failed", false);
                })
            });
        }
    });

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

    function updateProgress() {
        if (isLoggedIn()) {
            getDownloadProcess((data) => {
                let html = "";
                for (let elem of data)
                {
                    html += `<p>${elem.songName} by ${elem.songArtist} (${elem.currentState}): ${elem.error}</p>`;
                }
                document.getElementById("download-progress").innerHTML = html;
            });
        }
    }

    setInterval(updateProgress, 10_000); // 10s
    updateProgress();
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