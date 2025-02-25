chrome.runtime.onMessage.addListener(
    function(request, sender, sendResponse) {
        if (request.greeting === "fetchData") {
            let url, artist, name, albumImage, albumName;

            try
            {
                url = location.href;
                artist = document.querySelector(".ytd-channel-name > a").innerHTML;
                name = document.querySelector("#title yt-formatted-string.ytd-watch-metadata").innerHTML;
                const linkTarget = document.querySelector("#shelf-container .yt-video-attribute-view-model__secondary-subtitle > span");
                const linkName = linkTarget.querySelector("a");
                albumName = linkName ? linkName.innerHTML : linkTarget.innerHTML;
                albumImage = document.querySelector("#shelf-container img").src;
            }
            finally
            {
                sendResponse({
                    url: url,
                    artist: artist ?? "",
                    name: name ?? "",
                    albumImage: albumImage ?? "",
                    albumName: albumName ?? ""
                });
            }
        }
        return true;
    }
);