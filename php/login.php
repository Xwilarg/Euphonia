<?php

require __DIR__ . '/../vendor/autoload.php';

use Xwilarg\Discord\OAuth2;

$json = json_decode(file_get_contents("config.json"), true);

// https://stackoverflow.com/a/6768831/6663248
$oauth2 = new OAuth2($json["clientId"], $json["secret"], "https://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]");

if ($oauth2->isRedirected() === false)
{
    $oauth2->startRedirection(['identify']);
}
else
{
    $ok = $oauth2->loadToken();
    if (!$ok) {
        $oauth2->startRedirection(['identify']);
    } else {
        // TODO: save info
        echo "Ok";
    }
}