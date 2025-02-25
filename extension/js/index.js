let website;
let token;

async function initAsync() {
    chrome.storage.local.get(["website", "token"]).then((result) => {
        if (result.website === undefined) {
            document.getElementById("choose-loading").classList.add("is-hidden");
            document.getElementById("choose-website").classList.remove("is-hidden");
        } else {
            website = result.website;

            if (result.token === undefined) {
                document.getElementById("choose-loading").classList.add("is-hidden");
                document.getElementById("choose-password").classList.remove("is-hidden");
            } else {
                token = result.token;
                fetch(`${website}?json=1`) // Get JSON info
                .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
                .then(json => {
                    document.getElementById("choose-loading").classList.add("is-hidden");
                    document.getElementById("choose-upload").classList.remove("is-hidden");

                    // TODO: dupplicated code from web

                    if (json.playlists) { // Upload available playlists
                        for (let [key, value] of Object.entries(json.playlists)) {
                            document.getElementById("upload-playlist").innerHTML += `<option value="${key}">${value.name}</option>`;
                        }
                    }

                    // Code from web/src/upload.js
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

                        document.getElementById("choose-loading").classList.add("is-hidden");
                        document.getElementById("choose-upload").classList.remove("is-hidden");
                        fetch(`${apiTarget}api/download/upload`, {
                            method: 'POST',
                            headers: {
                                'Authorization': `Bearer ${token}`
                            },
                            body: data
                        })
                        .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
                        .then(json => {
                            if (json.success) {
                                alert(`Upload is on the way`);
                                document.getElementById("choose-loading").classList.remove("is-hidden");
                                document.getElementById("choose-upload").classList.add("is-hidden");
                            } else {
                                alert(`Failed to upload song: ${err}`);
                                document.getElementById("choose-loading").classList.remove("is-hidden");
                                document.getElementById("choose-upload").classList.add("is-hidden");
                            }
                        })
                        .catch((err) => {
                            alert(`Failed to upload song: ${err}`);
                            document.getElementById("choose-loading").classList.remove("is-hidden");
                            document.getElementById("choose-upload").classList.add("is-hidden");
                        });
                    });
                })
                .catch((err) => {
                    alert("Failed to get website info")
                });
            }
        }
    });

    // Form to select your website
    document.getElementById("choose-website-submit").addEventListener("click", (e) => {
        e.preventDefault();

        const userEntry = document.getElementById("choose-website-url").value;

        website = `https://${userEntry}/`;
        chrome.storage.local.set({ website: website }).then(() => {
            document.getElementById("choose-website").classList.add("is-hidden");
            document.getElementById("choose-password").classList.remove("is-hidden");
        });
    });

    // Form to enter your password
    document.getElementById("choose-password-submit").addEventListener("click", (e) => {
        e.preventDefault();

        const userEntry = document.getElementById("choose-password-value").value;
        document.getElementById("choose-password-value").value = "";
        fetch(`${website}api/auth/token`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(userEntry)
        })
        .then(res => res.ok ? res.json() : Promise.reject(`Code ${resp.status}`))
        .then(json => {
            if (json.success) {
                token = json.token;
                chrome.storage.local.set({ token: token }).then(() => {
                    document.getElementById("choose-password").classList.add("is-hidden");
                    document.getElementById("choose-upload").classList.remove("is-hidden");
                });
            } else {
                alert("Login failed");
            }
        })
        .catch((err) => {
            alert("Login failed");
        });
    });
}


document.onreadystatechange = async function () {
    if (document.readyState == "interactive") {
        await initAsync();
    }
};