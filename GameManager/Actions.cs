using System;
using UnityEngine;

public class Actions
{
    public static Action OnPlayerDeath;
    public static Action OnLevelComplete;
    public static Action<int> OnLevelLoadStarted;       //build index - loading screen is fading in
    public static Action<float> OnLevelLoadProgress;    //0-1, for a progress bar / text
    public static Action<int> OnLevelLoaded;            //build index - loading screen has faded out

    public static Action<PLAYER_MODE> OnShift;          //mode has changed

    public static Action<string> OnConsoleLog;
    public static Action<string, AudioClip> OnComms;       //message, sound (null = HUD's default comms sound)
    public static Action<int, int> OnPlayerHealthChanged;  //current, max
    public static Action<int, int, bool> OnAmmoChanged;        //in magazine, magazine size, reloading
}
