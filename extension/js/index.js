let website;
let token;

async function initAsync() {
    chrome.storage.local.get(["website", "token"]).then((result) => {
        if (result.website !== undefined) {
            website = result.website;
            document.getElementById("choose-website").classList.add("is-hidden");

            if (result.token === undefined) {
                document.getElementById("choose-password").classList.remove("is-hidden");
            } else {
                token = result.token;
                fetch(`${website}auth/validate`, { // Validate user token
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${token}`
                    }
                })
                .then(resp => resp.ok ? resp.json() : Promise.reject(`Code ${resp.status}`))
                .then(json => {
                    if (json.success) {
                        console.log("ok");
                        document.getElementById("choose-upload").classList.remove("is-hidden");
                    } else {
                        console.log(json);
                        document.getElementById("choose-password").classList.remove("is-hidden");
                    }
                })
                .catch((err) => {
                    console.log(err);
                    document.getElementById("choose-password").classList.remove("is-hidden");
                });
            }
        }
    });

    // Form to select your website
    document.getElementById("choose-website-submit").addEventListener("click", (e) => {
        e.preventDefault();

        const userEntry = document.getElementById("choose-website-url").value;

        website = `https://${userEntry}/api/`;
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
        fetch(`${website}auth/token`, {
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