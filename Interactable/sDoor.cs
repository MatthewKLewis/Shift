using UnityEngine;

//Slides open when the player walks up to it - unless it's locked.
//Lock / unlock it from the console.
public class sDoor : sDevice
{
    [SerializeField] private bool locked = true;
    [SerializeField] private float openRange = 3f;   //horizontal distance from the door's closed position
    [SerializeField] private float openHeight = 3f;
    [SerializeField] private float speed = 4f;

    [Space(4)]
    [Header("Status light")]
    [SerializeField] private Light statusLight;
    [SerializeField] private Color lockedColor = new(1f, 0.15f, 0.1f);
    [SerializeField] private Color unlockedColor = new(0.2f, 1f, 0.3f);

    [Space(4)]
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;  //added at runtime (3D) if left empty
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip lockSound;
    [SerializeField] private AudioClip unlockSound;

    private Vector3 closedPosition;
    private sPlayer player;
    private bool isOpen;

    public override string Kind => "DOOR";
    public override string Status => (locked ? "LOCKED" : "unlocked") + (isOpen ? ", open" : "");
    public override string[] Verbs => new[] { "lock", "unlock" };

    private void Awake()
    {
        closedPosition = transform.position;
        audioSource = sUtil.SetupAudio(gameObject, audioSource, spatial: true);
        if (statusLight == null) statusLight = GetComponentInChildren<Light>();
        UpdateStatusLight();
    }

    void UpdateStatusLight()
    {
        if (statusLight != null) statusLight.color = locked ? lockedColor : unlockedColor;
    }

    protected override void Start()
    {
        base.Start();
        player = FindAnyObjectByType<sPlayer>();
    }

    private void Update()
    {
        bool playerNear = false;
        if (player != null)
        {
            Vector3 toPlayer = player.transform.position - closedPosition;
            toPlayer.y = 0f;
            playerNear = toPlayer.magnitude <= openRange;
        }

        //Locking an open door doesn't slam it on the player - it closes once they've stepped away
        bool shouldOpen = playerNear && (!locked || isOpen);
        if (shouldOpen != isOpen)
        {
            isOpen = shouldOpen;
            sUtil.PlaySound(audioSource, isOpen ? openSound : closeSound);
        }

        Vector3 target = closedPosition + (isOpen ? Vector3.up * openHeight : Vector3.zero);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    public override string Execute(string verb)
    {
        switch (verb)
        {
            case "lock":
                if (locked) return $"{id}: already locked";
                locked = true;
                UpdateStatusLight();
                sUtil.PlaySound(audioSource, lockSound);
                return isOpen ? $"{id}: locked (closes when clear)" : $"{id}: locked";
            case "unlock":
                if (!locked) return $"{id}: already unlocked";
                locked = false;
                UpdateStatusLight();
                sUtil.PlaySound(audioSource, unlockSound);
                return $"{id}: unlocked";
            default:
                return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? closedPosition : transform.position, openRange);
    }
}
