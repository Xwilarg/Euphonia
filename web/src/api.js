let apiTarget;

export async function api_initAsync() {
    apiTarget = (window.location.hostname === "localhost" ? "https://localhost:7066" : window.location.origin) + "/api/";

    fetch("/php/authBackend.php")
    .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
    .then(json => {
        if (json.success) {
            document.getElementById("apiTarget").innerHTML = apiTarget;
        } else {
            document.getElementById("apiTarget").innerHTML = "Internal error";
        }
    })
    .catch((err) => {
        document.getElementById("apiTarget").innerHTML = err;
    });
}

export async function getApiToken(pwd, onSuccess, onFailure) {

    fetch(`/api/token`)
    .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
    .then(json => {
        adminToken = json.token;
    })
    .catch((err) => {
        document.getElementById("error-log").innerHTML += `<div class="error">Login failed: ${err}</div>`;
    });
}