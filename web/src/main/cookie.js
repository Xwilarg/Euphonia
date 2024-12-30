// module "cookie.js"

/*
 * Manage cookies
 */

// https://stackoverflow.com/a/21125098
export function getCookie(name) {
    var match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
    if (match) return match[2];
}

export function setCookie(name, value) {
    document.cookie = `${name}=${value}; max-age=86400; path=/; SameSite=Strict`;
}

export function deleteCookie(name) {
    document.cookie = `${name}=; path=/; SameSite=Strict; expires=Thu, 01 Jan 1970 00:00:01 GMT`;
}