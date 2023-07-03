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
        const apiKey = await getApiKeyAsync();
        if (apiKey === "")
        {
            console.error("last.fm API key is not set");
        }
        else
        {
            window.location.href = `https://www.last.fm/api/auth/?api_key=${apiKey}&cb=${window.location.origin}`;
        }
    });

    const url = new URL(window.location.href);
    let token = url.searchParams.get("token");

    if (token !== undefined && token !== null) // We were redirected here from last.fm
    {
        const apiKey = await getApiKeyAsync();
        if (apiKey === "")
        {
            console.error("last.fm API key is not set");
        }
        else
        {
            const signResp = await fetch(window.config_remoteUrl + `php/getAuthUrl.php?token=${token}`);
            const signature = await signResp.text();
    
            const data = new URLSearchParams();
            data.append("method", "auth.getSession");
            data.append("api_key", apiKey);
            data.append("token", token);
            data.append("api_sig", signature);
    
            const url = `https://ws.audioscrobbler.com/2.0/?format=json`;
    
            const resp = await fetch(url, {
                method: "POST",
                body: data
            });
    
            const json = await resp.json();
    
            if (json.error !== undefined) {
                console.error(`An error happened during last.fm authentification: ${json.message}`);
            }
    
            document.cookie = `lastfm_token=${json.session.key}; max-age=86400; path=/; SameSite=Strict`;
        }
    }

    document.getElementById("lastfmStatus").innerHTML = getCookie("lastfm_token") == undefined ? "Unactive" : "Active";
}