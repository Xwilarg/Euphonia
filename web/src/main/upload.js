/*
 * Manage upload popup
 */

import { uploadSong } from "../common/api";

export async function upload_initAsync() {
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
}