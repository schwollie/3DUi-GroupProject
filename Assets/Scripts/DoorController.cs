using UnityEngine;

public class DoorController : MonoBehaviour
{
    private static readonly int Open = Animator.StringToHash("Open");
    private static readonly int Close = Animator.StringToHash("Close");
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private SoundDefinition openSound;
    [SerializeField] private SoundDefinition closeSound;
    
    public void OpenDoor()
    {
        doorAnimator.SetTrigger(Open);
        AudioManager.Instance.PlaySound(openSound, transform.position);
    }

    public void CloseDoor()
    {
        doorAnimator.SetTrigger(Close);
        AudioManager.Instance.PlaySound(closeSound, transform.position);
    }
}
