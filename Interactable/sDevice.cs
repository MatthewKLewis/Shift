using System.Collections.Generic;
using TMPro;
using UnityEngine;

//Anything the console can talk to. The id is shown floating above it in the world,
//so you read it in overhead/FPS mode and type it in console mode.
public abstract class sDevice : MonoBehaviour
{
    public static readonly List<sDevice> All = new();

    public string id = "device_1";
    [SerializeField] private bool showLabel = true;
    [SerializeField] private float labelHeight = 4f;

    public abstract string Kind { get; }
    public abstract string Status { get; }
    public abstract string[] Verbs { get; }

    //Return a line for the console, or null if the verb isn't supported
    public abstract string Execute(string verb);

    private Transform label;

    protected virtual void OnEnable() => All.Add(this);
    protected virtual void OnDisable() => All.Remove(this);

    protected virtual void Start()
    {
        if (!showLabel) return;

        var labelObject = new GameObject("Label");
        label = labelObject.transform;
        label.SetParent(transform, false);
        label.position = transform.position + Vector3.up * labelHeight;
        labelObject.AddComponent<sBillboard>();

        var text = labelObject.AddComponent<TextMeshPro>();

        if (GameManager.Instance != null && GameManager.Instance.defaultFont != null)
            text.font = GameManager.Instance.defaultFont;
        text.text = id;
        text.fontSize = 8f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.4f, 1f, 0.5f);
        text.rectTransform.sizeDelta = new Vector2(6f, 1f);
    }

    protected virtual void LateUpdate()
    {
        if (label == null || Camera.main == null) return;
        label.rotation = Camera.main.transform.rotation; //billboard, works for ortho and FPS
    }
}
