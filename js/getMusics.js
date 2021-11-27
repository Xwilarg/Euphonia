let json;
let remainingIndexs;

function startSong(path) {
    let player = document.getElementById("player");
    player.src = path;
    player.play();
}

function playSong(path, index) {
    remainingIndexs = [...Array(json.length).keys()];
    remainingIndexs.splice(index, 1);
    startSong(path);
}

window.onload = async function() {
    const resp = await fetch("php/getInfoJson.php");
    json = await resp.json();

    let html = "";
    let index = 0;
    for (let elem of json) {
        html += `
        <div class="song" onclick="playSong('/data/${elem.path}', ${index})">
            ${elem.name}
        </div>
        `;
        index++;
    }

    document.getElementById("player").addEventListener('ended', function() {
        let rand = remainingIndexs[Math.floor(Math.random() * remainingIndexs.length)];
        let item = json[rand];
        startSong("/data/" + item.path);
        remainingIndexs.splice(rand, 1);
    });
    document.getElementById("songlist").innerHTML = html;
}