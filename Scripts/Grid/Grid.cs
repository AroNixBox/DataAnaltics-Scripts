using System;
using System.Collections.Generic;
using Grid.Building.Data;
using TMPro;
using UnityEngine;

namespace Grid {
    // Generic Base Grid Class to create a Grid with any type of Object.
    public class Grid<TGridObject> : IDisposable {
        // Invoked when building is placed, upgraded or deleted
        public event EventHandler<GridValueChangedEventArgs<GridBuildingData>> OnPlacedObjectChanged;
        
        // Stores all Buildings placed on the Grid, works like Einwohnermeldeamt
        private readonly Dictionary<Vector2Int, GridBuildingData> _currentBuildingsOnGrid = new ();
        public class GridValueChangedEventArgs<T> : EventArgs {
            public T Value;
            public Vector2Int CellCoordinates;
        }
    
        // Entire Grid width
        private readonly int _width;
        // Entire Grid height
        private readonly int _height;
        // Size of each cell
        private readonly float _cellSize;
        // Middle of the Grid
        private readonly Vector3 _originPosition;
    
        // 2D Array of the Grid
        private readonly TGridObject[,] _gridArray;

        // Should we spawn TMP Texts for Debugging?

        public Grid(int width, int height, float cellSize, Vector3 centerPosition, 
            Func<Grid<TGridObject>, int, int, TGridObject> createGridObject, bool showDebug) {
            // X
            _width = width;
            // Z
            _height = height;
            // Size of each cell
            _cellSize = cellSize;

            //Calculate the origin position
            _originPosition = centerPosition - new Vector3(width, 0, height) * cellSize * 0.5f;
        
            _gridArray = new TGridObject[width, height];
        
            // 2D Array of the Debug Text
            var debugTextArray = new TextMeshPro[width][];
            for (int index = 0; index < width; index++) {
                debugTextArray[index] = new TextMeshPro[height];
            }

            //Create the Grid Object with whatever type is passed in
            for(var x = 0; x < _gridArray.GetLength(0); x++) {
                for(var z = 0; z < _gridArray.GetLength(1); z++) {
                    _gridArray[x, z] = createGridObject(this, x, z);
                }
            }

            // Subscribe to the event to track which buildings are placed on the grid
            OnPlacedObjectChanged += RefreshPlacedBuilding;
        
            if(!showDebug) { return; }
        
            var parent = new GameObject("Visual Grid").transform;
        
            //Cycle through the first dimension of the array
            for(var x = 0; x < _gridArray.GetLength(0); x++) {
                //Cycle through the second dimension of the array
                for(int z = 0; z < _gridArray.GetLength(1); z++) {
                    debugTextArray[x][z] = UtilClass.CreateWorldText(
                        _gridArray[x, z]?.ToString(), parent, 
                        //Center the text in the middle of the cell
                        GetWorldPosition(x, z) + new Vector3(cellSize, 0,cellSize) * 0.5f, 
                        cellSize, 10, Color.black, TextAlignmentOptions.Center, 10);
                
                
                    //Vertical lines
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z +1), Color.white, Mathf.Infinity);
                
                    //Horizontal lines
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x +1, z), Color.white, Mathf.Infinity);
                }
            }
            //Horizontal outside line
            Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, Mathf.Infinity);
        
            //Vertical outside line
            Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, Mathf.Infinity);
        
            // Subscribe to the OnPlacedObjectChanged event
            OnPlacedObjectChanged += (_, eventArgs) => {
                // Change the debug text of a specific cell
                // In 3D y represents z (because Unity is z forward)
               debugTextArray[eventArgs.CellCoordinates.x][eventArgs.CellCoordinates.y].text = 
                   // GetDebugText either returns only the xz coords of that building or the building upgrade name and xz coords
                   GetDebugText(eventArgs.Value, _gridArray[eventArgs.CellCoordinates.x, eventArgs.CellCoordinates.y]);
            };
        }
        
        //Print the Coordinates and Type of current Building Upgrade if there is one on the Grid
        private string GetDebugText(GridBuildingData buildingData, TGridObject gridObject) {
            // No Building on this Cell, print the x,z coords only
            if (buildingData == null) {
                return gridObject.ToString();
            }

             // We have a building on this Cell, print the x,z coords and the Building Upgrade Type
            return $"{gridObject}\n{buildingData.Type.ToString()}";
        }
        
        // Refresh the Data of the Grid on a specific Cell (e.g. when a building is placed, upgraded or deleted)
        public void TriggerGridObjectChanged(GridBuildingData buildingData) {
            OnPlacedObjectChanged?.Invoke(this, new GridValueChangedEventArgs<GridBuildingData> {
                Value = buildingData,
                CellCoordinates = new Vector2Int(buildingData.GridPosition.x, buildingData.GridPosition.z)
            });
        }
        
        // Converts XZ position to World Position
        public Vector3 GetWorldPosition(int x, int z) {
            return new Vector3(x, 0, z) * _cellSize + _originPosition;
        }
        
        // Converts world position to a XZ position
        public void GetXZ(Vector3 worldPosition, out int x, out int z) {
            x = Mathf.FloorToInt((worldPosition - _originPosition).x / _cellSize);
            z = Mathf.FloorToInt((worldPosition - _originPosition).z / _cellSize);
        }
    
        // Get the Grid Object based on the XZ-Grid Position => (So World position needs to be converted before calling this)
        public TGridObject GetGridObject(int x, int z) {
            return IsWithinBounds(x, z) ? _gridArray[x, z] :
                //Value is Invalid, but throw no exception
                default(TGridObject);
        }
    
        //Get the value based on the world position (e.g. mouse hit ground position), no convert needed
        public TGridObject GetGridObject(Vector3 worldPosition) {
            GetXZ(worldPosition, out var x, out var z);
        
            return GetGridObject(x, z);
        }
    
        // Did we click on the Grid?
        public bool IsWithinBounds(int x, int z) {
            return x >= 0 && z >= 0 && x < _width && z < _height;
        }

        // Cell Size of the Cell
        public float GetCellSize() {
            return _cellSize;
        }
        
        // Returns Each Building Data of all Objects placed on the Grid
        public List<GridBuildingData> GetAllBuildingDataOnGrid() => new (_currentBuildingsOnGrid.Values);
        
        // Called when a building is placed, upgraded or deleted, informs the Grid via event.
        private void RefreshPlacedBuilding(object sender, GridValueChangedEventArgs<GridBuildingData> buildingData) {
            // Extract the placed object
            var building = buildingData.Value;

            if (building.Type is BuildingType.None) {
                // If the building is null, remove it from the current upgrades
                _currentBuildingsOnGrid.Remove(buildingData.CellCoordinates);
            } else {
                // Add the building information in the current buildings on the grid
                _currentBuildingsOnGrid[buildingData.CellCoordinates] = building;
            }
        }

        public void Dispose() { // Unsubscribe from the event
            OnPlacedObjectChanged -= RefreshPlacedBuilding;
        }
    }
}
