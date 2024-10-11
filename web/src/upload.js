export async function upload_initAsync() {
    document.getElementById("upload").addEventListener("click", () => {
        document.getElementById("upload-window").hidden = !document.getElementById("upload-window").hidden;
    });

    document.getElementById("upload-form").addEventListener("submit", (e) => {
        e.preventDefault();
        console.log(e);
    });

    document.getElementById("upload-url").addEventListener("change", (e) => {
        const r = /youtu\.?be(\.com)?\/[^\?]+\?(v|si)=([^&]+)/g.exec(e.target.value);
        if (r !== null)
        {
            document.getElementById("upload-yt-player").src = `https://www.youtube-nocookie.com/embed/${r[3]}`;
        }
    });
}