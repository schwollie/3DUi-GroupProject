using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Collider))]
public class ScaleBowl : MonoBehaviour
{
    public GameObject Anchor;
    public float yRotDeg = 15;


    // Keep the bowl at the anchor's position and upright every frame
    private void LateUpdate()
    {
        if (Anchor != null)
        {
            transform.position = Anchor.transform.position;
            transform.rotation = Quaternion.Euler(0, yRotDeg, 0);
        }
    }

    private void OnValidate()
    {
        if (Anchor != null)
        {
            transform.position = Anchor.transform.position;
            transform.rotation = Quaternion.Euler(0, yRotDeg, 0);
        }
    }
}