/*
 * Manage upload popup
 */

import { uploadSong } from "../common/api";

function switchToYouTube() {
    document.querySelector("#upload-choices > .is-info").classList.remove("is-info");
    document.getElementById("upload-youtube").classList.add("is-info");

    document.getElementById("upload-field-youtube").classList.remove("is-hidden");
    document.getElementById("upload-yt-player").classList.remove("is-hidden");
    document.getElementById("upload-field-local").classList.add("is-hidden");
}
function switchToLocalFile() {
    document.querySelector("#upload-choices > .is-info").classList.remove("is-info");
    document.getElementById("upload-local").classList.add("is-info");

    document.getElementById("upload-field-youtube").classList.add("is-hidden");
    document.getElementById("upload-yt-player").classList.add("is-hidden");
    document.getElementById("upload-field-local").classList.remove("is-hidden");
}

export async function upload_initAsync() {
    if (isReduced) return;

    document.getElementById("upload-youtube").addEventListener("click", switchToYouTube);
    document.getElementById("upload-local").addEventListener("click", switchToLocalFile);

    document.getElementById("upload-file-button").addEventListener("change", e => {
        const files = e.target.files;
        if (files.length > 0)
        {
            document.getElementById("upload-file-name").innerHTML = files[0].name;
        }
    });

    document.getElementById("upload").addEventListener("click", () => {
        const popup = document.getElementById("upload-window");
        popup.classList.add("is-active");
    });

    document.getElementById("close-upload").addEventListener("click", () => {
        const popup = document.getElementById("upload-window");
        popup.classList.remove("is-active");
    });

    document.getElementById("upload-url").addEventListener("change", (_) => {
        const upload = document.getElementById("upload-url");
        const content = upload.value;
        if (content === "") {
            document.getElementById("upload-url-error").classList.add("is-hidden");
            upload.classList.remove("is-danger");
        } else {
            const r = /youtu\.?be(\.com)?\/(watch\?v=)?([^?]+)/g.exec(upload.value);
            if (r === null) {
                document.getElementById("upload-url-error").classList.remove("is-hidden");
                upload.classList.add("is-danger");
            } else {
                document.getElementById("upload-url-error").classList.add("is-hidden");
                upload.classList.remove("is-danger");
            }
        }
    });

    document.getElementById("upload-form").addEventListener("submit", (e) => {
        e.preventDefault();
        const data = new FormData(e.target);

        var form = document.getElementById("upload-form");
        for (var i = 0, len = form.elements.length; i < len; ++i) {
            form.elements[i].disabled = true;
        }
        uploadSong(data, () => {
            for (var i = 0, len = form.elements.length; i < len; ++i) {
                form.elements[i].disabled = false;
            }
            form.reset();
            document.getElementById("upload-yt-player").src = "";
            document.getElementById("upload-album-preview").classList.add("is-hidden");
        }, () => {
            for (var i = 0, len = form.elements.length; i < len; ++i) {
                form.elements[i].disabled = false;
            }
        });
    });

    document.getElementById("upload-url").addEventListener("change", (e) => {
        const r = /youtu\.?be(\.com)?\/(watch\?v=)?([^?]+)/g.exec(e.target.value);
        if (r !== null)
        {
            document.getElementById("upload-yt-player").src = `https://www.youtube-nocookie.com/embed/${r[3]}`;
        }
    });
    document.getElementById("album-url").addEventListener("change", (e) => {
        const value = document.getElementById("album-url").value;
        if (value) {
            document.getElementById("upload-album-preview").classList.remove("is-hidden");
            document.getElementById("upload-album-preview").src = value;
        } else {
            document.getElementById("upload-album-preview").classList.add("is-hidden");
        }
    });
}