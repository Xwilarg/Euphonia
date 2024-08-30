let apiTarget;

export async function api_initAsync() {
    apiTarget = (window.location.hostname === "localhost" ? "https://localhost:7066" : window.location.origin) + "/api/";

    fetch(apiTarget)
    .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
    .then(json => {
        document.getElementById("apiTarget").innerHTML = apiTarget;
    })
    .catch((err) => {
        document.getElementById("apiTarget").innerHTML = err;
    });

}