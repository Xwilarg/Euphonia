<?php
if (!isset($_GET["token"]))
{
    header("Status: 400 Bad Request");
    return;
}
$credentials = json_decode(file_get_contents("../data/credentials.json"), true);
$url = "api_key" . $credentials['apiKey'] . "methodauth.getSessiontoken" . $_GET["token"] . $credentials['secret'];
echo(md5($url));