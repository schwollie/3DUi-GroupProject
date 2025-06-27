using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Collider))]
public class ScaleBowl : MonoBehaviour
{
    public GameObject Anchor;


    // Keep the bowl at the anchor's position and upright every frame
    private void LateUpdate()
    {
        if (Anchor != null)
        {
            transform.position = Anchor.transform.position;
            transform.rotation = Quaternion.identity; // Always upright
        }
    }

    private void OnValidate()
    {
        if (Anchor != null)
        {
            transform.position = Anchor.transform.position;
            transform.rotation = Quaternion.identity;
        }
    }
}