<?php

require __DIR__ . '/../vendor/autoload.php';

use Xwilarg\Discord\OAuth2;

if (!isset($_GET["url"]) || !isset($_GET["name"])) {
    exit("Bad request");
}

$json = json_decode(file_get_contents("config.json"), true);

// https://stackoverflow.com/a/6768831/6663248
$oauth2 = new OAuth2($json["clientId"], $json["secret"], "https://$_SERVER[HTTP_HOST]/php/login.php");

if ($oauth2->isRedirected() === false)
{
    echo "You are not logged in, please refresh the page";
}
else
{
    $ok = $oauth2->loadToken();
    if (!$ok) {
        echo "You are not logged in, please refresh the page";
    } else {
        $answer = $oauth2->getUserInformation();

        if (array_key_exists("code", $answer)) {
            exit("An error occured: " . $answer["message"]);
        } else if ($answer["id"] === $json["adminId"]) {
            echo shell_exec("youtube-dl -i " . $_GET['url'] . " -o \"" . __DIR__ . "/../data/" . $_GET['name'] . "\"");
            echo shell_exec("ffmpeg -i \"" . __DIR__ . "/../data/" . $_GET['name'] . "*\" \"" . __DIR__ . "/../data/" . $_GET['name'] . ".wav\"");
        } else {
            echo "You are not allowed to upload songs"
        }
    }
}