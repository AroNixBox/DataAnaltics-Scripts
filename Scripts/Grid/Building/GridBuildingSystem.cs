using System;
using System.Collections.Generic;
using System.Linq;
using Grid.Building.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Grid.Building {
    public class GridBuildingSystem : MonoBehaviour {
        [BoxGroup("Debug Grid")]
        [HorizontalGroup("Debug Grid/Split", LabelWidth = 100)] 
        [SerializeField] private bool drawDebugGrid;
    
        [BoxGroup("Debug Grid")]
        [ShowIf("drawDebugGrid")]
        [SerializeField] private Color debugGridColor = Color.red;
    
        [Header("References")]
        [SerializeField] private PlacedBuilding buildingParent;
    
        [Header("Values")]
        [Tooltip("Cell size divided by Tiles of Building equals perfect fit")]
        [SerializeField, Range(1,15)] private int cellSize = 2;
        // Y = Z, because Y is up and we use z for the grid-"height"
        [SerializeField] private Vector2 gridOffset;
    
        [Tooltip("Four Corners of the Grid")]
        [SerializeField] private Transform[] boundsTransforms;
        [Tooltip("Building Types")]
        [SerializeField] private List<PlacedBuildingTypeSo> buildingObjectTypeSos;
    
        // Current Building to be placed
        private PlacedBuildingTypeSo _placedBuildingTypeSo;
        // Index of the currently selected building, which is previewed on that cell but not placed yet
        private int _currentSelectedBuildingGhostIndex;
        // Grid, where each Cell is a GridObject
        private Grid<GridObject> _grid;
        private Camera _mainCamera;
    
        // Weather the Ghost is red or green
        public class CanBuildChangedEventArgs : EventArgs {
            public bool CanBuild { get; }

            public CanBuildChangedEventArgs(bool canBuild) {
                CanBuild = canBuild;
            }
        }
    
        // Weather the Ghost is red or green
        public event EventHandler<CanBuildChangedEventArgs> OnCanBuildChanged;
        
        // Mouse outside the Grid
        public event Action OnStoppedBuilding; 
        
        // The previous GridObject, so we can check if we clicked the same cell again
        private GridObject _previousGridObject;
    
        // Despawns the old ghost and spawns a new one with the new position and the new PlacedBuildingTypeSo
        public event Action<Vector3, PlacedBuildingTypeSo> OnSelectChanged;
    
        private void Awake() {
            _mainCamera = Camera.main;
        
            // Check if there are 4 corners
            if(boundsTransforms.Length != 4) {
                Debug.LogError("Need 4 Corners");
                return;
            }
        
            // Create the Grid
            CreateGrid();

            // Default the first SO to be the default selected one
            _placedBuildingTypeSo = buildingObjectTypeSos[0];
        }

        private void Start() {
            var placedObjectTypeSos = new List<PlacedBuildingTypeSo>();
            
            // Iterate through all buildings that the player has in its "inventory"
            foreach(var element in buildingObjectTypeSos) {
                // Create Instances of the SO, so if we want to change values in runtime we dont mess up the original SO
                PlacedBuildingTypeSo placedBuildingTypeSo = ScriptableObject.CreateInstance<PlacedBuildingTypeSo>();
                
                // Copy the values of the original SO to the instance
                placedBuildingTypeSo.BuildingInformation = element.BuildingInformation;
                
                // Add the instance to the list
                placedObjectTypeSos.Add(placedBuildingTypeSo);
            }
            
            // Clear the original List
            buildingObjectTypeSos.Clear();
            // Replace the original List with the new List of Instances
            buildingObjectTypeSos.AddRange(placedObjectTypeSos);
        }

        // Create the Grid based on the 4 Corners
        private void CreateGrid() {
            // Create min and max values for X and Z based on the 4 Corners
            float minX = boundsTransforms.Min(t => t.position.x);
            float maxX = boundsTransforms.Max(t => t.position.x);
            float minZ = boundsTransforms.Min(t => t.position.z);
            float maxZ = boundsTransforms.Max(t => t.position.z);

            // Calculate the width and height of the grid
            float gridWidth = maxX - minX;
            float gridHeight = maxZ - minZ;

            // Center position of the grid
            Vector3 centerPosition = new Vector3((minX + maxX) / 2, 0, (minZ + maxZ) / 2);

            // Calculate the number of cells that can fit in the width and height of the grid
            int cellCountWidth = Mathf.FloorToInt(gridWidth / cellSize);
            int cellCountHeight = Mathf.FloorToInt(gridHeight / cellSize);

            // Create the Grid
            _grid = new Grid<GridObject>(cellCountWidth, cellCountHeight, cellSize, centerPosition + new Vector3(gridOffset.x, 0, gridOffset.y), 
                (g, x, z) => new GridObject(g, x, z), drawDebugGrid);
        }
        #region Cell
        
        private PlacedBuilding _buildingToSwap;
        private void Update() {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) { // Did we click on the UI?
                // This is necessary, because we converet every click to a world position and then to a grid position
                return; // If we clicked on the UI, no action should be taken
            } 
            
            var mouseWorldPosition = UtilClass.GetMouseWorldPosition(_mainCamera);
            
            // Convert it to XY position on the Grid
            _grid.GetXZ(mouseWorldPosition, out var x, out var z);
            
            var gridObject = _grid.GetGridObject(x, z);
            
            // Check if the middle mouse button was pressed down
            if (Input.GetMouseButtonDown(2)) {
                // Select a Cell as "Drag" and take the BuildingTypeSO from the Building that was selected
                
                
                // If the Cell isnt initialized correctly, return
                if (gridObject == null) { return; }
            
                // Try Selecting a Building to swap, null if there is none
                _buildingToSwap = gridObject.GetPlacedBuilding();
            } else if (Input.GetMouseButtonUp(2)) {
                // If the buildingToSwap is null, means we didnt select a building to swap
                if(_buildingToSwap == null) { return; }

                // Try replace the buildings position at a new position
                if (PlaceSelectedBuilding(x, z, gridObject, _buildingToSwap.GetPlacedBuildingTypeSo(), _buildingToSwap.buildingDataFromLastServerSave)) {
                    // Delete the old building, remove it from the server list
                    _buildingToSwap.ChangeBuildingPosition(_grid);
                    
                    // Null the buildingToSwapSo, because then we can keep going with the normal Hovering Ghost.
                    _buildingToSwap = null;
                }
                
                
                // Return, because after creating a new building, we dont want to do anything else
                return;
            }
            
            // Normally Select a Cell with the current selected BuildingTypeSO from the list
            var buildingTypeSo = _buildingToSwap?.GetPlacedBuildingTypeSo() ?? _placedBuildingTypeSo;
            
            SelectCell(x, z, gridObject, buildingTypeSo);
            
            // Only allow one at the following actions per frame
            if (Input.GetMouseButtonDown(0)) {
                PlaceSelectedBuilding(x, z, gridObject, _placedBuildingTypeSo, null);
            } else if (Input.GetMouseButtonDown(1)) {
                SwapOutBuildingWithNextOne(gridObject);
            } else {
                HandleBuildingSelectionScroll(x, z);
            }
        }

        // Tries selecting a Cell ( Select = Hover )
        private void SelectCell(int x, int z, GridObject gridObject, PlacedBuildingTypeSo placedBuildingTypeSo) {
            // The same Cell was clicked again
            if (_previousGridObject == gridObject) {
                return;
            }
            
            // Another Cell was clicked, update the previous one
            _previousGridObject = gridObject;
            
            // Did we hit a Cell?
            if (gridObject != null) {
                // Get the Building on the Cell
                var placedObject = gridObject.GetPlacedBuilding();
                if (placedObject != null) { // If there is a building on the Cell
                    
                    OnStoppedBuilding?.Invoke();
                    // TODO: Maybe can select the building in the future
                    return;
                }
                // No Building on the Cell, so we can continue
            } else { // Hovered outside the Grid
                // This gets only called once, since we set the previousGridObject to null, in the second iteration it will be the same as the current one (null)
                OnStoppedBuilding?.Invoke();
                Debug.Log("Grid not initialized correctly or clicked outside the grid");
                return;
            }
            
            // Convert the XZ position to World Position, so we know where to spawn the Building Ghost
            Vector3 worldPosition = _grid.GetWorldPosition(x, z);
            
            // Spawns a new Building Ghost or updates the current ones position
            OnSelectChanged?.Invoke(worldPosition, placedBuildingTypeSo);
            
            // The color of the Ghost shall only be representing the build status of the current selected building
            bool canBuild = IsCellValid(x, z);
            
            // Update the Color of the Ghost to represent the build status or destroy the Ghost if we can't build there 
            if (canBuild) {
                OnCanBuildChanged?.Invoke(this, new CanBuildChangedEventArgs(true));
            } else {
                OnStoppedBuilding?.Invoke();
            }
            
        }
        
        // Tells the Building on the Cell to swap out the current Building with the next one in the list
        private void SwapOutBuildingWithNextOne(GridObject gridObject) {
            // If the Cell isnt initialized correctly, return
            if (gridObject == null) { return; }
            
            var placedBuilding = gridObject.GetPlacedBuilding();
            // If there is a building on the Cell, we can now swap it out, return
            if (placedBuilding == null) { return; }
                
            var buildingType = gridObject.GetPlacedBuilding().GetBuildingType(); // Can not be null
            
            var buildingTypeSo = GetBuildingTypeSo(buildingType);
                
            // If there is an error with the BuildingTypeSO, return
            if (buildingTypeSo == null) { return; }
                
            // Find the BuildingTypeSO in the BuildingTypes List and replace it with the next one
            var nextIndex = buildingObjectTypeSos.IndexOf(buildingTypeSo) + 1;
            if (nextIndex >= buildingObjectTypeSos.Count) { // Out of bounds: wrap around to the first item
                nextIndex = 0;
            }
                
            // Replace the building with the next one in the list
            placedBuilding.ReplaceBuilding(buildingObjectTypeSos[nextIndex]);
        }
        
        // Check if we can build at this grid position
        private bool IsCellValid(int x, int z) {
            // Is the x, z position out of bounds?
            if (!_grid.IsWithinBounds(x, z)) {
                return false;
            }
    
            // Is the building type null? - Cell is not valid if the building type is null
            if (_grid.GetGridObject(x, z).GetPlacedBuilding() != null) {
                return false;
            }
            // TODO: Check if the ground under this tile is buildable => Even better do that foreach Cell on Start and only query it here.
    
            return true;
        }
    
        #endregion

        #region Building Buildings
        // Call when Player clicks on the grid
        private bool PlaceSelectedBuilding(int x, int z, GridObject gridObject, PlacedBuildingTypeSo buildingTypeToPlace, GridBuildingData copiedServerDataFromOldPosition) {
            // Did we hit a Cell?
            if (gridObject != null) {
                // Get the Building on the Cell
                var placedObject = gridObject.GetPlacedBuilding();
                if (placedObject != null) { // If there is a building on the Cell
                     
                    // NO OP
                    // TODO: Maybe can select the building in the future
                    return false;
                }

                // No Building on the Cell, so we can continue
            }
            else { // Clicked outside the Grid
                Debug.Log("Grid not initialized correctly or clicked outside the grid");
                return false;
            }
            
            // -> If we get here, we clicked on a cell with no building on it
        
            // Instantiate the Building
            CreateBuilding(buildingTypeToPlace, x, z, copiedServerDataFromOldPosition);
            
            // Inform the Ghost that we Created a Building
            OnCanBuildChanged?.Invoke(this, new CanBuildChangedEventArgs(false));
            return true;
        }

        // Call this when the player clicks or when loading from a save
        public PlacedBuilding CreateBuilding(PlacedBuildingTypeSo buildingTypeSo, int x, int z, GridBuildingData copiedServerDataFromOldPosition) {
            // Instantiate the Parent of the Building
            Transform placedBuildingParent = Instantiate(buildingParent.transform, _grid.GetWorldPosition(x, z), Quaternion.identity);
            // Inspector Type
            placedBuildingParent.name = buildingTypeSo.BuildingInformation.Data.Type.ToString();
            
            // Get the buildingParents PlacedBuilding Component, to initialize the building
            var placedBuilding = placedBuildingParent.GetComponent<PlacedBuilding>();
            
            _grid.GetGridObject(x, z).SetPlacedObject(placedBuilding);
        
            // Instantiate the Building Mesh Visual (No colliders, no Scripts attached)
            Instantiate(buildingTypeSo.BuildingInformation.Prefab, placedBuildingParent.position, Quaternion.identity, placedBuildingParent);
            
            var buildingPositionOnGrid = new Vector2Int(x, z);
            // This has to happen after the building Visual is Instantiated, because we animate the Visual
            // Insert the copiedServerData, this is only not null if the object was just moved.
            var buildingData = placedBuilding.Init(buildingPositionOnGrid, buildingTypeSo, copiedServerDataFromOldPosition);
            
            // Notify the Grid, needs to be after the Upgrade itself
            _grid.TriggerGridObjectChanged(buildingData);
            
            return placedBuilding;
        }
        
        // Scroll through the Building Types
        private void HandleBuildingSelectionScroll(int x, int z) {
            // Get the vertical scroll delta: positive for scroll up, negative for scroll down
            float scrollDelta = Input.mouseScrollDelta.y;

            // Check if there was any scroll action
            if (scrollDelta == 0 || buildingObjectTypeSos.Count <= 0) {
                return;
            }
           
            if (scrollDelta > 0) {  // Scroll up: increment the index to select the next building type
                _currentSelectedBuildingGhostIndex++;
                if (_currentSelectedBuildingGhostIndex >= buildingObjectTypeSos.Count) { // Out of bounds: wrap around to the first item
                    _currentSelectedBuildingGhostIndex = 0;
                }
            } else if (scrollDelta < 0) { // Scroll down: decrement the index to select the previous building type
                _currentSelectedBuildingGhostIndex--;
                if (_currentSelectedBuildingGhostIndex < 0) { // Out of bounds: wrap around to the last item
                    _currentSelectedBuildingGhostIndex = buildingObjectTypeSos.Count - 1;
                }
            }
            
            // Update the selected building type based on the new index
            _placedBuildingTypeSo = buildingObjectTypeSos[_currentSelectedBuildingGhostIndex];
            // Trigger any necessary updates, such as updating the building ghost preview
            var worldPosition = _grid.GetWorldPosition(x, z);
            // Trigger Updating the Building Ghost Prefab
            OnSelectChanged?.Invoke(worldPosition, _placedBuildingTypeSo);
            // Trigger Updating the Building Ghost Color based on the build status
            OnCanBuildChanged?.Invoke(this, new CanBuildChangedEventArgs(IsCellValid(x, z)));
        }
        #endregion
        
        // Helper Method to find the BuildingTypeSo with the given Type
        public PlacedBuildingTypeSo GetBuildingTypeSo(BuildingType buildingType) {
            return buildingObjectTypeSos.Find(x => x.BuildingInformation.Data.Type == buildingType);
        }
        
        #region Gizmos

        private void OnDrawGizmos() {
            // Check if we should draw the debug grid, if the application is running, or if the boundsTransforms array is not correctly set up
            if (!drawDebugGrid || Application.isPlaying || boundsTransforms == null || boundsTransforms.Length != 4) {
                return; // Exit the method if any of the conditions are met
            }

            // Calculate the center position of the grid by averaging the positions of the four corner transforms
            Vector3 centerPosition = (boundsTransforms[0].position + boundsTransforms[1].position + boundsTransforms[2].position + boundsTransforms[3].position) / 4;

            // Calculate the width of the grid as the distance between the first two transforms
            float gridWidth = Vector3.Distance(boundsTransforms[0].position, boundsTransforms[1].position);
            // Calculate the height of the grid as the distance between the first and the third transforms
            float gridHeight = Vector3.Distance(boundsTransforms[0].position, boundsTransforms[2].position);

            // Determine the number of cells that can fit in the width and height of the grid
            int cellCountWidth = Mathf.FloorToInt(gridWidth / cellSize);
            int cellCountHeight = Mathf.FloorToInt(gridHeight / cellSize);

            // Set the color for drawing the Gizmos
            Gizmos.color = debugGridColor;

            // Loop through each cell in the grid
            for (int x = 0; x < cellCountWidth; x++) {
                for (int z = 0; z < cellCountHeight; z++) {
                    // Calculate the center position of each cell, adjusting for the grid's center position and offset
                    Vector3 cellCenter = centerPosition + new Vector3((x - cellCountWidth * 0.5f) * cellSize, 0, (z - cellCountHeight * 0.5f) * cellSize);
                    // Draw a wireframe cube at the calculated center position of each cell, applying the grid offset
                    Gizmos.DrawWireCube(cellCenter + new Vector3(gridOffset.x, 0, gridOffset.y), new Vector3(cellSize, 0, cellSize));
                }
            }
        }

        
        // Helper Method to print all Building Names on the Grid to the Console
        [Button("Print All Placed Building Names")]
        private void PrintAllPlacedBuildingNames() {
            var buildingNames = 
                _grid.GetAllBuildingDataOnGrid().
                    Select(obj => obj.Type.ToString()).
                    ToList();

            Utils.ColorfulDebug.LogWithRandomColor(buildingNames);
        }
        #endregion

        public Grid<GridObject> GetGrid() {
            return _grid;
        }
        
        private void OnDestroy() {
            // Call Dispose on the Grid to remove all the Events
            _grid.Dispose();
        }
    }
}