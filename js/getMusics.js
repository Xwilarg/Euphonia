let json;

function playSong(path) {
    document.getElementById("player").src = path;
    document.getElementById("player").play();
}

window.onload = async function() {
    const resp = await fetch("php/getInfoJson.php");
    json = await resp.json();

    let html = "";
    for (let elem of json) {
        html += `
        <div class="song" onclick="playSong('/data/${elem.path}')">
            ${elem.name}
        </div>
        `;
    }

    document.getElementById("songlist").innerHTML = html;
}