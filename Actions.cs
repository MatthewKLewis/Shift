using System;
using UnityEngine;

public class Actions
{
    public static Action OnPlayerDeath;
    public static Action OnLevelComplete;
    public static Action<PLAYER_MODE, PLAYER_MODE> OnShiftStarted; //from, to - transition animations start
    public static Action<PLAYER_MODE> OnShift;          //mode has changed
    public static Action<PLAYER_MODE> OnRequestShift;   //ask the player to change mode (e.g. console "exit")
    public static Action<string> OnConsoleLog;
}
