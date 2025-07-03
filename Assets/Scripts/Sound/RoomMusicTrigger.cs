using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomMusicTrigger : MonoBehaviour
{
    [Header("Room Music Settings")] [SerializeField]
    private SoundDefinition roomMusic;

    [Range(0f, 1f)] [SerializeField] private float roomMusicVolume = 1f;

    [SerializeField] private bool fadeOutAmbientMusic = true;

    [Header("Trigger Settings")] [SerializeField]
    private string playerTag = "Player";

    private bool _playerInRoom;
    private static RoomMusicTrigger _currentActiveRoom;

    private void Awake()
    {
        // Ensure the collider is set as a trigger
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !_playerInRoom)
        {
            _playerInRoom = true;
            EnterRoom();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && _playerInRoom)
        {
            _playerInRoom = false;
            ExitRoom();
        }
    }

    private void EnterRoom()
    {
        // If there's another active room, it should handle its exit
        if (_currentActiveRoom != null && _currentActiveRoom != this) _currentActiveRoom._playerInRoom = false;

        _currentActiveRoom = this;

        if (AudioManager.Instance != null)
            AudioManager.Instance.TransitionToRoomMusic(roomMusic, roomMusicVolume, fadeOutAmbientMusic);
    }

    private void ExitRoom()
    {
        // Only transition back if this is the currently active room
        if (_currentActiveRoom == this)
        {
            _currentActiveRoom = null;

            if (AudioManager.Instance != null) AudioManager.Instance.TransitionBackToAmbient();
        }
    }

    private void OnDrawGizmos()
    {
        // Visualize the trigger area in the editor
        var col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = _playerInRoom ? Color.green : Color.yellow;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);

            if (col is BoxCollider box)
            {
                var oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            }
        }
    }
}