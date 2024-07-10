using Grid.Building.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Grid.Building {
    public class BuildingGhost : MonoBehaviour {
        // We are acccessing the events of the GridBuildingSystem
        [SerializeField] private GridBuildingSystem gridBuildingSystem;
    
        // Layer of the Ghost and it Childs, can not be LayerMask, multiple layers are not supported
        [SerializeField] [ValueDropdown("GetLayers")] private int selectedLayer;
        [SerializeField] private Material validBuildColor;
        [SerializeField] private Material invalidBuildColor;
    
        // Current Visual Ghost
        private Transform _visual;
    
        // Ghost renderer to set the color
        private MeshRenderer[] _childMeshRenderers;
        
        private Material _canBuildMaterial;

        private void Start() {
            gridBuildingSystem.OnSelectChanged += OnBuildingSystemInstanceSelectChanged;
            gridBuildingSystem.OnCanBuildChanged += OnCanBuildChanged;
            gridBuildingSystem.OnStoppedBuilding += DespawnGhost;
        }

        private void OnDestroy() {
            gridBuildingSystem.OnSelectChanged -= OnBuildingSystemInstanceSelectChanged;
            gridBuildingSystem.OnCanBuildChanged -= OnCanBuildChanged;
            gridBuildingSystem.OnStoppedBuilding -= DespawnGhost;
        }

        /// <summary>
        /// Switches the Color of the Ghost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnCanBuildChanged(object sender, GridBuildingSystem.CanBuildChangedEventArgs args) {
            if(_visual == null) return;
            
            _canBuildMaterial = args.CanBuild ? validBuildColor : invalidBuildColor;

            foreach (var meshRenderer in _childMeshRenderers) {
                meshRenderer.material = _canBuildMaterial;
            }
        }
    
        /// <summary>
        /// Destroys the old Ghost
        /// </summary>
        private void DespawnGhost() {
            if(_visual != null) {
                Destroy(_visual.gameObject);
                _visual = null;
            }
        }

        /// <summary>
        /// Despawns the old ghost and spawns a new one with the new position and the new PlacedObjectTypeSO
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <param name="placedObjectTypeSo"></param>
        private void OnBuildingSystemInstanceSelectChanged(Vector3 targetPosition, PlacedBuildingTypeSo placedObjectTypeSo) {
            // SO should never be null
            if (!placedObjectTypeSo) {
                Debug.LogError("PlacedObjectTypeSO is null");
                return;
            }
        
            // Destroy the old Ghost
            DespawnGhost();
        
            // Spawn the new Ghost in the correct position with the correct color
            _visual = Instantiate(placedObjectTypeSo.BuildingInformation.Prefab, targetPosition, Quaternion.identity).transform;
            
            // Set the Material of the ghost!
            _childMeshRenderers = _visual.GetComponentsInChildren<MeshRenderer>();
            
            _visual.parent = transform;
        
            // Set the Layer of the Ghost
            SetLayerRecursively(_visual.gameObject, selectedLayer);
        }

        /// <summary>
        /// Recursive Layer setting foreach child of the passed GO
        /// </summary>
        /// <param name="visualGameObject"></param>
        /// <param name="layer"></param>
        private void SetLayerRecursively(GameObject visualGameObject, int layer) {
            foreach (Transform child in visualGameObject.transform) {
                SetLayerRecursively(child.gameObject, layer);
            }
            visualGameObject.layer = layer;
        }
        /// <summary>
        /// Returns a Dropdown List of all Layers
        /// </summary>
        /// <returns></returns>
        private static ValueDropdownList<int> GetLayers() {
            ValueDropdownList<int> layers = new ValueDropdownList<int>();
            for (int i = 0; i < 32; i++) {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName)) {
                    layers.Add(layerName, i);
                }
            }
            return layers;
        }
    }
}