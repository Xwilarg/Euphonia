<?php

require __DIR__ . '/../vendor/autoload.php';

use Xwilarg\Discord\OAuth2;

$json = json_decode(file_get_contents("config.json"), true);

// https://stackoverflow.com/a/6768831/6663248
$oauth2 = new OAuth2($json["clientId"], $json["secret"], "https://$_SERVER[HTTP_HOST]/php/login.php");

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
        $answer = $oauth2->getUserInformation();

        if (array_key_exists("code", $answer)) {
            exit("An error occured: " . $answer["message"]);
        } else {
            echo "<script>document.cookie = 'userToken=" . $answer["id"] . "; path=/'</script>";
            echo "You are logged as " . $answer["username"] . "#" . $answer["discriminator"] . "<br/>";
            echo "You can now close this page";
        }
    }
}