using Sirenix.OdinInspector;
using UnityEngine;

namespace Grid.Building.Data {
    // Data Container to Create Buildings linked with Prefabs and Upgrades
    [CreateAssetMenu(menuName = "Placed Object Type")]
    public class PlacedBuildingTypeSo : ScriptableObject {
        [BoxGroup("General")]
        [ShowInInspector]
        [HideLabel]
        public BuildingData BuildingInformation;
        
        // TODO: Add Upgrades ----
        // TODO: Add Size if Building occupies more than 1x1
    }
}