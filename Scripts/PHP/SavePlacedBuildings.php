<?php
require 'db_connect.php'; // Include the database connection script

// Check if the database connection was successful
if (empty($conn)) {
    die("0"); // Exit with error code 0 (database connection failure)
}

// Prepare a statement to insert building data into the 'playerbuildings' table
$stmt = $conn->prepare("INSERT INTO playerbuildings (Email, BuildingName, PosX, PosZ) VALUES (?, ?, ?, ?)");

// Check if the statement preparation was successful
if (!$stmt) {
    // If the statement preparation failed, log the error and exit
    die("Prepare failed 999: (" . $conn->errno . ") " . $conn->error);
}

// Prepare a statement to check for existing buildings at a given position for a specific user
$checkBuildingQuery = $conn->prepare("SELECT * FROM playerbuildings WHERE Email = ? AND PosX = ? AND PosZ = ?");

// Initialize a counter to track the number of successfully saved buildings
$savedBuildingsAmount = 0;

// Loop through the building data received from the client
for ($i = 0; isset($_POST["data$i"]); $i++) {
    $json = $_POST["data$i"]; // Get the raw JSON data for the current building
    // associative array = each element is stored as a key-value pair : Dict<Key, Value>
    $data = json_decode($json, true); // Decode the JSON data into a PHP associative array
    $building = $data['building']; // Extract the building data from the array
    $email = $data['email']; // Extract the user's email from the array

    // Check if the data was correctly sent and received
    if (
        // Check if 'type' key exists within the 'building' array
        !isset($building['type']) ||
        // Check if 'gridPosition' key exists within the 'building' array
        !isset($building['gridPosition']) ||
        // Check if 'x' key exists within the 'gridPosition' array
        !isset($building['gridPosition']['x']) ||
        // Check if 'y' key exists within the 'gridPosition' array
        !isset($building['gridPosition']['z']) ||

        // Check if 'type' is a string
        !is_string($building['type']) ||
        // Check if 'x' is a numeric value (int or float)
        !is_numeric($building['gridPosition']['x']) ||
        // Check if 'y' is a numeric value (int or float)
        !is_numeric($building['gridPosition']['z'])
    ) {
        die("2"); // Exit with error code 2 (invalid building data)
    }

    // Get the building's type name, X position, and Z position
    $buildingTypeName = $building['type'];
    $posX = (int)$building['gridPosition']['x'];
    $posZ = (int)$building['gridPosition']['z'];

    // Check if a building already exists at this position for this user
    $checkBuildingQuery->bind_param("sii", $email, $posX, $posZ);
    $checkBuildingQuery->execute();
    $checkBuildingResult = $checkBuildingQuery->get_result();

    // If a building already exists, skip this iteration
    if ($checkBuildingResult->num_rows > 0) {
        continue;
    }

    // Bind the building data to the prepared statement
    // 'ssii' indicates that the parameters are strings, string, integer, integer
    // because in PHP all values are strings, we need to specify the types
    $stmt->bind_param("ssii", $email, $buildingTypeName, $posX, $posZ);

    // Execute the statement to insert the building data into the database
    if (!$stmt->execute()) {
        die("3: " . $conn->error); // Exit with error code 3 and the database error message
    }

    $savedBuildingsAmount++; // Increment the counter for successfully saved buildings
}

// Output a success message indicating the number of buildings saved
echo "Saved " . $savedBuildingsAmount . " buildings.";

// Close the prepared statements and the database connection
$stmt->close();
$checkBuildingQuery->close();
$conn->close();
