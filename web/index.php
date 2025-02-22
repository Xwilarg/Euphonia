<?php

require_once "vendor/autoload.php";

use Twig\Loader\FilesystemLoader;
use Twig\Environment;
use Symfony\Component\Translation\Translator;
use Symfony\Component\Translation\Loader\YamlFileLoader;
use Symfony\Bridge\Twig\Extension\TranslationExtension;

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

    $langs = [ "en", "fr" ];
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
        "json" => $rawInfo,
        "rawMetadata" => $rawMetadata,
        "metadata" => $metadata,
        "og" => [
            "name" => $name,
            "description" => $description
        ],
        "local" => join(", ", $locals)
    ]);
}