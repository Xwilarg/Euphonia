<?php
$url = 'http://localhost:5000/api/auth/register';
$path = dirname(__FILE__) . DIRECTORY_SEPARATOR . ".." . DIRECTORY_SEPARATOR . "data" . DIRECTORY_SEPARATOR;

$curl = curl_init();
curl_setopt_array($curl, array(
    CURLOPT_FOLLOWLOCATION => 1,
    CURLOPT_URL => $url,
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_CUSTOMREQUEST => 'POST',
    CURLOPT_POSTFIELDS => json_encode(array("Path" => $path, "Key" => $_SERVER["HTTP_HOST"])),
    CURLOPT_HTTPHEADER => array('Content-Type: application/json')
));
curl_setopt($curl, CURLOPT_SSL_VERIFYPEER, FALSE);
curl_setopt($curl, CURLOPT_SSL_VERIFYHOST, FALSE);

$response = curl_exec($curl);

curl_close($curl);
header('Content-Type: application/json; charset=utf-8');
echo $response;