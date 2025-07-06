using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

public class ReplaceModelsWithPrefabs : MonoBehaviour
{
    [MenuItem("Tools/Replace Models with Prefabs (Auto Match)")]
    static void Replace()
    {
        // Path to your prefabs folder (adjust as needed)
        string prefabFolderPath = "Assets/Prefabs/";

        foreach (GameObject go in Selection.gameObjects)
        {
            // Strip trailing "(n)" using regex
            string baseName = Regex.Replace(go.name, @"\s*\(\d+\)$", "");

            // Search for a matching prefab
            string[] guids = AssetDatabase.FindAssets(baseName + " t:prefab", new[] { prefabFolderPath });

            if (guids.Length > 0)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab != null)
                {
                    GameObject newGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.scene);

                    newGO.transform.position = go.transform.position;
                    newGO.transform.rotation = go.transform.rotation;
                    newGO.transform.localScale = go.transform.localScale;
                    newGO.transform.parent = go.transform.parent;

                    newGO.name = go.name; // Keep original name with number if needed

                    Undo.RegisterCreatedObjectUndo(newGO, "Replace with Prefab");
                    Undo.DestroyObjectImmediate(go);
                }
            }
            else
            {
                Debug.LogWarning($"No prefab found for: {baseName}");
            }
        }
    }
}