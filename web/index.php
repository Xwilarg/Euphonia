<?php

require_once "vendor/autoload.php";

use Twig\Loader\FilesystemLoader;
use Twig\Environment;

$loader = new FilesystemLoader(["templates"]);
$twig = new Environment($loader);

echo $twig->render("index.html.twig", [
    "json" => file_get_contents("data/info.json"),
    "metadata" => json_decode(file_get_contents("data/metadata.json"), true)
]);