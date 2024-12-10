<?php

require_once "vendor/autoload.php";

use Twig\Loader\FilesystemLoader;
use Twig\Environment;

$loader = new FilesystemLoader(["templates"]);
$twig = new Environment($loader);
$json = isset($_GET["json"]) && $_GET["json"] === "1";

$rawInfo = file_get_contents("https://$_SERVER[HTTP_HOST]/data/info.json");
$info = json_decode($rawInfo, true);
$metadata = json_decode(file_get_contents("https://$_SERVER[HTTP_HOST]/data/metadata.json"), true);

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
        "metadata" => $metadata,
        "og" => [
            "name" => $name,
            "description" => $description
        ]
    ]);
}