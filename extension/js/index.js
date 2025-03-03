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

                    // Get data from content_script (information about current music)
                    chrome.tabs.query({active: true, currentWindow: true}, (tabs) => {
                        chrome.tabs.sendMessage(tabs[0].id, {greeting: "fetchData"}, (response) => {
                            if (response) {
                                chrome.storage.local.get(["form"]).then((data) => {
                                    let form = data.form;
                                    if (form && form.url === response.url) { // Check cache answer URL (allow to close and open the popup again with same info)
                                        document.getElementById("youtube-url").value = form.url;
                                        document.getElementById("artist").value = form.artist;
                                        document.getElementById("name").value = form.name;
                                        document.getElementById("album-url").value = form.albumImage;
                                        document.getElementById("album-name").value = form.albumName;
                                    } else { // Answer changed
                                        form = response;
                                        document.getElementById("youtube-url").value = response.url;
                                        document.getElementById("artist").value = response.artist;
                                        document.getElementById("name").value = response.name;
                                        document.getElementById("album-url").value = response.albumImage;
                                        document.getElementById("album-name").value = response.albumName;
                                        chrome.storage.local.set({ form: response }).then(() => {});
                                    }

                                    // Listen for form changes
                                    document.getElementById("artist").addEventListener("change", e => { form.artist = e.target.value; chrome.storage.local.set({ form: form }).then(() => {}); } );
                                    document.getElementById("name").addEventListener("change", e => { form.name = e.target.value; chrome.storage.local.set({ form: form }).then(() => {}); } );
                                    document.getElementById("album-url").addEventListener("change", e => { form.albumImage = e.target.value; chrome.storage.local.set({ form: form }).then(() => {}); } );
                                    document.getElementById("album-name").addEventListener("change", e => { form.albumName = e.target.value; chrome.storage.local.set({ form: form }).then(() => {}); } );
                                });
                            } else {
                                document.getElementById("choose-loading").classList.remove("is-hidden");
                                document.getElementById("choose-loading").innerHTML = chrome.i18n.getMessage("youtubeOnly") + "<br>" + chrome.i18n.getMessage("youtubeReload");
                                document.getElementById("choose-upload").classList.add("is-hidden");
                            }
                        });
                    });

                    // TODO: dupplicated code from web

                    if (json.playlists) { // Upload available playlists
                        for (let [key, value] of Object.entries(json.playlists)) {
                            document.getElementById("upload-playlist").innerHTML += `<option value="${key}">${value.name}</option>`;
                        }
                    }

                    // Code from web/src/upload.js
                    document.getElementById("upload-form").addEventListener("submit", (e) => {
                        e.preventDefault();
                        const data = new FormData(e.target);

                        document.getElementById("choose-loading").classList.remove("is-hidden");
                        document.getElementById("choose-upload").classList.add("is-hidden");
                        fetch(`${website}api/download/upload`, {
                            method: 'POST',
                            headers: {
                                'Authorization': `Bearer ${token}`
                            },
                            body: data
                        })
                        .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
                        .then(json => {
                            if (json.success) {
                                alert(chrome.i18n.getMessage("uploadOngoing"));
                                document.getElementById("choose-loading").classList.add("is-hidden");
                                document.getElementById("choose-upload").classList.remove("is-hidden");
                            } else {
                                alert(chrome.i18n.getMessage("uploadFailed") + json.message);
                                document.getElementById("choose-loading").classList.add("is-hidden");
                                document.getElementById("choose-upload").classList.remove("is-hidden");
                            }
                        })
                        .catch((err) => {
                            alert(chrome.i18n.getMessage("uploadFailed") + err);
                            document.getElementById("choose-loading").classList.add("is-hidden");
                            document.getElementById("choose-upload").classList.remove("is-hidden");
                        });
                    });
                })
                .catch((err) => {
                    alert(chrome.i18n.getMessage("infoFailed"));
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
                alert(chrome.i18n.getMessage("loginFailed"));
            }
        })
        .catch((err) => {
            alert(chrome.i18n.getMessage("loginFailed"));
        });
    });
}

document.onreadystatechange = async function () {
    if (document.readyState == "interactive") {
        await initAsync();

        for (const tr of document.getElementsByClassName("tr"))
        {
            tr.innerHTML = chrome.i18n.getMessage(tr.innerHTML);
        }
    }
};