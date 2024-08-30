import { musics_initAsync } from "./getMusics"
import { lastfm_initAsync } from "./lastfm"
import { api_initAsync } from "./api"

window.onload = async function() {
    await api_initAsync();
    await musics_initAsync();
    await lastfm_initAsync();
}