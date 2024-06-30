<?php
header('Content-Type: application/json; charset=utf-8');
if (file_exists("../data/info.json"))
{
    echo(file_get_contents("../data/info.json"));
}
else
{
    echo("{}");
}