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

    // New field to track if player was in room before disabling
    private bool _wasPlayerInRoomBeforeDisable;

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

    // New method to enable/disable the room music trigger
    public void SetEnabled(bool enable)
    {
        if (enable)
        {
            // Enabling the trigger
            enabled = true;

            // If player was in room before disabling, restore the music
            if (_wasPlayerInRoomBeforeDisable)
            {
                // Check if player is still physically in the trigger area
                if (IsPlayerCurrentlyInTrigger())
                {
                    _playerInRoom = true;
                    EnterRoom();
                }
                else
                {
                    // Player left while disabled, ensure clean state
                    _playerInRoom = false;
                }
            }

            _wasPlayerInRoomBeforeDisable = false;
        }
        else
        {
            // Disabling the trigger
            _wasPlayerInRoomBeforeDisable = _playerInRoom;

            // If music is currently playing from this trigger, stop it
            if (_playerInRoom && _currentActiveRoom == this)
            {
                _currentActiveRoom = null;
                if (AudioManager.Instance != null)
                    AudioManager.Instance.TransitionBackToAmbient();
            }

            _playerInRoom = false;
            enabled = false;
        }
    }

    // Helper method to check if player is currently in the trigger area
    private bool IsPlayerCurrentlyInTrigger()
    {
        var col = GetComponent<Collider>();
        if (col == null) return false;

        // Find the player
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return false;

        // Check if player's collider overlaps with this trigger
        var playerCollider = player.GetComponent<Collider>();
        if (playerCollider == null) return false;

        return col.bounds.Intersects(playerCollider.bounds);
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