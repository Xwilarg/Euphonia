// module "notification.js"

export function showNotification(text, isSuccess) {
    const notif = document.getElementById("notification");
    notif.innerHTML = text;
    notif.classList.remove("is-hidden");
    notif.classList.add(isSuccess ? "is-success" : "is-danger");

    setTimeout(() => {
        notif.classList.add("is-hidden");
    }, 3000);
}