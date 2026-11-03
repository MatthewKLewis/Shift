using UnityEngine;

//Drive into the zone to receive a comms message (raises the HUD comms panel and plays its sound).
[RequireComponent(typeof(Collider))]

public class sCommsTrigger : MonoBehaviour
{
    [TextArea(2, 5)]
    [SerializeField] private string message = "Unit 7, do you read me?";
    [SerializeField] private AudioClip sound;         //leave empty for the HUD's default comms sound
    [SerializeField] private bool triggerOnce = true;

    private bool triggered;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && triggered) return;
        if (other.GetComponentInParent<sPlayer>() == null) return;

        triggered = true;
        Actions.OnComms?.Invoke(message, sound);
    }

    //Zones are invisible in game, so draw them in the scene view
    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.3f, 0.7f, 1f, triggered ? 0.05f : 0.2f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
