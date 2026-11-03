using UnityEngine;

public class sHUD : MonoBehaviour
{
    public Transform consoleTransform;
    public Transform crosshairTransform;
    //public Transform ammoTransform; ?
    //public Transform inventoryTransform; ?

    private void OnEnable()
    {
        Actions.OnShift += HandleShift;
    }

    private void OnDisable()
    {
        Actions.OnShift -= HandleShift;
    }

    private void Start()
    {
        HandleShift(PLAYER_MODE.orthogonal);
    }

    void HandleShift(PLAYER_MODE newMode)
    {
        switch (newMode)
        {
            case PLAYER_MODE.orthogonal:
                consoleTransform.localScale = Vector3.zero;
                crosshairTransform.localScale = Vector3.zero;
                break;

            case PLAYER_MODE.firstPerson:
                consoleTransform.localScale = Vector3.zero;
                crosshairTransform.localScale = Vector3.one;
                break;

            case PLAYER_MODE.console:
                consoleTransform.localScale = Vector3.one;
                crosshairTransform.localScale = Vector3.zero;
                break;

            default:
                print("HUD - unknown mode!");
                break;
        }
    }
}
