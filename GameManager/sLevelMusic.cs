using UnityEngine;

//Drop in a level to set its music. GameManager persists between scenes, so this just asks it
//to crossfade - reloading the same level (e.g. after death) keeps the track going.
public class sLevelMusic : MonoBehaviour
{
    [SerializeField] private AudioClip music;  //leave empty for silence
    [SerializeField] private float fadeTime = -1f; //< 0 = GameManager's default

    private void Start()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.PlayMusic(music, fadeTime);
    }
}
