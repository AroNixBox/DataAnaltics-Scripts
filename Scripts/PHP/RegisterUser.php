<?php

require 'db_connect.php';

if(empty($conn)){ // If the connection failed
    die("0");
}

// Send the entried values from our unity game to our php script through a post http request.
// We do that by using the $_Post
$UserEmail = $_POST['Email'];
// MD5 function Md5 hashes the users password, as encryption
$UserPassword = MD5($_POST['Password']);
$UserName = $_POST['Username'];


// Create the database to check if the users email is already in the Database
// SQL QUERY
// IN SQL a string is a  single ', the rest ist contcatination in php, and instead of + we use .
$RegisterUserQuery = "INSERT INTO users VALUES('".$UserEmail."', '".$UserName."','".$UserPassword."')";

// Execute that Query into the Database
try {
    // Create a new Object that contains the result of our Query
    // The Query is perfromed through $conn->query
    $RegisterUserResult = $conn->query($RegisterUserQuery);
    if($RegisterUserResult === false){ // Query failed to run, maybe because of primary key violation (Same register with an already existing email
        // We use numbers, because we tryparse in Unity to check if this error occured
        die("10");
    }
} catch (Exception $ex) {
    die("20");
}

// Everything was successful if the program reaches this point
echo("User: ".$UserName." was successfully registered!");
$conn->close();
