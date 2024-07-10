using Sirenix.OdinInspector;
using UnityEngine;

namespace Grid.Building.Data {
    // Data for a Building State/ Upgrade
    [System.Serializable]
    public class BuildingData {
        [HideLabel] public GridBuildingData Data = new();
        
        [field: SerializeField] public GameObject Prefab { get; set; }
    }
    // This enum is used for all Buildings that are buildable
    // Add more types if needed
    public enum BuildingType {
        House,
        Tower,
        None
    }
}