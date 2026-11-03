using UnityEngine;

[RequireComponent(typeof(Collider))]

public class sExit : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<sPlayer>() == null) return;
        print("Level complete!");
        Actions.OnLevelComplete?.Invoke();
    }
}
