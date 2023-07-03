import * as wanakana from 'wanakana';

let json;

// URL of the remote server
// Left empty if stored at the same place as the webpage
let url = "";

// ID of the current playlist
let currentPlaylist = null;

// Next songs to play
let playlist = [];
let playlistIndex;

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
    for (let c of document.getElementsByClassName(sanitize(json.musics[playlist[playlistIndex - 1]].name))) {
        c.classList.add("current");
    }
}

// Play the next song of the playlist
function nextSong() {
    // Displayer player if not here
    document.getElementById("fullPlayer").hidden = false;

    // Update playlist text
    let playlistSize = playlist.length - playlistIndex;
    document.getElementById("playlist-title").innerHTML =
    `${playlistSize} song${playlistSize > 1 ? 's' : ''} queued:<br/>`;
    let playlistElems = document.getElementsByClassName("next-song");
    for (let i = 0; i < playlistElems.length; i++) {
        if (playlistIndex + i + 1 >= playlist.length) {
            break;
        }
        playlistElems[i].innerHTML = sanitize(json.musics[playlist[playlistIndex + i + 1]].name);
    }

    // Select current song and move playlist forward
    let elem = json.musics[playlist[playlistIndex]];
    playlistIndex++;

    // Load song and play it
    let player = document.getElementById("player");
    player.src = `${url}/data/normalized/${elem.path}`;
    player.play();

    // Display song data
    document.getElementById("currentImage").src = `${url}${getAlbumImage(elem)}`;
    document.getElementById("currentSong").innerHTML = `${elem.name}<br/>by ${elem.artist}`;

    // Set media session
    navigator.mediaSession.metadata = new MediaMetadata({
        title: elem.name,
        artist: elem.artist,
        artwork: [
            { src: `${url}${getAlbumImage(elem)}` }
        ]
    });

    updateSongHighlightColor();
}

// Create a random playlist with the parameter as the first song
function prepareShuffle(index) {
    playlist = [];
    playlistIndex = 0;
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

// Sanitize a name so the user can't inject HTML with the title
function sanitize(text) {
    return text
        .replaceAll('&', '$amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll('\'', '&#39;');
}

// #endregion

// #region On page load

function getAlbumImage(elem) {
    if (elem.album === null) {
        return "/img/CD.png";
    }
    return "/data/icon/" + json.albums[elem.album].path;
}

function getPlaylistHtml(id, name) {
    let mostPresents = {};
    let count = 0;
    for (let elem of json.musics) {
        if (elem.playlist !== id) {
            continue;
        }
        count++;
        if (elem.album === null) {
            continue;
        }
        let img = url + getAlbumImage(elem);
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
        htmlImgs = `<img src="/img/CD.png"/>`;
    } else {
        for (let img of imgs) {
            htmlImgs += `<img src="${img[0]}"/>`;
        }
    }

    return `
    <div class="song playlist-display-container" onclick="window.location=window.location.origin + window.location.pathname + '?playlist=${id}';">
        <div class="list">
        ${htmlImgs}
        </div>
        <p>
            ${sanitize(name)}<br/>
            ${count} songs
        </p>
    </div>
    `;
}

function displayPlaylists(playlists, id, filter) {
    let html = "";
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

/// Update displayed songs
/// @params musics: List of songs to take from
/// @params id: id of the div to update in the HTML
/// @params filter: text to filter on
/// @params doesSort: do we sort the songs by character order
/// @params doesShuffle: do we randomize the position of the songs BEFORE slicing them (meaning the n songs taken are random)
function displaySongs(musics, id, filter, doesSort, doesShuffle, count) {
    let html = "";
    if (filter === "") {
        if (doesShuffle) {
            musics = musics
            .map(value => ({ value, sort: Math.random() }))
            .sort((a, b) => a.sort - b.sort)
            .map(({ value }) => value);
        }
        musics = musics.slice(0, count);
    } else {
        musics = musics
        .filter(elem =>
            sanitize(elem.name).toLowerCase().includes(filter) ||
            sanitize(elem.artist).toLowerCase().includes(filter) ||
            sanitize(wanakana.toRomaji(elem.name)).toLowerCase().includes(filter) ||
            sanitize(wanakana.toRomaji(elem.artist)).toLowerCase().includes(filter)
        );
    }
    if (doesSort) {
        musics.sort((a, b) => a.name.localeCompare(b.name));
    }

    let indexs = [];
    for (let elem of musics) {
        let albumImg = url + getAlbumImage(elem);
        html += `
        <div class="song ${sanitize(elem.name)}">
            <img id="img-${id}-${elem.id}" src="${albumImg}"/><br/>
            <p>
                ${sanitize(elem.name)}<br/>
                ${sanitize(elem.artist)}
            </p>
        </div>
        `;
        indexs.push(elem.id);
    }
    document.getElementById(id).innerHTML = html;
    for (let i of indexs) {
        document.getElementById(`img-${id}-${i}`).onclick = () => {
            prepareShuffle(i);
        }
    }

    // Playlist changed, maybe there is a song we should highlight now
    updateSongHighlightColor();
}

function addZero(nb) {
    if (nb <= 9) {
        return "0" + nb;
    }
    return nb;
}

let oldRanges = "";

async function loadSongsAsync() {
    // Get music infos
    const resp = await fetch(url + "php/getInfoJson.php");
    json = await resp.json();

    // Update JSON names
    if (json.musics !== undefined)
    {
        for (let elem of json.musics) {
            if (elem.type !== undefined && elem.type !== null) {
                elem.name += ` (${elem.type})`;
            }
            if (elem.playlist === undefined || elem.playlist === null) {
                elem.playlist = "default";
            }
        }
    }
}

function loadPage() {
    document.getElementById("back").addEventListener("click", () => {
        window.location=window.location.origin + window.location.pathname;
    });

    // Set media session
    navigator.mediaSession.setActionHandler('previoustrack', previousSong);
    navigator.mediaSession.setActionHandler('nexttrack', nextSong);

    let player = document.getElementById("player");

    // Set player buttons
    document.getElementById("previous").addEventListener("click", previousSong);
    document.getElementById("skip").addEventListener("click", nextSong);
    document.getElementById("togglePlay").addEventListener("click", togglePlay);
    document.getElementById("volume").addEventListener("change", (_) => {
        player.volume = document.getElementById("volume").value / 100;
    });
    document.getElementById("durationSlider").addEventListener("change", (_) => {
        player.currentTime = document.getElementById("durationSlider").value;
    });

    // Player callbacks
    player.addEventListener('volumechange', (_) => {
        document.getElementById("volume").value = player.volume * 100;
    });
    player.addEventListener('play', (_) => {
        document.getElementById("togglePlay").innerHTML = "||";
    });
    player.addEventListener('pause', (_) => {
        document.getElementById("togglePlay").innerHTML = "▶";
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
    displaySongs(json.musics, "songlist", "", false, true, 5);
    if (json.highlight.length > 0) {
        document.getElementById("highlight").hidden = false;
        displaySongs(json.musics.filter(x => json.highlight.includes(x.name) && (x.type === undefined || x.type === null)), "highlightlist", "", false, false, 5);
    }
    displaySongs(json.musics.slice(-15).reverse(), "latestlist", "", false, false, 15);
}
// #endregion

// #region onclick events

function refresh() {
    displaySongs(json.musics, "songlist", "", false, true, 5);
}

// Hide / show settings
function toggleSettings() {
    document.getElementById("settings").hidden = !document.getElementById("settings").hidden;
}

let isMinimalist = false;
function toggleMinimalistMode() {
    isMinimalist = !isMinimalist;

    if (isMinimalist) {
        document.getElementById("currentImage").classList.add("hidden");
        document.getElementById("playlists").classList.add("hidden");
    } else {
        document.getElementById("currentImage").classList.remove("hidden");
        document.getElementById("playlists").classList.remove("hidden");
    }
}

function exportSources() {
    document.getElementById("exportSourcesField").value = json.musics
        .map((value) => ({ value, sort: Math.random() }))
        .sort((a, b) => a.sort - b.sort)
        .map(({ value }) => value.source).join('\n');
}

function chooseDisplay() {
    // Get ?playlist parameter
    const url = new URL(window.location.href);
    let playlist = url.searchParams.get("playlist");

    // If parameter is not set or set to a wrong value
    if (playlist === null || playlist === undefined || json["playlists"] === undefined || json["playlists"][playlist] === undefined) {
        // If there is no playlist we just display the default one
        if (json.musics !== undefined && json.musics.some(x => x.playlist !== "default")) {
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
        json.musics = json.musics.filter(x => x.playlist === playlist);
        for (let id in json.musics) {
            json.musics[id].id = id;
        }
        loadPage();
    }
}

// Called when changing remote server URL
async function resetServer() {
    url = document.getElementById("remoteUrl").value
    if (!url.endsWith("/")) {
        url += "/";
    }
    await loadSongsAsync();
    chooseDisplay();
}

window.musics_initAsync = musics_initAsync;
async function musics_initAsync() {
    // Buttons
    document.getElementById("remoteUrl").addEventListener("click", resetServer);
    document.getElementById("toggle-settings").addEventListener("click", toggleSettings);
    document.getElementById("refresh").addEventListener("click", refresh);
    document.getElementById("minimalistMode").addEventListener("click", toggleMinimalistMode);
    document.getElementById("exportSources").addEventListener("click", exportSources);

    await loadSongsAsync();

    // Filter text bar
    document.getElementById("filter").addEventListener("input", (e) => {
        let filterValue = e.target.value.toLowerCase();
        document.getElementById("refresh").disabled = filterValue !== "";
        if (currentPlaylist === null) {
            displayPlaylists(json.playlists, "playlistlist", filterValue);
        } else {
            displaySongs(json.musics, "songlist", filterValue, true, true);
        }
    });
    document.getElementById("filter").value = "";

    chooseDisplay();
}

window.onkeydown = function(e){
    if (e.key === ' ') { // Spacebar switch play/pause
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
}