using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grid;
using Grid.Building;
using Grid.Building.Data;
using Newtonsoft.Json;
using UnityEngine;
using Sirenix.OdinInspector;

namespace MySQL {
    // This class manages the saving and deleting of building data.
    public class BuildingSaveManager : MonoBehaviour {
        public static BuildingSaveManager Instance { get; private set; }

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }
        }

        private readonly List<PlacedBuilding> _buildingsLoadedFromServer = new();

        // If a building was repositioned, we destroy the old building and instatiate a new one, so we need to readd the new one here
        public void AddSelfToServerBuildingList(PlacedBuilding building) {
            _buildingsLoadedFromServer.Add(building);
        } 
        
        // As explained above, need to remove the "old" building when it was repositioned
        public void RemoveSelfFromServerBuildingList(PlacedBuilding building) {
            if (_buildingsLoadedFromServer.Contains(building)) {
                _buildingsLoadedFromServer.Remove(building);
            }
        } 
        
        [Header("References")]
        [SerializeField] private GridBuildingSystem gridBuildingSystem;

        private void OnApplicationQuit() {
            SaveBuildings();
        }

        private async void SaveBuildings() {
            await SaveUpdatedBuildings();
            await SaveNewBuildingsOnGrid();
        }
        
        public void LoadBuildings() {
            _ = LoadSavedBuildings();
        }
        

        #region Developer only!
        // This button saves all building types when clicked.
        [BoxGroup("Actions"), HorizontalGroup("Actions/Buttons")]
        [Button("Save Building Types", ButtonSizes.Large), GUIColor(0.6f, 1f, 0.6f)]
        private void SaveBuildingTypesButtonClicked() {
            _ = SaveAllBuildingData();
        }
        
        /// <summary>
        /// Saves all building type data to the database.
        /// </summary>
        /// <returns> If we saved all BuildingData </returns>
        private async Task SaveAllBuildingData() {
            // Get all values from the BuildingType enum and convert them to strings, because we store them as strings in the database
            // to prevent issues when removing enum values and the order changes
            var buildingTypes = Enum.GetValues(typeof(BuildingType))
                .Cast<BuildingType>()  // Cast to IEnumerable
                .Select(buildingType => buildingType.ToString()) // Convert each enum value to its string representation
                .ToArray(); // Create an array of the string names
            
            // Call the SaveBuildingTypes method to send the building type names to the server for saving
            if (await MySQLManager.SaveBuildingTypes(buildingTypes)) {
                // Success: Building types were saved successfully to the database
                // Add your success handling logic here (e.g., display a message to the user)
            } else {
                // Failure: There was an error saving the building types
                // Add your failure handling logic here (e.g., display an error message or retry the operation)
            }
        }

        #endregion
        
        
        /// <summary>
        /// Saves all buildings currently placed on the grid to the database.
        /// This method can only be called when logged in, as it requires a logged-in user's email.
        /// </summary>
        private async Task SaveNewBuildingsOnGrid() {
            // Create a list to hold building data for each building on the grid
            var dataList = new List<Dictionary<string, object>>();
            
            var buildingsOnGrid = gridBuildingSystem.GetGrid().GetAllBuildingDataOnGrid().
                Select(obj => obj).ToList();
            
            // Iterate through each building in the 'buildings' collection
            foreach (var building in buildingsOnGrid) {
                // Check if the building was loaded from the server before
                if (_buildingsLoadedFromServer.Any(placedBuilding => placedBuilding.GetBuildingDataFromLoadedBuilding().Item2.GridPosition == building.GridPosition)) {
                    continue; // Skip the building, as it was already loaded from the server
                }
                
                // Create a BuildingDataForServer, because this holds the BuildingType.ToString() instead of the enum representation
                var buildingData = new BuildingDataForServer {
                    type = building.Type.ToString(), // Convert the BuildingType enum to its string name
                    gridPosition = building.GridPosition  // Store the building's grid position
                };

                // Create a dictionary to hold the building data and the logged-in user's email
                var data = new Dictionary<string, object> {
                    { "building", buildingData }, // Store the building data under the key "building"
                    { "email", UserManager.LoggedInUser.Email } // Store the user's email under the key "email"
                };

                // pass the data dict to the list
                dataList.Add(data);
            }

            // Serialize each dictionary in the list into a separate JSON string and store them as a list of strings to send to the server
            // This uses Newtonsoft.Json's serialization.
            var jsonDataList = dataList.Select(JsonConvert.SerializeObject).ToList();

            // pass the json data list to the SavePlacedBuildings method and try to save the buildings
            var success = await MySQLManager.SavePlacedBuildings(jsonDataList);

            if (success) {
                // NO OP
            } else {
                // NO OP
            }
        }
        
        
        // This Method is currently seperated from the save buildings Method, because if they use the same pool of buildings,
        // The save method would add the same building again, because we have changed the values in the List in Unity directly
        // And this list is what we are using to as placeholder for buildings
        private async Task SaveUpdatedBuildings() {
            // Create a list to hold building data that needs to be sent to the server
            var dataList = new List<Dictionary<string, object>>();

            // Iterate through all buildings loaded from the server
            foreach (var building in _buildingsLoadedFromServer) {
                // Get the old and new data
                var (oldData, newData) = building.GetBuildingDataFromLoadedBuilding();

                // Check if the building's position or type has changed
                if (oldData.GridPosition.x != newData.GridPosition.x || 
                    oldData.GridPosition.z != newData.GridPosition.z ||
                    oldData.Type.ToString() != newData.Type.ToString()) {
                    // Building has been updated, prepare data to send to the server
                    var data = new Dictionary<string, object> {
                        // Create a nested dictionary to hold building information
                        { 
                            // Key will get $_POST['building'] read in the PHP file
                            // Whatever is in the SavedBuildingDataForServer object will be pulled out in the for loop in the PHP file
                            // Key: "building", Value: BuildingDataForServer object
                            "building", new SavedBuildingDataForServer 
                            {
                                // Convert the BuildingType enum to its string name
                                type = newData.Type.ToString(),
                                // Current Position on Grid
                                gridPosition = new GridPosition(newData.GridPosition.x, newData.GridPosition.z), 
                                // Position on Grid when loaded from the server to identify the building in the database
                                oldGridPosition = new GridPosition(oldData.GridPosition.x, oldData.GridPosition.z)
                            }
                        },
                        // Add player's email, so we know which users buildings we need to update
                        { 
                            // Same here, "email" will be $_POST['email'] in the PHP file - case-sensitive
                            "email", UserManager.LoggedInUser.Email 
                        }
                    };

                    // Log the JSON representation of the data (for debugging)
                    Debug.Log(JsonConvert.SerializeObject(data)); 

                    // Add the building data to the list
                    dataList.Add(data);
                }
            }

            // Only send the data to the server if there is at least one building to update
            if (dataList.Count > 0) {
                // Serialize each building's data into JSON format and convert them to a List of strings
                var jsonDataList = dataList.Select(JsonConvert.SerializeObject).ToList();

                // Hand the JSON data list to the SQL Manager.
                var success = await MySQLManager.UpdateBuildings(jsonDataList);
                
                if (success) {
                    Debug.Log("Updated Buildings");
                } else {
                    Debug.Log("Failed to update Buildings");
                }
            }
        }


        
        // Helper class to store building data for sending to the server
        [System.Serializable]
        private class BuildingDataForServer {
            public string type; 
            public GridPosition gridPosition;
        }
        
        // Extended Helper class to also store the old grid position, to identify the building in the database
        [System.Serializable]
        private class SavedBuildingDataForServer : BuildingDataForServer{
            public GridPosition oldGridPosition;
        }
        
        private async Task LoadSavedBuildings() {
            // Safety Check:
            _buildingsLoadedFromServer.Clear();
            
            var savedBuildings = await MySQLManager.LoadSavedBuildings(UserManager.LoggedInUser.Email);

            // Check if we successfully loaded the buildings
            if (savedBuildings != null) {
                foreach (var savedBuildingData in savedBuildings) {
                    // Get the Building Data List from the GridBuildingSystem and find out which BuildingData's Type is equivalent to the type that is saved
                    var buildingTypeSo = gridBuildingSystem.GetBuildingTypeSo(savedBuildingData.Type);
                    var spawnedBuilding = gridBuildingSystem.CreateBuilding(buildingTypeSo, savedBuildingData.GridPosition.x, savedBuildingData.GridPosition.z, null);
                    
                    spawnedBuilding.SetBuildingServerData(savedBuildingData);
                    _buildingsLoadedFromServer.Add(spawnedBuilding);
                }
            } else {
                // Failed
                Debug.Log("Failed to load buildings");
            }
        }
        
        // This enum represents the different types of buildings.
    }
}
