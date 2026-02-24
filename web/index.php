<?php

require_once "vendor/autoload.php";

use Twig\Loader\FilesystemLoader;
use Twig\Environment;
use Symfony\Component\Translation\Translator;
use Symfony\Component\Translation\Loader\YamlFileLoader;
use Symfony\Bridge\Twig\Extension\TranslationExtension;

$loader = new FilesystemLoader(["templates", "templates/modals"]);
$twig = new Environment($loader);

$basePath = (str_starts_with($_SERVER['HTTP_HOST'], 'localhost') ? "" : (empty($_SERVER['HTTPS']) ? 'http' : 'https') . "://$_SERVER[HTTP_HOST]/api/data");

$rawInfo = file_get_contents("$basePath/info");
$info = json_decode($rawInfo, true);
$rawMetadata = file_get_contents("$basePath/metadata");
$metadata = json_decode($rawMetadata, true);

$name = $metadata["name"];
$description = "";
$image = null;
$isReduced = false;
if (isset($_GET["song"])) {
    foreach ($info["musics"] as $m)
    {
        if ($m["key"] !== null) {
            $key = $m["key"];
        } else {
            $key = $m["name"] . "_" . ($m["artist"] ?? "") . "_" . ($m["type"] ?? "");
        }
        if ($key === $_GET["song"]) {
            $name = $m["name"];
            if ($m["artist"] !== null) {
                $name .= " by " . $m["artist"];
            }
            if ($m["thumbnailHash"] !== null && array_key_exists($m["thumbnailHash"], $info["albumHashes"])) {
                $image = "https://$_SERVER[HTTP_HOST]/data/icon/" . $info["albumHashes"][$m["thumbnailHash"]];
            }
            else if ($m["album"] !== null && array_key_exists($m["album"], $info["albums"])) {
                $image = "https://$_SERVER[HTTP_HOST]/data/icon/" . $info["albums"][$m["album"]]["path"];
            }
        }
    }
    $isReduced = true;
}
else if (isset($_GET["playlist"]))
{
    $target = $_GET["playlist"];
    foreach ($info["playlists"] as $key => $p)
    {
        if ($key == $target)
        {
            $name = $p["name"];
            $description = $p["description"];
        }
    }
}

$translator = new Translator("en");

$local = $_SERVER['HTTP_ACCEPT_LANGUAGE'];
$locals = [];
foreach (explode(",", $_SERVER['HTTP_ACCEPT_LANGUAGE']) as $local) {
    $value = explode(";", $local)[0];
    if ($value === "*") {
        array_push($locals, "en");
        break;
    }
    else {
        array_push($locals, $value);
    }
}
$translator->setlocale($locals[0]);

$langs = [ "en", "fr", "es" ];
foreach ($langs as $lang) {
    $translator->addLoader("yml", new YamlFileLoader());
    $translator->addResource(
        "yml",
        __DIR__."/translations/messages.$lang.yml",
        $lang
    );
}
$twig->addExtension(new TranslationExtension($translator));

echo $twig->render("index.html.twig", [
    "json" => $rawInfo, // Raw data containing all song info
    "rawMetadata" => $rawMetadata, // Metadata containing how the website should behave
    "metadata" => $metadata,
    "og" => [ // https://ogp.me/
        "name" => $name,
        "description" => $description,
        "image" => $image
    ],
    "local" => join(", ", $locals), // Detected user languages (for debug)
    "isReduced" => $isReduced // When sharing a song, we only display it and nothing else
]);