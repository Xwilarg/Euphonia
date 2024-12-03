import { deleteCookie, getCookie, setCookie } from "../main/cookie";

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

            const cookieAdmin = getCookie("admin");
            if (cookieAdmin)
            {
                fetch(`${apiTarget}auth/validate`, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${cookieAdmin}`
                    }
                })
                .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
                .then(json => {
                    if (json.success) {
                        adminToken = cookieAdmin;
                        for (let c of document.getElementsByClassName("requires-admin")) {
                            c.classList.remove("is-hidden");
                        }
                        document.getElementById("toggleAdmin").innerHTML = "Turn off admin mode";
                    } else {
                        console.warn(`Admin token vertification failed: ${json.reason}`);
                        logOff();
                    }
                })
                .catch((err) => {
                    console.warn(`Admin token vertification failed: ${err}`);
                    logOff();
                });
            }
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
    deleteCookie("admin");
    adminToken = null;
    for (let c of document.getElementsByClassName("requires-admin")) {
        c.classList.add("is-hidden");
    }
    document.getElementById("toggleAdmin").innerHTML = "Switch to admin mode";
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
            setCookie("admin", json.token);
            onSuccess();

            for (let c of document.getElementsByClassName("requires-admin")) {
                c.classList.remove("is-hidden");
            }
            document.getElementById("toggleAdmin").innerHTML = "Turn off admin mode";
        } else {
            onFailure();
            logOff();
        }
    })
    .catch((err) => {
        document.getElementById("error-log").innerHTML += `<div class="error">Login failed: ${err}</div>`;
        onFailure();
        logOff();
    });
}

export async function generatePassword(pwd) {
    fetch(`${apiTarget}auth/hash`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(pwd)
    })
    .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
    .then(json => {
        if (json.success) {
            prompt("Hashed password, please paste it in your credentials file", json.token);
        } else {
            alert(`Request failed: ${json.reason}`);
        }
    })
    .catch((err) => {
        alert(`Request failed: ${err}`);
    });
}

export async function uploadSong(data, onSuccess, onFailure) {
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
            alert(`Success`);
            onSuccess();
        } else {
            alert(`Failed to upload song: ${json.reason}`);
            onFailure();
        }
    })
    .catch((err) => {
        alert(`Failed to upload song: ${err}`);
        onFailure();
    });
}

export async function archiveSong(key, onSuccess, onFailure) {
    const data = new FormData();
    data.append("Key", key);

    fetch(`${apiTarget}data/archive`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${adminToken}`
        },
        body: data
    })
    .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
    .then(json => {
        if (json.success) {
            onSuccess();
        } else {
            alert(`Failed to archive song: ${json.reason}`);
            onFailure();
        }
    })
    .catch((err) => {
        alert(`Failed to archive song: ${err}`);
        onFailure();
    });
}

export async function repairSong(key, onSuccess, onFailure) {
    const data = new FormData();
    data.append("Key", key);

    fetch(`${apiTarget}data/repair`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${adminToken}`
        },
        body: data
    })
    .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
    .then(json => {
        if (json.success) {
            onSuccess();
            alert("Your song was successfully repaired");
        } else {
            alert(`Failed to repair song: ${json.reason}`);
            onFailure();
        }
    })
    .catch((err) => {
        alert(`Failed to repair song: ${err}`);
        onFailure();
    });
}

export async function updateSong(data, onSuccess, onFailure) {
    fetch(`${apiTarget}data/update`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${adminToken}`
        },
        body: data
    })
    .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
    .then(json => {
        if (json.success) {
            alert(`Your song was successfully uploaded`);
            onSuccess();
        } else {
            alert(`Failed to update song: ${json.reason}`);
            onFailure();
        }
    })
    .catch((err) => {
        alert(`Failed to update song: ${err}`);
        onFailure();
    });
}