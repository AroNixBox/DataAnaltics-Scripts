using System.Collections;
using Grid.Building.Data;
using MySQL;
using UnityEngine;

namespace Grid.Building {
    public class PlacedBuilding : MonoBehaviour {
        // Data of this Building (X,Z Position and Type) => Change when Upgrading or Moving
        private GridBuildingData PlacedBuildingData { get; } = new ();
    
        // Which DataContainer is this Building from? So we know - if we want to upgrade it
        private PlacedBuildingTypeSo _placedBuildingTypeSo;
        
        // Data filled, when the Server Initialized the Building on Load.
        public GridBuildingData buildingDataFromLastServerSave = new();
        
        // Call when placing the building
        // Spawns it and sets the Data
        public GridBuildingData Init(Vector2Int gridPosition, PlacedBuildingTypeSo placedBuildingTypeSo, GridBuildingData copiedServerDataFromOldPosition) {
            _placedBuildingTypeSo = placedBuildingTypeSo;
        
            // X,Z Coords and Type of the Building
            PlacedBuildingData.Type = placedBuildingTypeSo.BuildingInformation.Data.Type;
            PlacedBuildingData.GridPosition = new GridPosition(gridPosition.x, gridPosition.y);
            
            if (copiedServerDataFromOldPosition != null && copiedServerDataFromOldPosition.GridPosition != null) { // Building was Moved
                buildingDataFromLastServerSave.GridPosition = new GridPosition(copiedServerDataFromOldPosition.GridPosition.x, copiedServerDataFromOldPosition.GridPosition.z);
                buildingDataFromLastServerSave.Type = copiedServerDataFromOldPosition.Type;
                
                // Readd self again to the server list
                BuildingSaveManager.Instance.AddSelfToServerBuildingList(this);
            }
        
            var child = GetBuildingVisual();
        
            StartCoroutine(BounceAnimation(child,0.1f, 0.1f, 0.1f));
            
            return PlacedBuildingData;
        }
        
        // Call when loading the building from the server
        // Sets the ServerData on this Building
        public void SetBuildingServerData(GridBuildingData buildingData) {
            buildingDataFromLastServerSave.GridPosition = new GridPosition(buildingData.GridPosition.x, buildingData.GridPosition.z);
            buildingDataFromLastServerSave.Type = buildingData.Type;
        }
        
        // Only Call if this building was loaded from the server (saved already)
        // Call from SaveManager to get the Building Data and the InitialData
        // So how it was when it was loaded from the server and how it is now
        public (GridBuildingData, GridBuildingData) GetBuildingDataFromLoadedBuilding() {
            return (buildingDataFromLastServerSave, PlacedBuildingData);
        }
        
        // Visual BuildingAnimation
        private IEnumerator BounceAnimation(Transform child, float duration, float height, float width) {
            float elapsedTime = 0;
            Vector3 startingScale = child.localScale;

            while (elapsedTime < duration) {
                float bounceY = Mathf.Sin(elapsedTime / duration * Mathf.PI) * height;
                float bounceX = Mathf.Sin(elapsedTime / duration * Mathf.PI) * width;
                child.localScale = startingScale + new Vector3(bounceX, bounceY, 0);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            child.localScale = startingScale;
        }
        
        // TODO
        // Call this when Moving the building
        // IMPORTANT: This is the updated value that will be saved in the Database when the player saves the game
        public void MoveBuilding(Vector2Int newOrigin) {
            PlacedBuildingData.GridPosition = new GridPosition(newOrigin.x, newOrigin.y);
        }

        // TODO
        // Call this when Replacing the building
        // IMPORTANT: This is the updated value that will be saved in the Database when the player saves the game
        public void ReplaceBuilding(PlacedBuildingTypeSo buildingTypeSo) {
            // Get rid of the old visual
            var child = transform.GetChild(0);
            Destroy(child.gameObject);
            
            // New Building:
            // Replace the Type of the old building with the new one
            PlacedBuildingData.Type = buildingTypeSo.BuildingInformation.Data.Type;
            
            // Instantiate the new building, at this position, child it to this
            var newBuildingVisual = Instantiate(buildingTypeSo.BuildingInformation.Prefab, transform.position, Quaternion.identity, transform);
            StartCoroutine(BounceAnimation(newBuildingVisual.transform, 0.1f, 0.1f, 0.1f));
        }
        
        // Call this to find out what type of building this is
        public BuildingType GetBuildingType() {
            return PlacedBuildingData.Type;
        }
        
        public PlacedBuildingTypeSo GetPlacedBuildingTypeSo() {
            return _placedBuildingTypeSo;
        }
        
        //  Returns the visual of the building
        private Transform GetBuildingVisual() {
            var childCount = transform.childCount;
            if(childCount > 1) {
                Debug.LogError("More than one child found in PlacedBuilding, should ONLY be visual, else update this method.");
            }
        
            return transform.GetChild(0);
        }
        
        // Call this when deleting the building (right click it e.g.)
        public void ChangeBuildingPosition(Grid<GridObject> grid) {
            // Remove this building from the server list
            BuildingSaveManager.Instance.RemoveSelfFromServerBuildingList(this);
            grid.TriggerGridObjectChanged(new GridBuildingData {
                GridPosition = PlacedBuildingData.GridPosition, 
                Type = BuildingType.None
            });
            Destroy(gameObject);
        }
    }
}