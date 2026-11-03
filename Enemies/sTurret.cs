using UnityEngine;

//A device AND an enemy: shoot it in FPS mode, or "disable" it from the console.
[RequireComponent(typeof(sHealth))]

public class sTurret : sDevice
{
    [SerializeField] private Transform head;
    [SerializeField] private float range = 14f;
    [SerializeField] private float shotsPerSecond = 1f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private bool active = true;

    [Space(4)]
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;  //added at runtime (3D) if left empty
    [SerializeField] private AudioClip shotSound;
    [SerializeField] private AudioClip alertSound;     //player comes into range
    [SerializeField] private AudioClip powerDownSound; //console "disable"
    [SerializeField] private AudioClip powerUpSound;   //console "enable"
    [SerializeField] private float shotPitchVariance = 0.05f;

    private sPlayer player;
    private float nextShotTime;
    private bool playerInRange;

    public override string Kind => "TURRET";
    public override string Status => active ? "ACTIVE" : "offline";
    public override string[] Verbs => new[] { "disable", "enable" };

    protected override void Start()
    {
        base.Start();
        player = FindAnyObjectByType<sPlayer>();
        if (head == null) head = transform;
        audioSource = sUtil.SetupAudio(gameObject, audioSource, spatial: true);
    }

    private void Update()
    {
        if (!active || player == null || player.IsDead) return;

        Vector3 target = player.transform.position + Vector3.up;
        Vector3 toPlayer = target - head.position;

        bool inRange = toPlayer.magnitude <= range;
        if (inRange && !playerInRange) sUtil.PlaySound(audioSource, alertSound);
        playerInRange = inRange;
        if (!inRange) return;

        Quaternion look = Quaternion.LookRotation(toPlayer);
        head.rotation = Quaternion.RotateTowards(head.rotation, look, turnSpeed * Time.deltaTime);

        if (Time.time < nextShotTime || Quaternion.Angle(head.rotation, look) > 5f) return;

        //Needs line of sight - closed doors and walls block it
        var ray = new Ray(head.position, toPlayer.normalized);
        if (!sUtil.Raycast(ray, range, transform, out RaycastHit hit)) return;
        if (!hit.transform.IsChildOf(player.transform)) return;

        nextShotTime = Time.time + 1f / shotsPerSecond;
        player.GetComponent<sHealth>().Damage(damage);
        sUtil.Tracer(head.position, hit.point, Color.red, 0.1f);
        sUtil.PlaySound(audioSource, shotSound, pitchVariance: shotPitchVariance);
        Actions.OnConsoleLog?.Invoke($"WARNING: hit by {id}");
    }

    public override string Execute(string verb)
    {
        switch (verb)
        {
            case "disable":
                if (active) sUtil.PlaySound(audioSource, powerDownSound);
                active = false;
                playerInRange = false; //so it alerts again if re-enabled
                return $"{id}: offline";
            case "enable":
                if (!active) sUtil.PlaySound(audioSource, powerUpSound);
                active = true;
                return $"{id}: online (why?)";
            default:
                return null;
        }
    }
}
