document.onreadystatechange = async function () {
    if (document.readyState == "interactive") {
        chrome.storage.local.get(["website", "token"]).then((result) => {
            document.getElementById("website").innerHTML = result.website;
            document.getElementById("token").innerHTML = result.token === undefined ? "Not set" : "Set";
        });
        document.getElementById("resetAll").addEventListener("click", _ => {
            chrome.storage.local.remove("website");
            chrome.storage.local.remove("token");
            document.getElementById("website").innerHTML = undefined;
            document.getElementById("token").innerHTML = "Not set";
        });
    }
};