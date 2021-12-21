async function loadPage() {
    const resp = await fetch(url + "php/upload.php");
    json = await resp.json();
}