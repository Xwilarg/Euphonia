/*
 * Take care of initializing everything
 */

import { musics_initAsync } from "./getMusics"
import { api_initAsync } from "../common/api"
import { upload_initAsync } from "./upload"
import { navbar_initAsync } from "./navbar"
import { modal_initAsync } from "./modal"
import { settings_initAsync } from "./settings"

async function initAsync() {
    for (const e of document.getElementsByClassName("requires-admin")) {
        e.classList.add("is-hidden");
    };

    await navbar_initAsync();
    await api_initAsync();
    await settings_initAsync();
    await musics_initAsync();
    await upload_initAsync();
    await modal_initAsync();
}

document.onreadystatechange = async function () {
    if (document.readyState == "interactive") {
        await initAsync();
    }
};