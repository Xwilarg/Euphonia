import { api_initAsync, generatePassword } from "./api";

window.onload = function() {
    api_initAsync();

    document.getElementById("password-form").addEventListener("submit", (e) => {
        e.preventDefault();
        generatePassword(document.getElementById("password").value);
    });
}