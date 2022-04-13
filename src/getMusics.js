import * as wanakana from 'wanakana';

let json;

// URL of the remote server
// Left empty if stored at the same place as the webpage
let url = "";

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

// Play the next song of the playlist
function nextSong() {
    // Displayer player if not here
    document.getElementById("fullPlayer").hidden = false;

    // Update playlist text
    let playlistSize = playlist.length - playlistIndex;
    document.getElementById("playlist-title").innerHTML =
    `${playlistSize} song${playlistSize > 1 ? 's' : ''} queued:<br/>`;
    document.getElementById("playlist-content").innerHTML =
        playlist
            .slice(playlistIndex, playlistIndex + 3)
            .map(x => sanitize(json.musics[x].name))
            .join("<br/>");

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

    // Color the currently played song
    for (let c of document.getElementsByClassName("current")) {
        c.classList.remove("current");
    }
    let current = document.getElementById(sanitize(elem.name));
    if (current !== null) {
        current = current.classList.add("current");
    }
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

function displaySongs(musics, id, filter) {
    let html = "";
    if (filter === "") {
        musics = musics
        .map(value => ({ value, sort: Math.random() }))
        .sort((a, b) => a.sort - b.sort)
        .map(({ value }) => value)
        .slice(0, 5);
    } else {
        musics = musics
        .filter(elem =>
            sanitize(elem.name).toLowerCase().includes(filter) ||
            sanitize(elem.artist).toLowerCase().includes(filter) ||
            sanitize(wanakana.toRomaji(elem.name)).toLowerCase().includes(filter) ||
            sanitize(wanakana.toRomaji(elem.artist)).toLowerCase().includes(filter)
        );
    }
    musics.sort((a, b) => a.name.localeCompare(b.name));

    let indexs = [];
    for (let elem of musics) {
        let albumImg = url + getAlbumImage(elem);
        // TODO: may have dupplicate ID
        html += `
        <div class="song" id="${sanitize(elem.name)}">
            <img id="img-${id}-${elem.index}" src="${albumImg}"/><br/>
            <p>
                ${sanitize(elem.name)}<br/>
                ${sanitize(elem.artist)}
            </p>
        </div>
        `;
        indexs.push(elem.index);
    }
    document.getElementById(id).innerHTML = html;
    for (let i of indexs) {
        document.getElementById(`img-${id}-${i}`).onclick = () => {
            prepareShuffle(i);
        }
    }
}

function addZero(nb) {
    if (nb <= 9) {
        return "0" + nb;
    }
    return nb;
}

let oldRanges = "";

async function loadPage() {
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
        document.getElementById("maxDuration").innerHTML = Math.trunc(player.duration / 100) + ":" + addZero(Math.trunc(player.duration % 100));
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
            document.getElementById("currDuration").innerHTML = Math.trunc(player.currentTime / 100) + ":" + addZero(Math.trunc(player.currentTime % 100));
            document.getElementById("durationSlider").value = player.currentTime;
        }
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

    // Filter text bar
    document.getElementById("filter").addEventListener("input", (e) => {
        let filterValue = e.target.value.toLowerCase();
        document.getElementById("refresh").disabled = filterValue !== "";
        displaySongs(json.musics, "songlist", filterValue);
    });
    document.getElementById("filter").value = "";

    // Buttons
    document.getElementById("remoteUrl").addEventListener("click", resetServer);
    document.getElementById("toggle-settings").addEventListener("click", toggleSettings);
    document.getElementById("refresh").addEventListener("click", refresh);
    document.getElementById("export-youtube").addEventListener("click", exportYoutube);

    // Get music infos
    const resp = await fetch(url + "php/getInfoJson.php");
    json = await resp.json();

    // Update JSON names
    for (let index in json.musics) {
        let elem = json.musics[index];
        if (elem.type !== undefined && elem.type !== null) {
            elem.name += ` (${elem.type})`;
        }
        elem.index = index;
    }

    // Display songs
    displaySongs(json.musics, "songlist", "");
    if (json.highlight.length > 0) {
        document.getElementById("highlight").hidden = false;
        displaySongs(json.musics.filter(x => json.highlight.includes(x.name) && (x.type === undefined || x.type === null)), "highlightlist", "");
    }
    displaySongs(json.musics.slice(-5), "latestlist", "");
}
// #endregion

// #region onclick events

function exportYoutube() {
    document.getElementById("export-youtube-result").innerHTML =
        json.musics
            .map(x => x.source)
            .filter(x => x !== null)
            .map(value => ({ value, sort: Math.random() }))
            .sort((a, b) => a.sort - b.sort)
            .map(({ value }) => value)
            .join('\n');
}

function refresh() {
    displaySongs(json.musics, "songlist", "");
}

// Hide / show settings
function toggleSettings() {
    document.getElementById("settings").hidden = !document.getElementById("settings").hidden;
}

// Called when changing remote server URL
async function resetServer() {
    url = document.getElementById("remoteUrl").value
    if (!url.endsWith("/")) {
        url += "/";
    }
    await loadPage();
}

window.onload = async function() {
    await loadPage();
}

window.onkeydown = function(e){
    if (e.key == ' ') { // Spacebar switch play/pause
        e.preventDefault(); // Prevent page from scrolling down
        togglePlay();
    }
}