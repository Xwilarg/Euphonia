<?php

require_once "vendor/autoload.php";

use Twig\Loader\FilesystemLoader;
use Twig\Environment;

$loader = new FilesystemLoader(["templates"]);
$twig = new Environment($loader);
$json = isset($_GET["json"]) && $_GET["json"] === "1";

$basePath = (str_starts_with($_SERVER['HTTP_HOST'], 'localhost') ? "" : (empty($_SERVER['HTTPS']) ? 'http' : 'https') . "://$_SERVER[HTTP_HOST]/") . "data";

$rawInfo = file_get_contents("$basePath/info.json");
$info = json_decode($rawInfo, true);
$rawMetadata = file_get_contents("$basePath/metadata.json");
$metadata = json_decode($rawMetadata, true);

$name = $metadata["name"];
$description = "";
if (isset($_GET["playlist"]))
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

if ($json)
{
    header('Content-Type: application/json; charset=utf-8');
    echo $rawInfo;
}
else
{
    echo $twig->render("index.html.twig", [
        "json" => $rawInfo,
        "rawMetadata" => $rawMetadata,
        "metadata" => $metadata,
        "og" => [
            "name" => $name,
            "description" => $description
        ]
    ]);
}