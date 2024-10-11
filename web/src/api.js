let apiTarget;
let adminToken = null;

export async function api_initAsync() {
    apiTarget = (window.location.hostname === "localhost" ? "http://localhost:5000" : window.location.origin) + "/api/";

    const debugTarget = document.getElementById("apiTarget");

    fetch("/php/authBackend.php")
    .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
    .then(json => {
        if (json.success) {
            if (debugTarget !== null) debugTarget.innerHTML = apiTarget;
            document.getElementById("toggleAdmin").disabled = false;
        } else {
            if (debugTarget !== null) debugTarget.innerHTML = "Internal error";
        }
    })
    .catch((err) => {
        if (debugTarget !== null) debugTarget.innerHTML = err;
    });
}

export function isLoggedIn() {
    return adminToken !== null;
}

export function logOff() {
    adminToken = null;
    for (let c of document.getElementsByClassName("requires-backend")) {
        c.classList.add("hidden");
    }
}

export async function getApiToken(pwd, onSuccess, onFailure) {
    if (pwd === null) return;

    fetch(`${apiTarget}auth/token`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(pwd)
    })
    .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
    .then(json => {
        if (json.success) {
            adminToken = json.token;
            onSuccess();

            for (let c of document.getElementsByClassName("requires-backend")) {
                c.classList.remove("hidden");
            }
        } else {
            onFailure();
            adminToken = null;
        }
    })
    .catch((err) => {
        document.getElementById("error-log").innerHTML += `<div class="error">Login failed: ${err}</div>`;
        onFailure();
        adminToken = null;
    });
}

export async function uploadSong(data) {
    fetch(`${apiTarget}data/upload`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${adminToken}`
        },
        body: data
    })
    .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
    .then(json => {
        if (json.success) {
            alert(json);
        } else {
            alert("Failure");
        }
    })
    .catch((err) => {
        document.getElementById("error-log").innerHTML += `<div class="error">Upload failed: ${err}</div>`;
    });
}