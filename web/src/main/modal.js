export async function modal_initAsync()
{
    if (isReduced) return;

    document.getElementById("password-cancel").addEventListener("click", (e) => {
        document.getElementById("password-text").value = "";
        document.getElementById("password-modal").classList.remove("is-active");
    });
    document.getElementById("password-submit").addEventListener("click", (e) => {
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

let passwordCallback = null;