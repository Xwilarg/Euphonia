chrome.runtime.onMessage.addListener(
    function(request, sender, sendResponse) {
        if (request.greeting === "fetchData") {
            let url, artist, name, albumImage, albumName;

            try
            {
                url = location.href;

                if (url.startsWith("https://www.youtube.com"))
                {
                    artist = document.querySelector("#text").title;
                    name = document.querySelector("#title yt-formatted-string.ytd-watch-metadata").innerHTML;
                    const linkTarget = document.querySelector("#shelf-container .yt-video-attribute-view-model__secondary-subtitle > span");
                    const linkName = linkTarget.querySelector("a");
                    albumName = linkName ? linkName.innerHTML : linkTarget.innerHTML;
                    albumImage = document.querySelector("#shelf-container img").src;
                }
                else
                {
                    const midControls = document.querySelector(".middle-controls");
                    artist = midControls.querySelector(".subtitle a").innerHTML;
                    name = midControls.querySelector(".title").title;
                    albumName = midControls.querySelectorAll(".subtitle a")[1].innerHTML;
                    albumImage = document.querySelector("#song-image img").src;
                }
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