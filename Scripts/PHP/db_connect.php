<?php
// LOGIN CREDENTIALS:
$servername = "localhost";
$dbUsername = "root";
$dbPassword = "root";
$dbName = "Worldbuilding";

// Establish Database Connection
$conn = new mysqli($servername, $dbUsername, $dbPassword, $dbName);

// Check the Connection
if($conn->connect_error){ // If failed die is a return.
    // Die stops executing the script and prompts the input to the console
    // We use numbers, because we tryparse in Unity to check if this error occured
    die("0");
}