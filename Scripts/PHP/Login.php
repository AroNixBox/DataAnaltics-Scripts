<?php

require 'db_connect.php';

if(empty($conn)){ // If the connection failed
    die("0");
}

// &_Post can read values from a webserver. So our Unity Game. CASE SENSITIVE!!!!!!!
$UserEmail = $_POST['Email'];
// MD5 function Md5 hashes the users password, as encryption
// In login we are never going to compare the passwort to the actual password in our script for safety reasons
$UserPassword = MD5($_POST['Password']);


// Check if the email and password
// Loginquery holds the Username now.
$LoginQuery = "SELECT Username, Email, Password FROM `users` WHERE Email = '".$UserEmail."' AND Password = '".$UserPassword."';";

// Execute Login Query
try {
    // Create a new Object that contains the result of our Query
    // The Query is performed through $conn->query
    $LoginResult = $conn->query($LoginQuery);

    if($LoginResult === false){ // Primary key violation means: No Email address in the Database
        // We use numbers, because we tryparse in Unity to check if this error occurred
        die("10");
    }
} catch (Exception $ex) { // Another ex
    die("20");
}

// The Query ran successfully, we need to check our login result
if($LoginResult->num_rows > 0){ // User has registered, Login successful
    // Get the Information from the row that was returned
    // Create a row Variable and set it to our fetch result
    $row = $LoginResult->fetch_assoc();

    // Everything was successful if the program reaches this point
    // Return the Username
    echo json_encode($row);
}else{ // User has not registered yet
    die("30");
}

$conn->close();
