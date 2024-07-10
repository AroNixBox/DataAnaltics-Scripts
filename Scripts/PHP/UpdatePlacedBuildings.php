<?php
// 1. Include the Database Connection Script
require 'db_connect.php';

// Check if the database connection was successful
if (empty($conn)) {
    die("0"); // Exit with error code 0 (database connection failure)
}

// Prepare a statement to update the Building Name, X Position, and Z Position of a building
// Do that for the building where the Email, old X and old Z Position match values in the database
$updateStmt = $conn->prepare("UPDATE playerbuildings SET BuildingName = ?, PosX = ?, PosZ = ? WHERE Email = ? AND PosX = ? AND PosZ = ?");

// Check if the statement preparation was successful
if (!$updateStmt) {
    // If the statement preparation failed, log the error and exit
    die("Prepare failed 999: (" . $conn->errno . ") " . $conn->error);
}

// Initialize a counter to track the number of successfully updated buildings
$updatedBuildingsAmount = 0;

// Loop through the building data received
for ($i = 0; isset($_POST["data$i"]); $i++) {
    // Pull the Data for this iterations building
    $json = $_POST["data$i"];
    // Decode the JSON data into an associative array (like a dictionary key-value pair)
    $data = json_decode($json, true);

    // building is the key in the associative array - Json data, store its value in a variable
    $building = $data['building'];
    // same for email
    $email = $data['email'];

    // Basic data validation, TODO: More robust check as the game grows, only allow certain building types, etc.
    if (!isset($building['type'],
            $building['gridPosition']['x'],
            $building['gridPosition']['z'],
            $building['oldGridPosition']['x'],
            $building['oldGridPosition']['z'])
        || !is_string($building['type'])
        || !is_numeric($building['gridPosition']['x'])
        || !is_numeric($building['gridPosition']['z'])
        || !is_numeric($building['oldGridPosition']['x'])
        || !is_numeric($building['oldGridPosition']['z'])
    ) {
        die("2"); // Exit with error code 2 (invalid building data)
    }

    // Get the building's types old x and z position, to find the building in the database
    // Get the building's types new x and z position and type, to update the building in the database
    $buildingTypeName = $building['type'];
    $posX = (int)$building['gridPosition']['x'];
    $posZ = (int)$building['gridPosition']['z'];
    $oldPosX = (int)$building['oldGridPosition']['x'];
    $oldPosZ = (int)$building['oldGridPosition']['z'];

    // Bind the parameters to the prepared statement
    // "siisii" - yes yes (spanish)
    // in php, i = integer, s = string
    $updateStmt->bind_param("siisii", $buildingTypeName, $posX, $posZ, $email, $oldPosX, $oldPosZ);
    if (!$updateStmt->execute()) {
        die("Update failed 888: " . $conn->error);
    }

    // Increment the counter
    $updatedBuildingsAmount++;
}

// Output success message
echo "Updated " . $updatedBuildingsAmount . " buildings.";

// Close the prepared statement
$updateStmt->close();
// Close the database connection
$conn->close();
