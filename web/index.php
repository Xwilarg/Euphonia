<?php

require_once "vendor/autoload.php";

use Twig\Loader\FilesystemLoader;
use Twig\Environment;

$loader = new FilesystemLoader(["templates"]);
$twig = new Environment($loader);

$rawInfo = file_get_contents("data/info.json");
$info = json_decode($rawInfo, true);
$metadata = json_decode(file_get_contents("data/metadata.json"), true);

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

echo $twig->render("index.html.twig", [
    "json" => $rawInfo,
    "metadata" => $metadata,
    "og" => [
        "name" => $name,
        "description" => $description
    ]
]);