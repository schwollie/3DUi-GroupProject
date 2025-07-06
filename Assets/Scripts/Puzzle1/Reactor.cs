using UnityEngine;
using UnityEngine.Events;

public class Reactor : MonoBehaviour
{
    public SnapAnchor RedSnapAnchor;
    public SnapAnchor BlueSnapAnchor;

    public UnityEvent onReactorActivated;
    public UnityEvent onReactorFailedActivation;

    public void Start()
    {
        RedSnapAnchor.onSnapEnter.AddListener(OnSnap);
        BlueSnapAnchor.onSnapEnter.AddListener(OnSnap);
    }

    private void OnSnap()
    {
        if (RedSnapAnchor.currentSnappedObject != null && BlueSnapAnchor.currentSnappedObject != null)
        {
            var redPowercell = RedSnapAnchor.currentSnappedObject.GetComponent<Powercell>();
            var bluePowercell = BlueSnapAnchor.currentSnappedObject.GetComponent<Powercell>();


            if (redPowercell != null && bluePowercell != null)
            {
                if (redPowercell.CorrectCharge() && bluePowercell.CorrectCharge())
                    OnSuccess();
                else
                    OnFail();
            }
            else
            {
                OnFail();
            }
        }
    }

    private void OnSuccess()
    {
        onReactorActivated.Invoke();
        RedSnapAnchor.AllowLeave(false);
        BlueSnapAnchor.AllowLeave(false);
    }

    private Powercell currentSnapedA;
    private Powercell currentSnapedB;

    private void OnFail()
    {
        // Store references BEFORE resetting
        Powercell redPowercell = null;
        Powercell bluePowercell = null;

        if (RedSnapAnchor.currentSnappedObject != null)
            redPowercell = RedSnapAnchor.currentSnappedObject.GetComponent<Powercell>();

        if (BlueSnapAnchor.currentSnappedObject != null)
            bluePowercell = BlueSnapAnchor.currentSnappedObject.GetComponent<Powercell>();

        // Reset the anchors
        RedSnapAnchor.Reset();
        BlueSnapAnchor.Reset();

        // Store references for delayed execution
        currentSnapedA = redPowercell;
        currentSnapedB = bluePowercell;

        // Execute destroy and respawn after 0.1 seconds
        Invoke(nameof(DelayedDestroyAndRespawn), 0.1f);

        onReactorFailedActivation.Invoke();
    }

    private void DelayedDestroyAndRespawn()
    {
        if (currentSnapedA != null)
            currentSnapedA.DestroyPowercellAndRespawn();

        if (currentSnapedB != null)
            currentSnapedB.DestroyPowercellAndRespawn();
    }
}