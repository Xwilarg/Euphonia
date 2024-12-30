<?php

require_once "vendor/autoload.php";

use Twig\Loader\FilesystemLoader;
use Twig\Environment;

$loader = new FilesystemLoader(["templates"]);
$twig = new Environment($loader);

$rawInfo = file_get_contents("$basePath/info.json");
echo $twig->render("tools.html.twig", [
    "json" => $rawInfo
]);