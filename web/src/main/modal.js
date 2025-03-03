/*
 * Handle popups
 */

export async function modal_initAsync()
{
    if (isReduced) return;

    document.getElementById("password-cancel").addEventListener("click", (e) => {
        document.getElementById("password-text").value = "";
        document.getElementById("password-modal").classList.remove("is-active");
    });
    document.getElementById("password-submit").addEventListener("submit", (e) => {
        e.preventDefault();
        if (passwordCallback === null) console.error("Password submitted when callback not set");

        passwordCallback(document.getElementById("password-text").value);
        document.getElementById("password-text").value = "";
        document.getElementById("password-modal").classList.remove("is-active");
    });
}

export function modal_askPassword(callback) {
    passwordCallback = callback;
    document.getElementById("password-modal").classList.add("is-active");
}

export function modal_showNotification(text, isSuccess) {
    const notif = document.getElementById("notification");
    notif.innerHTML = text;
    notif.classList.remove("is-hidden");
    notif.classList.add(isSuccess ? "is-success" : "is-danger");

    setTimeout(() => {
        notif.classList.add("is-hidden");
    }, 3000);
}

let passwordCallback = null;