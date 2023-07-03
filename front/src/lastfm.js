// https://stackoverflow.com/a/21125098
function getCookie(name) {
    var match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
    if (match) return match[2];
}

async function getApiKeyAsync() {
    const resp = await fetch(url + "php/getLastfmApiKey.php");
    return await resp.text();
}

export async function lastfm_initAsync()
{
    document.getElementById("lastfmLogin").addEventListener("click", async () => {
        window.location.href = `http://www.last.fm/api/auth/?api_key=${getApiKeyAsync()}&cb=${window.location.origin}`
    });

    const url = new URL(window.location.href);
    let token = url.searchParams.get("token");

    if (token !== undefined && token !== null)
    {
        const apiKey = getApiKeyAsync();
        const signature = md5(`api_key${apiKey}methodauth.getSessiontoken${token}`)
        const url = `http://ws.audioscrobbler.com/2.0/?format=json&api_key=${apiKey}&token=${token}&api_sig=${signature}`;

        const resp = await fetch(url);
        const json = await resp.json();

        console.log(json); // TODO: Debug what is inside and store it in cookie
        // We were redirected here from last.fm
        //document.cookie = `lastfm_token=${signature}; max-age=86400; path=/; SameSite=Strict`;
    }

    lastfmStatus = getCookie("lastfm_token") == undefined ? "Unactive" : "Active";
}