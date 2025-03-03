/*
 * Handle user preferences
 */

let useRawAudio = false; // Use raw files instead of normalized
let isMinimalist = false;

export async function settings_initAsync()
{
    if (isReduced) return;

    useRawAudio = JSON.parse(localStorage.getItem("useRaw") ?? false);
    const useRawToggle = document.getElementById("use-raw");
    useRawToggle.checked = useRawAudio;
    useRawToggle.addEventListener("change", (_) => {
        useRawAudio = useRawToggle.checked;
        localStorage.setItem("useRaw", useRawAudio);
    });

    isMinimalist = JSON.parse(localStorage.getItem("isMinimalist") ?? false);
    const isMinimalistToggle = document.getElementById("use-minimalist");
    isMinimalistToggle.checked = isMinimalist;
    isMinimalistToggle.addEventListener("change", (_) => {
        isMinimalist = isMinimalistToggle.checked;
        localStorage.setItem("isMinimalist", isMinimalist);

        if (isMinimalist) {
            document.getElementById("currentImage").classList.add("is-hidden");
            for (let e of document.querySelectorAll(".song-img"))
            {
                e.classList.add("is-hidden");
            }
        } else {
            document.getElementById("currentImage").classList.remove("is-hidden");
            for (let e of document.querySelectorAll(".song-img"))
            {
                e.classList.remove("is-hidden");
            }
        }
    });
}

export function doesUseRawAudio() { return useRawAudio; }
export function isMinimalistMode() { return isMinimalist; }