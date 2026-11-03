using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class sConsole : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text outputText;

    [SerializeField] private int maxLines = 14;
    [SerializeField] private float deviceRange = 15f; //metres - you have to drive close enough to hack

    private readonly List<string> lines = new();
    private readonly List<string> history = new();
    private int historyIndex;
    private bool consoleActive;
    private bool locked; //dead or level complete - output only
    private sPlayer player;
    private sHealth playerHealth;

    private Dictionary<string, (string help, Action<string[]> run)> commands;

    private void Awake()
    {
        commands = new()
        {
            ["help"]  = ("list commands",                     _ => Help()),
            ["clear"] = ("clear the screen",                  _ => { lines.Clear(); Refresh(); }),
            ["scan"]  = ("list devices in range",             _ => Scan()),
            ["status"]= ("show hull integrity",               _ => Status()),
            ["exit"]  = ("close the console",                 _ => Actions.OnRequestShift?.Invoke(PLAYER_MODE.orthogonal)),
        };
    }

    private void OnEnable()
    {
        Actions.OnShift += HandleShift;
        Actions.OnConsoleLog += Print;
        Actions.OnPlayerDeath += Lock;
        Actions.OnLevelComplete += Lock;
        inputField.onSubmit.AddListener(Submit);
    }

    private void OnDisable()
    {
        Actions.OnShift -= HandleShift;
        Actions.OnConsoleLog -= Print;
        Actions.OnPlayerDeath -= Lock;
        Actions.OnLevelComplete -= Lock;
        inputField.onSubmit.RemoveListener(Submit);
    }

    private void Start()
    {
        player = FindAnyObjectByType<sPlayer>();
        if (player != null) playerHealth = player.GetComponent<sHealth>();

        inputField.onFocusSelectAll = false;
        inputField.lineType = TMP_InputField.LineType.SingleLine;

        lines.Clear();
        Print("SHIFT-OS v0.1  //  type 'help'");
    }

    private void Update()
    {
        if (!consoleActive || locked) return;

        //Keep focus: TMP deactivates the field after every submit, and a stray click deselects it
        if (!inputField.isFocused) Focus();

        var kb = Keyboard.current;
        if (kb != null && history.Count > 0)
        {
            if (kb.upArrowKey.wasPressedThisFrame) RecallHistory(-1);
            if (kb.downArrowKey.wasPressedThisFrame) RecallHistory(+1);
        }
    }

    void HandleShift(PLAYER_MODE newMode)
    {
        consoleActive = newMode == PLAYER_MODE.console;

        if (consoleActive && !locked)
        {
            inputField.text = "";
            Focus();
        }
        else
        {
            Unfocus();
        }
    }

    void Focus()
    {
        //Both are needed: the EventSystem selection routes keyboard events to the field,
        //ActivateInputField puts the caret in it (applied on the field's next update)
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != inputField.gameObject)
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        inputField.ActivateInputField();
    }

    void Unfocus()
    {
        inputField.DeactivateInputField();
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == inputField.gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    void Lock()
    {
        locked = true;
        inputField.text = "";
        Unfocus();
        inputField.interactable = false;
    }

    void Submit(string text)
    {
        if (!consoleActive || locked) return;

        inputField.text = "";
        text = text.Trim();
        if (text.Length == 0) return;

        history.Add(text);
        historyIndex = history.Count;

        Print("> " + text);
        Execute(text);
    }

    void Execute(string text)
    {
        string[] words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string verb = words[0];
        string[] args = words.Skip(1).ToArray();

        if (commands.TryGetValue(verb, out var command))
        {
            command.run(args);
            return;
        }

        //Anything else is "<verb> <device>", e.g. "open door_1", "disable turret_a"
        if (args.Length != 1)
        {
            Print($"unknown command '{verb}'. type 'help'");
            return;
        }

        sDevice device = sDevice.All.FirstOrDefault(d => d.id.ToLowerInvariant() == args[0]);
        if (device == null)
        {
            Print($"no device '{args[0]}'");
            return;
        }

        if (player != null && Vector3.Distance(player.transform.position, device.transform.position) > deviceRange)
        {
            Print($"{device.id}: out of range");
            return;
        }

        string result = device.Execute(verb);
        Print(result ?? $"{device.id}: '{verb}' not supported. try: {string.Join(", ", device.Verbs)}");
    }

    void Help()
    {
        foreach (var command in commands)
            Print($"  {command.Key,-8}{command.Value.help}");
        Print("  <action> <device>   e.g. open door_1");
    }

    void Scan()
    {
        var inRange = sDevice.All
            .Select(d => (device: d, distance: player == null ? 0f : Vector3.Distance(player.transform.position, d.transform.position)))
            .Where(d => d.distance <= deviceRange)
            .OrderBy(d => d.distance)
            .ToList();

        if (inRange.Count == 0)
        {
            Print("scan: nothing in range");
            return;
        }

        foreach (var (device, distance) in inRange)
            Print($"  {device.id,-12}{device.Kind,-8}{device.Status,-10}{distance:0}m  [{string.Join("|", device.Verbs)}]");
    }

    void Status()
    {
        if (playerHealth == null) return;
        Print($"hull integrity {playerHealth.Current:0}/{playerHealth.maxHealth:0}");
    }

    void RecallHistory(int direction)
    {
        historyIndex = Mathf.Clamp(historyIndex + direction, 0, history.Count);
        inputField.text = historyIndex < history.Count ? history[historyIndex] : "";
        inputField.caretPosition = inputField.text.Length;
    }

    public void Print(string line)
    {
        lines.Add(line);
        while (lines.Count > maxLines) lines.RemoveAt(0);
        Refresh();
    }

    void Refresh()
    {
        outputText.text = string.Join("\n", lines);
    }
}
