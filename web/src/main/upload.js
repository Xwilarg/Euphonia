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