using UnityEngine;

namespace Grid.Building {
        // When the grid is created, each cell is initialized with a GridObject
        public class GridObject {
            private readonly Grid<GridObject> _grid;
            private readonly int _x;
            private readonly int _z;
            private PlacedBuilding _placedObject;
        
            // locally store this x, z and the grid reference
            public GridObject(Grid<GridObject> grid, int x, int z) {
                _grid = grid;
                _x = x;
                _z = z;
            }
            
            // Assign a building to this cell
            public void SetPlacedObject(PlacedBuilding placedObject) {
                _placedObject = placedObject;
                // Notify the Grid, needs to be after the Upgrade itself
            }
        
            // Center of the Cell, maybe to place world space UI
            public Vector3 GetCellCenterWorldPosition() {
                Vector3 worldPosition = _grid.GetWorldPosition(_x, _z);
                Vector3 tileCenterPosition = new Vector3(worldPosition.x + _grid.GetCellSize() / 2, 0, worldPosition.z + _grid.GetCellSize() / 2);
                return tileCenterPosition;
            }
            //  Returns the Building on this Cell
            public PlacedBuilding GetPlacedBuilding() {
                return _placedObject;
            }
        
            // When deleting the building, Call this from the building this cell is holding
            public void ClearPlacedObject() {
                _placedObject = null;
            }
            
            // Override the ToString method to print the x, z of the cell whenever it is called
            public override string ToString() {
                return _x + ", " + _z + "\n";
            }
        }
}