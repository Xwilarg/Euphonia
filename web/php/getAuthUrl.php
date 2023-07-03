<?php
if (!isset($_GET["method"]))
{
    header("Status: 400 Bad Request");
    return;
}

if (isset($_GET["token"]))
{
    $url = "token" . $_GET["token"]; 
}
else if (isset($_GET["sk"]))
{
    $url = "sk" . $_GET["sk"]; 
}
else
{
    header("Status: 400 Bad Request");
    return;
}
$credentials = json_decode(file_get_contents("../data/credentials.json"), true);
$url = "api_key" . $credentials['apiKey'] . "method" . $_GET["method"] . $url . $credentials['secret'];
echo(md5($url));