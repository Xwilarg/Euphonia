// module "lastfm.js"

/*
 * Take care of last.fm integration
 */

import { setCookie, getCookie } from "./cookie"

let lastFmApiKey;

async function makeAuthCallAsync(method, params)
{
    const sk = getCookie("lastfm_key");
    if (!sk)
    {
        return;
    }

    let tokenUrl = `/php/getAuthUrl.php?sk=${sk}&method=${method}`;
    for (const [key, value] of Object.entries(params)) {
        tokenUrl += `&${key}=${encodeURI(value)}`;
    }
    const signResp = await fetch(tokenUrl);
    const signature = await signResp.text();

    const data = new URLSearchParams();
    for (const [key, value] of Object.entries(params)) {
        data.append(key, value);
    }
    data.append("method", method);
    data.append("api_key", lastFmApiKey);
    data.append("sk", sk);
    data.append("api_sig", signature);

    const url = `https://ws.audioscrobbler.com/2.0/?format=json`;

    const resp = await fetch(url, {
        method: "POST",
        body: data
    });

    return await resp.json();
}

export async function registerNowPlayingAsync(song, artist, album, length)
{
    if (!lastFmApiKey) return;
    const json = await makeAuthCallAsync("track.updateNowPlaying", {
        artist: artist,
        track: song,
        album: album,
        duration: Math.ceil(length)
    });
    console.log(`[last.fm] Updated track being played: ${JSON.stringify(json)}`);
}

export async function registerScrobbleAsync(song, artist, album, length, timestamp)
{
    if (!lastFmApiKey) return;
    const json = await makeAuthCallAsync("track.scrobble", {
        artist: artist,
        track: song,
        album: album,
        timestamp: timestamp,
        duration: Math.ceil(length)
    });
    console.log(`[last.fm] Updated track scrobbled: ${JSON.stringify(json)}`);
}

export async function lastfm_initAsync()
{
    if (document.getElementById("lastfmLogin") === null) return; // lastfm was disabled, nothing to do

    const resp = await fetch("/php/getLastfmApiKey.php");
    lastFmApiKey = await resp.text();

    if (!lastFmApiKey)
    {
        document.getElementById("lastfmStatus").innerHTML = "Unavailable";
        return;
    }
    document.getElementById("lastfmLogin").disabled = false;

    document.getElementById("lastfmLogin").addEventListener("click", async () => {
        window.location.href = `https://www.last.fm/api/auth/?api_key=${lastFmApiKey}&cb=${window.location.origin}`;
    });

    const url = new URL(window.location.href);
    let token = url.searchParams.get("token");

    if (token !== undefined && token !== null) // We were redirected here from last.fm
    {
        const apiKey = lastFmApiKey;
        if (apiKey === "")
        {
            console.error("last.fm API key is not set");
        }
        else
        {
            const signResp = await fetch(`/php/getAuthUrl.php?token=${token}&method=auth.getSession`);
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

            setCookie("lastfm_key", json.session.key);
        }
    }

    document.getElementById("lastfmStatus").innerHTML = getCookie("lastfm_key") == undefined ? "Unactive" : "Active";
}