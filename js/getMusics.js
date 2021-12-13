let json;

// URL of the remote server
// Left empty if stored at the same place as the webpage
let url = "";

// Next songs to play
let playlist = [];

// Play the next song of the playlist
function nextSong() {
    // Update playlist text
    document.getElementById("playlist").innerHTML =
        `${playlist.length} song${playlist.length > 1 ? 's' : ''} queued:<br/>` +
        playlist
            .slice(0, 3)
            .map(x => sanitize(json[x].name))
            .join("<br/>");

    // Select current song and move playlist forward
    let elem = json[0];
    playlist.shift();

    // Load song and play it
    let player = document.getElementById("player");
    player.src = `${url}/data/${elem.path}`;
    player.play();

    // Color the currently played song
    let current = document.getElementsByClassName("current");
    if (current.length > 0) {
        current[0].classList.remove("current");
    }
    document.getElementById(sanitize(elem.name)).classList.add("current");
}

// Create a random playlist with the parameter as the first song
function prepareShuffle(index) {
    playlist.push(index);

    let indexes = [...Array(json.length).keys()];
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

async function loadPage() {
    const resp = await fetch(url + "php/getInfoJson.php");
    json = await resp.json();

    let html = "";
    let index = 0;
    for (let elem of json) {
        html += `
        <div class="song" id="${sanitize(elem.name)}" onclick="prepareShuffle(${index})">
            <img src="${url + (elem.icon === undefined ? "/img/CD.png" : "/data/" + elem.icon)}"/><br/>
            ${sanitize(elem.name)}<br/>
            ${sanitize(elem.artist)}
        </div>
        `;
        index++;
    }

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
    document.getElementById("songlist").innerHTML = html;
}

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