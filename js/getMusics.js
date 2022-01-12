let json;

// URL of the remote server
// Left empty if stored at the same place as the webpage
let url = "";

// Next songs to play
let playlist = [];

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

// Play the next song of the playlist
function nextSong() {
    // Update playlist text
    document.getElementById("playlist").innerHTML =
        `${playlist.length} song${playlist.length > 1 ? 's' : ''} queued:<br/>` +
        playlist
            .slice(0, 3)
            .map(x => sanitize(json.musics[x].name))
            .join("<br/>");

    // Select current song and move playlist forward
    let elem = json.musics[playlist[0]];
    playlist.shift();

    // Load song and play it
    let player = document.getElementById("player");
    player.src = `${url}/data/${elem.path}`;
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
    navigator.mediaSession.setActionHandler('nexttrack', function() { nextSong(); });

    // Color the currently played song
    let current = document.getElementsByClassName("current");
    if (current.length > 0) {
        current[0].classList.remove("current");
    }
    document.getElementById(sanitize(elem.name)).classList.add("current");
}

// Create a random playlist with the parameter as the first song
function prepareShuffle(index) {
    playlist = [];
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
    if (elem.album === "NAME") {
        return "/data/icon/" + json.albums[elem.name].path;
    }
    return "/data/icon/" + json.albums[elem.album].path;
}

function displaySongs(filter) {
    let html = "";
    let index = 0;
    let musics = json.musics;
    musics.sort((a, b) => a.name.localeCompare(b.name));
    for (let elem of musics) {
        let albumImg = url + getAlbumImage(elem);
        if (sanitize(elem.name).toLowerCase().includes(filter) || sanitize(elem.artist).toLowerCase().includes(filter)) {
            html += `
            <div class="song" id="${sanitize(elem.name)}">
                <img onclick="prepareShuffle(${index})" src="${albumImg}"/><br/>
                ${sanitize(elem.name)}<br/>
                ${sanitize(elem.artist)}
            </div>
            `;
        }
        index++;
    }
    document.getElementById("songlist").innerHTML = html;
}

async function loadPage() {
    const resp = await fetch(url + "php/getInfoJson.php");
    json = await resp.json();

    displaySongs("");

    // Audio player config
    let player = document.getElementById("player");
    player.volume = 0.1; // Base volume is way too loud
    // When song end, we start the next one
    player.addEventListener('ended', function() {
        // Play next song if playlist isn't empty
        if (playlist.length > 0) {
            nextSong();
        }
    });

    // Redirect to login page
    document.getElementById("loginUrl").href = url + "/php/login.php";

    // Filter text bar
    document.getElementById("filter").addEventListener("input", (e) => {
        displaySongs(e.target.value.toLowerCase());
    });

    // Update stuffs that uses cookies (like logins)
    updateCookieSettings();
}
// #endregion

// #region onclick events

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

// https://stackoverflow.com/a/15724300/6663248
function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

function updateCookieSettings() {
    // Display Discord username if set
    const user = getCookie("user");
    if (user !== undefined) {
        const canUpload = getCookie("canUpload");
        document.getElementById("needLogin").hidden = true;
        document.getElementById("alreadyLogged").hidden = false;
        document.getElementById("alreadyLogged").innerHTML = `You are logged as ${user}, you are ${canUpload === "1" ? "allowed" : "not allowed"} to upload songs`;
    } else {
        document.getElementById("needLogin").hidden = false;
        document.getElementById("alreadyLogged").hidden = true;
    }
}
// #endregion

window.onload = async function() {
    await loadPage();
}

window.onkeydown = function(e){
    if (e.key == ' ') {
        e.preventDefault(); // Prevent page from scrolling down
        togglePlay();
    }
}