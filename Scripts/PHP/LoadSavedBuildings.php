<?php

require 'db_connect.php';

if(empty($conn)){ // If the connection failed
    die("0");
}

$Email = $_POST['Email']; // Get the email from the POST data

// Prepare the SQL statement
$LoadBuildingQuery = $conn->prepare("SELECT * FROM playerbuildings WHERE playerbuildings.Email = ?");

// Bind the parameters to the SQL statement
$LoadBuildingQuery->bind_param("s", $Email);

// Execute the query
$LoadBuildingQuery->execute();

// Get the result
$LoadBuildingResult = $LoadBuildingQuery->get_result();

$buildings = array(); // Initialize an array to hold the buildings

// Fetch each row from the result
while($row = $LoadBuildingResult->fetch_assoc()){
    $buildings[] = $row; // Add the row to the buildings array
}

// Convert the buildings array to JSON
$jsonData = json_encode($buildings);

echo $jsonData; // Echo the JSON data

$LoadBuildingQuery->close(); // Close the statement
$conn->close(); // Close the connection