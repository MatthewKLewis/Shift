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

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
