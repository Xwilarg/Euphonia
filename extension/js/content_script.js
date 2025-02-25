chrome.runtime.onMessage.addListener(
    function(request, sender, sendResponse) {
        if (request.greeting === "fetchData") {
            sendResponse({
                url: location.href,
                artist: document.querySelector(".ytd-channel-name > a").innerHTML,
                name: document.querySelector("#title yt-formatted-string").innerHTML,
                albumImage: document.querySelector("#shelf-container img").src,
                albumName: document.querySelector("#shelf-container .yt-video-attribute-view-model__secondary-subtitle > span").innerHTML
            });
        }
    }
);