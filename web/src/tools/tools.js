import { api_initAsync, generatePassword } from "../common/api";

window.onload = function() {
    api_initAsync();

    document.getElementById("password-form").addEventListener("submit", (e) => {
        e.preventDefault();
        generatePassword(document.getElementById("password").value);
    });
}