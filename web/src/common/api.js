import { deleteCookie, getCookie, setCookie } from "../main/cookie";
import { modal_showNotification } from "../main/modal";

let apiTarget;
let adminToken = null;

export async function api_initAsync() {
    apiTarget = (window.location.hostname === "localhost" ? "http://localhost:5000" : window.location.origin) + "/api/";

    const debugTarget = document.getElementById("apiTarget");
    fetch(`${apiTarget}`, {
        method: 'HEAD'
    })
    .then(resp => resp.status)
    .then(status => {
        document.getElementById("toggleAdmin").disabled = false;
        if (debugTarget)
        {
            debugTarget.innerHTML = status;
        }

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
    })
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

function handleFetchResponse(resp)
{
    const contentType = resp.headers.get("content-type");
    if (contentType && contentType.indexOf("application/json") !== -1) {
        return resp.json();
    } else {
        return Promise.reject(`Code ${resp.status}`);
    }
}

export async function getDownloadProcess(onSuccess)
{
    fetch(`${apiTarget}download/progress`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${adminToken}`
        }
    })
    .then()
    .then(handleFetchResponse)
    .then(json => {
        if (json.success) {
            onSuccess(json.data);
        }
    })
    .catch((err) => {});
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
    .then(handleFetchResponse)
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
        console.error(err);
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
    .then(handleFetchResponse)
    .then(json => {
        if (json.success) {
            prompt("Hashed password, please paste it in your credentials file", json.token);
        } else {
            alert(`Request failed: ${json.reason}`);
        }
    })
    .catch((err) => {
        alert(`Request failed: ${err}`);
        console.error(err);
    });
}

export async function uploadSong(data, onSuccess, onFailure, endpoint) {
    fetch(`${apiTarget}download/upload/${endpoint}`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${adminToken}`
        },
        body: data
    })
    .then(handleFetchResponse)
    .then(json => {
        if (json.success) {
            modal_showNotification("The song was successfully uploaded", true);
            onSuccess();
        } else {
            alert(`Failed to upload song: ${json.reason}`);
            onFailure();
        }
    })
    .catch((err) => {
        alert(`Failed to upload song: ${err}`);
        console.error(err);
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
    .then(handleFetchResponse)
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
        console.error(err);
        onFailure();
    });
}

export async function favoriteSong(key, toggle, onSuccess, onFailure) {
    const data = new FormData();
    data.append("Key", key);
    data.append("IsOn", toggle);

    fetch(`${apiTarget}data/favorite`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${adminToken}`
        },
        body: data
    })
    .then(handleFetchResponse)
    .then(json => {
        if (json.success) {
            onSuccess();
        } else {
            alert(`Failed to favorite song: ${json.reason}`);
            onFailure();
        }
    })
    .catch((err) => {
        alert(`Failed to favorite song: ${err}`);
        console.error(err);
        onFailure();
    });
}

export async function repairSong(key, onSuccess, onFailure) {
    const data = new FormData();
    data.append("Key", key);

    fetch(`${apiTarget}download/repair`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${adminToken}`
        },
        body: data
    })
    .then(handleFetchResponse)
    .then(json => {
        if (json.success) {
            onSuccess();
            alert("Your song repair is underway");
        } else {
            alert(`Failed to repair song: ${json.reason}`);
            onFailure();
        }
    })
    .catch((err) => {
        alert(`Failed to repair song: ${err}`);
        console.error(err);
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
    .then(handleFetchResponse)
    .then(json => {
        if (json.success) {
            modal_showNotification("The song upload is underway", true);
            onSuccess(json);
        } else {
            alert(`Failed to update song: ${json.reason}`);
            onFailure();
        }
    })
    .catch((err) => {
        alert(`Failed to update song: ${err}`);
        console.error(err);
        onFailure();
    });
}

export async function validateIntegrity() {
    fetch(`${apiTarget}integrity`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${adminToken}`
        }
    })
    .then(handleFetchResponse)
    .then(json => {
        if (json.success) {
            alert(`Integrity succeeded`);
        } else {
            alert(`Integrity failed: ${json.reason}`);
        }
    })
    .catch((err) => {
        alert(`Unexpected error: ${err}`);
        console.error(err);
    });
}

export async function createPlaylist(name, onSuccess, onFailure) {
    const data = new FormData();
    data.append("Name", name);

    fetch(`${apiTarget}playlist/add`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${adminToken}`
        },
        body: data
    })
    .then(handleFetchResponse)
    .then(json => {
        if (json.success) {
            onSuccess();
        } else {
            alert(`Failed to create playlist: ${json.reason}`);
            onFailure();
        }
    })
    .catch((err) => {
        alert(`Failed to create playlist: ${err}`);
        console.error(err);
        onFailure();
    });
}

export async function removePlaylist(name, onSuccess, onFailure) {
    const data = new FormData();
    data.append("Key", name);

    fetch(`${apiTarget}playlist/remove`, {
        method: 'DELETE',
        headers: {
            'Authorization': `Bearer ${adminToken}`
        },
        body: JSON.stringify(name)
    })
    .then(handleFetchResponse)
    .then(json => {
        if (json.success) {
            onSuccess();
        } else {
            alert(`Failed to delete playlist: ${json.reason}`);
            onFailure();
        }
    })
    .catch((err) => {
        alert(`Failed to delete playlist: ${err}`);
        console.error(err);
        onFailure();
    });
}