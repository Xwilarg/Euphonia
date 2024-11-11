import { musics_initAsync } from "./getMusics"
import { lastfm_initAsync } from "./lastfm"
import { api_initAsync } from "./api"
import { upload_initAsync } from "./upload"

window.onload = async function() {
    for (const e of document.getElementsByClassName("requires-admin")) {
        e.classList.add("hidden");
    };

    await api_initAsync();
    await musics_initAsync();
    await lastfm_initAsync();
    await upload_initAsync();
}