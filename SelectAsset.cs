using UnityEditor;
using UnityEngine;

public static class SelectAsset
{
    public static void SelectAssetInProject(string assetPath)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (asset != null)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
        else
        {
            Debug.LogError($"Asset not found at path: {assetPath}");
        }
    }
}