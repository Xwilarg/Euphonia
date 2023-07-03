// https://stackoverflow.com/a/21125098
function getCookie(name) {
    var match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
    if (match) return match[2];
}

async function getApiKeyAsync() {
    const resp = await fetch(window.config_remoteUrl + "php/getLastfmApiKey.php");
    return await resp.text();
}

window.lastfm_initAsync = lastfm_initAsync;
async function lastfm_initAsync()
{
    document.getElementById("lastfmLogin").addEventListener("click", async () => {
        window.location.href = `http://www.last.fm/api/auth/?api_key=${await getApiKeyAsync()}&cb=${window.location.origin}`;
    });

    const url = new URL(window.location.href);
    let token = url.searchParams.get("token");

    if (token !== undefined && token !== null) // We were redirected here from last.fm
    {
        const apiKey = await getApiKeyAsync();
        const signature = md5(`api_key${apiKey}methodauth.getSessiontoken${token}`)

        const data = new URLSearchParams();
        data.append("method", "auth.getSession");
        data.append("api_key", apiKey);
        data.append("token", token);
        data.append("api_sig", signature);

        const url = `http://ws.audioscrobbler.com/2.0/?format=json`;

        const resp = await fetch(url, {
            method: "POST",
            body: data
        });

        const json = await resp.json();

        console.log(json); // TODO: Debug what is inside and store it in cookie: https://www.last.fm/api/show/auth.getSession

        //document.cookie = `lastfm_token=${json.token}; max-age=86400; path=/; SameSite=Strict`;
    }

    document.getElementById("lastfmStatus").innerHTML = getCookie("lastfm_token") == undefined ? "Unactive" : "Active";
}