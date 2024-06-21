import * as wanakana from 'wanakana';

let json;

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
    updateScrobblerAsync();

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
    console.log(`[Song] Playing ${elem.name} by ${elem.artist} from ${elem.album}`);
    currSong = elem;
    timeListened = 0;
    lastTimeUpdate = 0;
    trackDuration = 0;
    timeStarted = Math.floor(Date.now() / 1000);
    playlistIndex++;

    // Load song and play it
    let player = document.getElementById("player");
    player.src = `${window.config_remoteUrl}/data/normalized/${elem.path}`;
    player.play();

    // Display song data
    document.getElementById("currentImage").src = `${window.config_remoteUrl}${getAlbumImage(elem)}`;
    document.getElementById("currentSong").innerHTML = `${elem.name}<br/>by ${elem.artist}`;

    // Set media session
    navigator.mediaSession.metadata = new MediaMetadata({
        title: elem.name,
        artist: elem.artist,
        artwork: [
            { src: `${window.config_remoteUrl}${getAlbumImage(elem)}` }
        ]
    });

    updateSongHighlightColor();
}

// Create a random playlist
function prepareWholeShuffle() {
    playlistIndex = Math.floor(Math.random() * json.musics.length);

    let indexes = [...Array(json.musics.length).keys()];

    // https://stackoverflow.com/a/46545530/6663248
    playlist = playlist.concat(indexes
        .map((value) => ({ value, sort: Math.random() }))
        .sort((a, b) => a.sort - b.sort)
        .map(({ value }) => value)
    );

    nextSong();
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

// Play a single song, used when using the share function
function playSingleSong(index) {
    playlist = [];
    playlistIndex = 0;
    playlist.push(index);

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
        let img = window.config_remoteUrl + getAlbumImage(elem);
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
        let albumImg = window.config_remoteUrl + getAlbumImage(elem);
        html += `
        <div class="song ${sanitize(elem.name)}" id="song-${id}-${elem.id}">
            <div class="song-img${isMinimalist ? " hidden" : ""}">
                <img id="img-${id}-${elem.id}" src="${albumImg}"/>
            </div>
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
        // Add listeners
        // If we are in minimalist mode, we allow click anywhere on the song since only the text part is displayed
        // If we are not, we only allow click on the image so user can copy text without starting song
        document.getElementById(`img-${id}-${i}`).onclick = () => {
            if (!isMinimalist) {
                prepareShuffle(i);
            }
        }
        document.getElementById(`song-${id}-${i}`).onclick = () => {
            if (isMinimalist) {
                prepareShuffle(i);
            }
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
    const resp = await fetch(window.config_remoteUrl + "php/getInfoJson.php");
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

    if (json.readme !== undefined) {
        document.getElementById("readme").innerHTML = json.readme.join("<br/>");
    }
}

async function updateScrobblerAsync() {
    // last.fm documentation says a song can be srobbled if we listended for more than its halve, or more than 4 minutes
    console.log(`[Song] Last song was listened for a duration of ${timeListened} out of ${trackDuration} seconds`);
    if (currSong !== null && trackDuration != 0 && (timeListened > trackDuration / 2 || timeListened > 240))
    {
        window.lastfm_registerScrobbleAsync(currSong.name, currSong.artist, currSong.album, trackDuration, timeStarted);
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
    document.getElementById("previous").addEventListener("click", previousSong);
    document.getElementById("skip").addEventListener("click", nextSong);
    document.getElementById("togglePlay").addEventListener("click", togglePlay);
    document.getElementById("share").addEventListener("click", (_) => {
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
        document.getElementById("togglePlay").innerHTML = '<span class="material-symbols-outlined">pause</span>';
    });
    player.addEventListener('pause', (_) => {
        document.getElementById("togglePlay").innerHTML = '<span class="material-symbols-outlined">play_arrow</span>';
    });
    player.addEventListener('loadedmetadata', (_) => {
        document.getElementById("maxDuration").innerHTML = Math.trunc(player.duration / 60) + ":" + addZero(Math.trunc(player.duration % 60));
        document.getElementById("durationSlider").max = player.duration;

        // A song was played
        window.lastfm_registerNowPlayingAsync(currSong.name, currSong.artist, currSong.album, player.duration);
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
            document.getElementById("random").classList.remove("hidden");
        } else {
            document.getElementById("random").classList.add("hidden");
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

// Hide / show settings
function toggleSettings() {
    document.getElementById("settings").hidden = !document.getElementById("settings").hidden;
}

let isMinimalist = false;
function toggleMinimalistMode() {
    isMinimalist = !isMinimalist;

    if (isMinimalist) {
        document.getElementById("currentImage").classList.add("hidden");
        for (let e of document.querySelectorAll(".song-img"))
        {
            e.classList.add("hidden");
        }
    } else {
        document.getElementById("currentImage").classList.remove("hidden");
        for (let e of document.querySelectorAll(".song-img"))
        {
            e.classList.remove("hidden");
        }
    }
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
        if (json.musics !== undefined)
        {
            json.musics = json.musics.filter(x => x.playlist === playlist);
            for (let id in json.musics) {
                json.musics[id].id = id;
            }
        }
    }
    loadPage();
}

window.musics_initAsync = musics_initAsync;
async function musics_initAsync() {
    window.config_remoteUrl = "";

    // Buttons
    document.getElementById("toggle-settings").addEventListener("click", toggleSettings);
    document.getElementById("refresh").addEventListener("click", refresh);
    document.getElementById("random").addEventListener("click", random);
    document.getElementById("minimalistMode").addEventListener("click", toggleMinimalistMode);

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

/*
window.onbeforeunload = async function() {
    //TODO
    //await updateScrobblerAsync();
}
*/