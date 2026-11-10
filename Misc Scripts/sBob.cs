using UnityEngine;

public class sBob : MonoBehaviour
{
    [SerializeField] private AnimationCurve curve;

    void Update()
    {
        transform.localPosition = new Vector3(0, curve.Evaluate(Time.time % curve.keys[2].time), 0);
    }
}
