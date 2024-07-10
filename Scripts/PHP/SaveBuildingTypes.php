<?php
require 'db_connect.php';

if (empty($conn)) {
    die("0"); // Database connection error
}

// 1. Receive and Validate Data
$buildingTypesJson = $_POST['BuildingTypes'];

if (!$buildingTypesJson) {
    die("1"); // Missing building types data
}

$buildingTypeNames = json_decode($buildingTypesJson, true); // Decode into array of names

if (!is_array($buildingTypeNames) || empty($buildingTypeNames)) {
    die("2"); // Invalid building types data
}

// 2. Fetch Existing Building Types
$dbBuildingTypes = []; // New empty array
$query = "SELECT Name FROM buildingtypes"; // Fetch only the Name column
$result = $conn->query($query);  // Execute the query and store it in $result
if (!$result) {
    die("Error 3: Fetching building types failed"); // Error fetching building types
}

while ($row = $result->fetch_assoc()) { // Row is the associative array from result to make it easier to work with
    $dbBuildingTypes[] = $row['Name']; // Store the names in the array without the key, which is only 'Name'
}

// 3. Deletions
foreach ($dbBuildingTypes as $name) { // Go through all buildingTypes in the database
    if (!in_array($name, $buildingTypeNames)) {
        // If the name of the fetched buildingType from the Database is not in the provided JSON Data from Unity
        // means dev removed the Type in Unity and we want to delete it from the Database aswell.

        // Delete all playerbuildings with this BuildingName
        $deletePlayerBuildingsQuery = "DELETE FROM playerbuildings WHERE BuildingName = '$name'";
        if (!$conn->query($deletePlayerBuildingsQuery)) {
            die("Error 56:deleting playerBuildings for BuildingType $name: " . $conn->error);
        }

        // Delete the BuildingType from the buildingtypes table
        $deleteBuildingTypeQuery = "DELETE FROM buildingtypes WHERE Name = '$name'";
        if (!$conn->query($deleteBuildingTypeQuery)) {
            die("Error 57: deleting buildingType $name: " . $conn->error);
        }
    }
}

// 4. Insertions
// After we ensured that we removed all BuildingTypes that the dev removed in Unity, we can now add new ones
foreach ($buildingTypeNames as $buildingTypeName) { // Iterate over all JSON buildingTypes from Unity
    if (!in_array($buildingTypeName, $dbBuildingTypes)) { // If the buildingType is not in the database
        $insertQuery = "INSERT INTO buildingtypes (Name) VALUES ('$buildingTypeName')"; // Insert the new buildingType
        if (!$conn->query($insertQuery)) {
            // Error Handling!
            if ($conn->errno != 1062) { // 1062 = Duplicate entry
                // Connection error
                die("Error 59: inserting $buildingTypeName: " . $conn->error);
            } else {
                // If the error is a duplicate entry, which wouldnt make sense, because we checked for duplicates before
                die("Error 58: buildingType $buildingTypeName already exists");
            }

        }
    }
}

echo "Building types updated successfully!";
$conn->close();
