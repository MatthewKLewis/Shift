using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class sHUD : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private Transform ammoTransform;
    [SerializeField] private Transform healthTransform;
    [SerializeField] private Transform consoleTransform;
    [SerializeField] private Transform crosshairTransform;
    [SerializeField] private Transform commsTransform;
    [SerializeField] private TMP_Text ammoText;

    [Space(4)]
    [Header("Health pips")]
    [SerializeField] private float pipSpacing = 8f;
    [SerializeField] private Color pipFullColor = new(0.3f, 0.95f, 0.45f);
    [SerializeField] private Color pipEmptyColor = new(0.15f, 0.15f, 0.15f, 0.8f);
    public Image healthPip;           //template, cloned once per point of max health - swap its sprite for hearts
    private readonly List<Image> pips = new();

    [Space(4)]
    [Header("Comms")]
    [SerializeField] private TMP_Text commsText;
    [SerializeField] private AudioSource commsAudio;    //added at runtime if left empty
    [SerializeField] private AudioClip commsSound;      //default, used when a message doesn't bring its own
    [SerializeField] private float commsRaiseTime = 0.2f;
    [SerializeField] private float commsBaseTime = 2f;      //seconds on screen per message...
    [SerializeField] private float commsTimePerChar = 0.05f; //...plus reading time
    private readonly Queue<(string message, AudioClip sound)> commsQueue = new();
    private Coroutine commsRoutine;

    //public Transform inventoryTransform; ?

    private void OnEnable()
    {
        Actions.OnShift += HandleShift;
        Actions.OnAmmoChanged += HandleAmmoChanged;
        Actions.OnPlayerHealthChanged += HandleHealthChanged;
        Actions.OnComms += HandleCommsEvent;
    }

    private void OnDisable()
    {
        Actions.OnShift -= HandleShift;
        Actions.OnAmmoChanged -= HandleAmmoChanged;
        Actions.OnPlayerHealthChanged -= HandleHealthChanged;
        Actions.OnComms -= HandleCommsEvent;
        commsRoutine = null; //disabling stops coroutines, so let the next message restart it
    }

    private void Start()
    {
        HandleShift(PLAYER_MODE.orthogonal);

        //Comms overlays every mode, so HandleShift leaves it alone
        commsTransform.localScale = new Vector3(1f, 0f, 1f);
        if (commsAudio == null) commsAudio = gameObject.AddComponent<AudioSource>();
        commsAudio.playOnAwake = false;
    }

    void HandleShift(PLAYER_MODE newMode)
    {
        switch (newMode)
        {
            case PLAYER_MODE.orthogonal:
                consoleTransform.localScale = Vector3.zero;
                crosshairTransform.localScale = Vector3.zero;
                ammoTransform.localScale = Vector3.zero;
                healthTransform.localScale = Vector3.one;
                break;

            case PLAYER_MODE.firstPerson:
                consoleTransform.localScale = Vector3.zero;
                crosshairTransform.localScale = Vector3.one;
                ammoTransform.localScale = Vector3.one;
                healthTransform.localScale = Vector3.zero;
                break;

            case PLAYER_MODE.console:
                consoleTransform.localScale = Vector3.one;
                crosshairTransform.localScale = Vector3.zero;
                ammoTransform.localScale = Vector3.zero;
                healthTransform.localScale = Vector3.zero;
                break;

            default:
                print("HUD - unknown mode!");
                break;
        }
    }

    void HandleAmmoChanged(int inMagazine, int magazineSize, bool reloading)
    {
        ammoText.text = reloading ? "RELOADING" : $"{inMagazine} / {magazineSize}";
    }

    void HandleHealthChanged(int current, int max)
    {
        //Grow the row to match max health (the template itself is pip 0)
        if (pips.Count == 0) pips.Add(healthPip);
        while (pips.Count < max)
        {
            var pip = Instantiate(healthPip, healthPip.transform.parent);
            var rect = pip.rectTransform;
            rect.anchoredPosition = healthPip.rectTransform.anchoredPosition
                + Vector2.right * pips.Count * (healthPip.rectTransform.sizeDelta.x + pipSpacing);
            pips.Add(pip);
        }

        for (int i = 0; i < pips.Count; i++)
        {
            pips[i].gameObject.SetActive(i < max);
            pips[i].color = i < current ? pipFullColor : pipEmptyColor;
        }
    }

    void HandleCommsEvent(string message, AudioClip sound)
    {
        //Queue so a second message doesn't cut off the first
        commsQueue.Enqueue((message, sound));
        commsRoutine ??= StartCoroutine(CommsRoutine());
    }

    IEnumerator CommsRoutine()
    {
        bool raised = false;

        while (commsQueue.Count > 0)
        {
            var (message, sound) = commsQueue.Dequeue();

            commsText.text = message;

            AudioClip clip = sound != null ? sound : commsSound;
            if (clip != null) commsAudio.PlayOneShot(clip);

            //Stay up between back-to-back messages, just swap the text
            if (!raised)
            {
                yield return RaiseComms(1f);
                raised = true;
            }

            yield return new WaitForSeconds(commsBaseTime + message.Length * commsTimePerChar);
        }

        yield return RaiseComms(0f);
        commsRoutine = null;
    }

    IEnumerator RaiseComms(float target)
    {
        //Scales up from the panel's pivot - set the pivot to bottom-centre for a "rise up" look
        float start = commsTransform.localScale.y;
        for (float t = 0f; t < commsRaiseTime; t += Time.deltaTime)
        {
            commsTransform.localScale = new Vector3(1f, Mathf.Lerp(start, target, t / commsRaiseTime), 1f);
            yield return null;
        }
        commsTransform.localScale = new Vector3(1f, target, 1f);
    }
}
