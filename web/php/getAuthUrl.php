<?php
if (!isset($_GET["method"]))
{
    header("Status: 400 Bad Request");
    return;
}

$token = "";
$sk = "";
if (isset($_GET["token"]))
{
    $token = "token" . $_GET["token"];
}
else if (isset($_GET["sk"]))
{
    $sk = "sk" . $_GET["sk"];
}
else
{
    header("Status: 400 Bad Request");
    return;
}

$credentials = json_decode(file_get_contents("../data/credentials.json"), true)['lastfm'];

// TODO: last.fm auth is a massive pain and I don't want to spent 4 hours doing parsing for it
$url = "";
if (isset($_GET["album"])) $url .= "album" . $_GET["album"];
$url .= "api_key" . $credentials['apiKey'];
if (isset($_GET["artist"])) $url .= "artist" . $_GET["artist"];
if (isset($_GET["duration"])) $url .= "duration" . $_GET["duration"];
$url .= "method" . $_GET["method"];
$url .= $sk;
if (isset($_GET["timestamp"])) $url .= "timestamp" . $_GET["timestamp"];
$url .= $token;
if (isset($_GET["track"])) $url .= "track" . $_GET["track"];
$url .= $credentials['secret'];
echo(md5($url));