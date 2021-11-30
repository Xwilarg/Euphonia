let json;
let remainingIndexs;

var url = "";

function startSong(name, path) {
    let player = document.getElementById("player");
    player.src = url + path;
    player.play();

    let current = document.getElementsByClassName("current");
    if (current.length > 0) {
        current[0].classList.remove("current");
    }
    document.getElementById(name).classList.add("current");
}

function playSong(name, path, index) {
    remainingIndexs = [...Array(json.length).keys()];
    remainingIndexs.splice(index, 1);
    startSong(name, path);
}

async function loadPage() {
    const resp = await fetch(url + "php/getInfoJson.php");
    json = await resp.json();

    let html = "";
    let index = 0;
    for (let elem of json) {
        html += `
        <div class="song" id="${elem.name}" onclick="playSong('${elem.name}', '/data/${elem.path}', ${index})">
            <img src="${url + (elem.icon === undefined ? "/img/CD.png" : "/data/" + elem.icon)}"/><br/>
            ${elem.name}<br/>
            ${elem.artist}
        </div>
        `;
        index++;
    }

    let player = document.getElementById("player");
    player.volume = 0.1;
    player.addEventListener('ended', function() {
        let rand = remainingIndexs[Math.floor(Math.random() * remainingIndexs.length)];
        let item = json[rand];
        startSong(item.name, "/data/" + item.path);
        remainingIndexs.splice(rand, 1);
    });
    document.getElementById("songlist").innerHTML = html;
}

window.onload = async function() {
    if (location.hostname !== "") { // Don't init if we are local
        await loadPage();
    }
}