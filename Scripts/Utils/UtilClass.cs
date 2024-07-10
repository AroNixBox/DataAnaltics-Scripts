using TMPro;
using UnityEngine;

public class UtilClass
{
    public static TextMeshPro CreateWorldText(string text, Transform parent, Vector3 localPosition, float cellSize, float fontSize, Color color, TextAlignmentOptions textAlignment, int sortingOrder = 0) {
        GameObject textGameObject = new GameObject("World Text", typeof(TextMeshPro));
        Transform transform = textGameObject.transform;
        transform.SetParent(parent, false);
        transform.localPosition = localPosition;
        TextMeshPro textMeshPro = textGameObject.GetComponent<TextMeshPro>();
        textMeshPro.alignment = textAlignment;
        textMeshPro.text = text;
        textMeshPro.color = color;
        textMeshPro.enableWordWrapping = false;

        // Set the size of the TextMeshPro element to match the cell size
        textMeshPro.rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
        
        // Set font size based on cell size
        textMeshPro.fontSize = fontSize;
        
        textMeshPro.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
        
        textGameObject.transform.forward = -Vector3.up;
        
        return textMeshPro;
    }

    public static Vector3 GetMouseWorldPosition(Camera mainCamera) {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out var rayLength)) {
            Vector3 point = ray.GetPoint(rayLength);
            point.y = 0;
            return point;
        }
        return Vector3.zero;
    }
}