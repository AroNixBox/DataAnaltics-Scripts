using Grid.Building.Data;
using UnityEngine;

namespace Grid {
    // TODO Instead of Vector2Int we can use this class, because we dont need the y value
    // Helper class to store the x, z position of a building, ensures the values have a set number
    [System.Serializable]
    public class GridPosition {
        public int x;
        public int z;

        public GridPosition(int gridPositionX, int gridPositionZ) {
            x = gridPositionX;
            z = gridPositionZ;
        }
    }
    
    // Data from a Building that is placed on a grid
    [System.Serializable]
    public class GridBuildingData {
        // Which Building is placed
        [field: SerializeField] public BuildingType Type { get; set; }
        // Where is it placed (Set from the Init Method when Clicking or when loading from the Database
        public GridPosition GridPosition { get; set; }
    }
}
