<?php
if (!isset($_GET['file']))
{
    http_response_code(400);
}
else if (strpos($_GET['file'], "..") !== false)
{
    http_response_code(400);
}
else
{
    header("Content-Type: audio/wav");
    echo shell_exec("ffmpeg -i \"" . __DIR__ . "/../data/" . $_GET['file'] . "\" -f wav pipe:1");
}