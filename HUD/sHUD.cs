using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class sHUD : MonoBehaviour
{
    public Transform consoleTransform;
    public Transform crosshairTransform;
    public Transform ammoTransform;
    public TMP_Text ammoText;
    public Transform healthTransform;
    public Image healthPip;           //template, cloned once per point of max health - swap its sprite for hearts
    [SerializeField] private float pipSpacing = 8f;
    [SerializeField] private Color pipFullColor = new(0.3f, 0.95f, 0.45f);
    [SerializeField] private Color pipEmptyColor = new(0.15f, 0.15f, 0.15f, 0.8f);
    private readonly List<Image> pips = new();
    //public Transform inventoryTransform; ?

    private void OnEnable()
    {
        Actions.OnShift += HandleShift;
        Actions.OnAmmoChanged += HandleAmmoChanged;
        Actions.OnPlayerHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        Actions.OnShift -= HandleShift;
        Actions.OnAmmoChanged -= HandleAmmoChanged;
        Actions.OnPlayerHealthChanged -= HandleHealthChanged;
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
}
