/*
 * Manage navigation bar
 */

export async function navbar_initAsync()
{
    if (!isReduced) {
        document.getElementById("toggle-settings").addEventListener("click", () => {
            const settings = document.getElementById("settings-dropdown");
            if (settings.classList.contains("is-hidden")) settings.classList.remove("is-hidden");
            else settings.classList.add("is-hidden");
        });
    }
    document.getElementById("toggle-volume").addEventListener("click", () => {
        const settings = document.getElementById("volume-container-dropdown");
        if (settings.classList.contains("is-hidden")) settings.classList.remove("is-hidden");
        else settings.classList.add("is-hidden");
    });
}