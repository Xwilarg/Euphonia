// module "song.js"

import { repairSong, updateSong } from "../common/api";
import { getSongKey, prepareShuffle, updateSingleSongDisplay } from "./getMusics";

let isRepairOngoing;

export function spawnSongNode(curr, id, isMinimalist) {
    let template = document.getElementById("template-song");
    const node = template.content.cloneNode(true);

    let idContainer = `${curr.id}-${id}`;
    node.querySelector(".song").id = idContainer;
    // If we are in minimalist mode, we allow click anywhere on the song since only the text part is displayed
    node.querySelector(".song-img").onclick = () => {
        if (!isMinimalist) {
            prepareShuffle(curr.id);
        }
    };
    // If we are not, we only allow click on the image so user can copy text without starting song
    node.querySelector(".song").onclick = () => {
        if (isMinimalist) {
            prepareShuffle(curr.id);
        }
    }
    let target = node.querySelector(".edit-content");
    let form = node.querySelector("form");
    node.querySelector(".song-edit").addEventListener("click", () => {
        target.hidden = !target.hidden;
        if (!target.hidden) {
            form.reset();

            for (var i = 0, len = form.elements.length; i < len; ++i) {
                form.elements[i].disabled = false;
            }
            form.getElementsByClassName("edit-source")[0].value = curr.source;
            form.getElementsByClassName("edit-name")[0].value = curr.name;
            form.getElementsByClassName("edit-artist")[0].value = curr.artist;
        }
    });
    node.querySelector(".song-repair").addEventListener("click", () => {
        if (isRepairOngoing) {
            alert("A repair is already ongoing")
        } else {
            let res = confirm("Are you sure you want to repair this song?");
            if (res) {
                isRepairOngoing = true;
                repairSong(getSongKey(elem), () => {
                    isRepairOngoing = false;
                }, () => {
                    isRepairOngoing = false;
                });
            }
        }
    })
    form.addEventListener("submit", (e) => {
        e.preventDefault();

        const data = new FormData(e.target);
        data.append("Key", getSongKey(curr));

        for (var i = 0, len = form.elements.length; i < len; ++i) {
            form.elements[i].disabled = true;
        }
        updateSong(data, () => {
            target.hidden = true;

            if (data["Tags"] !== undefined) {
                curr.tags = data["Tags"];
            }

            updateSingleSongDisplay(document.getElementById(idContainer), curr);
        }, () => {
            target.hidden = true;
        });
    });

    updateSingleSongDisplay(node, curr);
    document.getElementById(id).appendChild(node);
    updateSingleSongDisplay(document.getElementById(idContainer), curr);
}