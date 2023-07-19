<?php
if (file_exists("../data/info.json"))
{
    echo(file_get_contents("../data/info.json"));
}
else
{
    echo("{}");
}