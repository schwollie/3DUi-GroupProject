using UnityEngine;
using UnityEditor;

public class ReplaceModelsWithPrefabs : MonoBehaviour
{
    [MenuItem("Tools/Replace Models with Prefabs")]
    static void Replace()
    {
        // Path to your prefabs folder (inside "Assets")
        string prefabFolderPath = "Assets/Prefabs/";

        foreach (GameObject go in Selection.gameObjects)
        {
            string name = go.name.Replace("(Clone)", "").Trim();

            string[] guids = AssetDatabase.FindAssets(name + " t:prefab", new[] { prefabFolderPath });

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
                    newGO.name = go.name;

                    Undo.RegisterCreatedObjectUndo(newGO, "Replace with Prefab");
                    Undo.DestroyObjectImmediate(go);
                }
            }
            else
            {
                Debug.LogWarning($"No matching prefab found for {name}");
            }
        }
    }
}