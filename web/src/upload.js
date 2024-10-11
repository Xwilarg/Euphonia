export async function upload_initAsync() {
    document.getElementById("upload").addEventListener("click", () => {
        document.getElementById("upload-window").hidden = !document.getElementById("upload-window").hidden;
    });
}