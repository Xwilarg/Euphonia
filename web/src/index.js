import { musics_initAsync } from "./getMusics"
import { lastfm_initAsync } from "./lastfm"

window.onload = async function() {
    await musics_initAsync();
    await lastfm_initAsync();
}