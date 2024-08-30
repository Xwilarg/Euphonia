<?php
echo(json_decode(file_get_contents("../data/credentials.json"), true)['lastfm']['apiKey']);