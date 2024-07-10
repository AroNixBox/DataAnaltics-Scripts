using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grid;
using Grid.Building.Data;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace MySQL {
    public class MySQLManager {
        // The Url is localhost:[the number of the port, can see mamp/ settings/ ports]/ [Folder Type]
        private static readonly string ServerURL = "localhost:80/Worldbuilding";
        private static readonly string LoginScript = "Login.php";
        private static readonly string RegisterScript = "RegisterUser.php";
        private static readonly string SaveBuildingTypesScript = "SaveBuildingTypes.php";
        private static readonly string SavePlacedBuildingsScript = "SavePlacedBuildings.php";
        private static readonly string LoadSavedBuildingsScript = "LoadSavedBuildings.php";
        private static readonly string UpdateBuildingsScript = "UpdatePlacedBuildings.php";
        
        
        // Async task so we can run in the background without blocking the main thread
        public static async Task<bool> RegisterUser(string email, string username, string password) { 
            var registerURL = $"{ServerURL}/{RegisterScript}";
            
            // Send
            return (await SendPostRequest(registerURL, new Dictionary<string, string> {
                // The key is the name of the variable in the PHP script, the value is the actual value we want to send
                // So the key values need to be exactly the same as the ones we $_POST in the PHP script
                
                { "Email", email },
                { "Username", username },
                { "Password", password }
            })).success;
        }
        
        // Async tastk so we can run in the background without blocking the main thread
        public static async Task<DatabaseUser> LoginUser(string email, string password) {
            var loginURL = $"{ServerURL}/{LoginScript}";

            // Send
            var result = await SendPostRequest(loginURL, new Dictionary<string, string> {
                { "Email", email },
                { "Password", password }
            });

            if (!result.success) return null;

            // Success:

            // Parse the JSON response to a DatabaseUser object
            var user = JsonConvert.DeserializeObject<DatabaseUser>(result.returnMessage);
            return user;
        }
        
        // Returns true if the building types were successfully saved to the database
        public static async Task<bool> SaveBuildingTypes(string[] buildingTypes) {  
            // Construct the URL for the server-side script that handles saving building types
            var saveBuildingTypesURL = $"{ServerURL}/{SaveBuildingTypesScript}";

            // Serialize the array of buildingType names into a JSON string using Newtonsoft.Json
            // This JSON string will be sent to the server
            var jsonBuildingTypes = JsonConvert.SerializeObject(buildingTypes);

            // Create a dictionary to hold the data to be sent in the POST request from the PHP script
            // The dictionary has one key-value pair:
            // - Key: "BuildingTypes" (this is the key expected by your PHP script)
            // - Value: The JSON string containing the serialized building type names
            var data = new Dictionary<string, string> {
                { "BuildingTypes", jsonBuildingTypes }
            };

            // Send the POST request to the server and await the result
            var result = await SendPostRequest(saveBuildingTypesURL, data);

            // This will be true if the building types were saved successfully on the server, and false otherwise
            return result.success;
        }
        
        /// <summary>
        /// Publishes our saved buildings to the server
        /// </summary>
        /// <param name="jsonDataList">A list of JSON strings, each representing a building's data.</param>
        /// <returns>Was the operation successfull?</returns>
        public static async Task<bool> SavePlacedBuildings(List<string> jsonDataList) {
            // URL
            var saveBuildingDataURL = $"{ServerURL}/{SavePlacedBuildingsScript}";

            // Create a dictionary to hold the JSON data for each building
            // The keys are in the format "data0", "data1", "data2", etc., to match the PHP script's expectation
            var data = new Dictionary<string, string>();
            for (int i = 0; i < jsonDataList.Count; i++) {
                data.Add($"data{i}", jsonDataList[i]); // Add each JSON string with a unique key, so the PHP file can identify them
            }

            // Send the POST request to the server and await the response
            var result = await SendPostRequest(saveBuildingDataURL, data);

            // Successful or not?
            return result.success;
        }

        /// <summary>
        /// Prepares the URL for the UpdateBuildingsScript and converts the provided List of JSON strings
        /// to a dictionary with the keys "data0", "data1", "data2", etc. and the values being the JSON strings.
        /// This makes it easy to read from the PHP script.
        /// </summary>
        /// <param name="jsonDataList"></param>
        /// <returns></returns>
        public static async Task<bool> UpdateBuildings(List<string> jsonDataList) {
            // Destination URL of the PHP script in the htdocs folder
            var updateBuildingDataURL = $"{ServerURL}/{UpdateBuildingsScript}";
            // new Dict <string, string> to send the data to the server
            var data = new Dictionary<string, string>();
            
            // iterate through the list and add each JSON string with a unique key to the dictionary
            for (int i = 0; i < jsonDataList.Count; i++) {
                data.Add($"data{i}", jsonDataList[i]);
            }

            // Send the POST request to the server and await the response
            var result = await SendPostRequest(updateBuildingDataURL, data);
            // Whenever the result is successful, return true
            // Because this Method is async, we are returning whenever the result is successful
            return result.success;
        }
        
        // Load the saved buildings from the server
        public static async Task<List<GridBuildingData>> LoadSavedBuildings(string email) {
            // The URL to the server-side script that handles loading saved buildings.
            var loadSavedBuildingsURL = $"{ServerURL}/{LoadSavedBuildingsScript}";

            // Send a POST request to the server with the user's email as a parameter.
            // The server-side script uses this email to identify which user's buildings to load.
            var result = await SendPostRequest(loadSavedBuildingsURL, new Dictionary<string, string> {
                { "Email", email }
            });

            // If the request was not successful, return null.
            if (!result.success) return null;

            // Trim any leading or trailing whitespace from the server's response.
            result.returnMessage = result.returnMessage.Trim();

            try {
                // Deserialize the server's response from a JSON string to a list of BuildingDataFromServerRaw objects.
                // This is done using the Newtonsoft.Json library.
                // Without the Library, we would have to trim the response and split it by commas and colons and so on.
                var buildingsData = JsonConvert.DeserializeObject<List<BuildingDataFromServerRaw>>(result.returnMessage);

                // Convert the list of BuildingDataFromServerRaw objects to a list of Building objects.
                // This is done by selecting each BuildingDataFromServerRaw object in the list and creating a new Building object from it.
                // The BuildingType and gridPosition properties of the Building object are set based on the properties of the BuildingDataFromServerRaw object.
                return buildingsData.Select(b => new GridBuildingData {
                    Type = System.Enum.Parse<BuildingType>(b.buildingName),
                    GridPosition = new GridPosition(int.Parse(b.posX), int.Parse(b.posZ))
                }).ToList();
            } catch (System.Exception e) {
                // If an error occurs during the deserialization or conversion process, log the error message and return null.
                Debug.LogError("Error deserializing building data: " + e.Message);
                return null;
            }
        }
        
        // Helper class to store the Data provided by the Server in string format to further convert them later.
        [System.Serializable]
        private class BuildingDataFromServerRaw {
            public string buildingName;
            public string posX;
            public string posZ;
        }
        
        /// <summary>
        /// Communication between Server and Unity
        /// expects data to be sent via the POST method.
        /// </summary>
        /// <param name="url">The URL of the server-side script to send the request to.</param>
        /// <param name="data">A dictionary containing the data to be sent in the POST request. 
        /// The keys of the dictionary should match the parameter names expected by the server-side script.
        /// For example, if your PHP script expects a parameter named "Email", you would include a key-value pair like this:
        /// `{"Email", "your_email_address@example.com"}`
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task result is a tuple containing:
        /// - success: A boolean value indicating whether the request was successful.
        /// - returnMessage: The PHP Output like the Data we want to display in Unity or an Error</returns>
        private static async Task<(bool success, string returnMessage)> SendPostRequest(string url, Dictionary<string, string> data) {
            // Create a new UnityWebRequest object and configure it for a POST request to the specified URL
            using var request = UnityWebRequest.Post(url, data);

            // Send the web request
            request.SendWebRequest();

            // Wait for the request to complete
            while (!request.isDone) { 
                await Task.Delay(100); // 100 ms delay between checks
            }

            // Check if the request resulted in an error
            if (request.error != null  // UnityWebRequest error
                || !string.IsNullOrWhiteSpace(request.error) // Additional check for UnityWebRequest errors
                || HasErrorMessage(request.downloadHandler.text)) { // Check for custom error messages that we defined in our PHP script like "0" or "1"

                Debug.Log("<color=red>" + request.downloadHandler.text + "</color>");
                
                // Failed, return the error message
                return (false, request.downloadHandler.text);
            }

            Debug.Log("<color=green>" + request.downloadHandler.text + "</color>");
            
            // Succeeded, return the response message
            return (true, request.downloadHandler.text);
        }

        
        /// <summary>
        /// We use TryParse to check if our self written Error Messages in the php script occured. So if the response is a number, we know it's an error message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private static bool HasErrorMessage(string msg) => int.TryParse(msg, out var res);
    }
}