using UnityEngine;

//Chases and rams the player. Can't be hacked - you have to shift to FPS and shoot it.
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(sHealth))]

public class sDrone : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float aggroRange = 12f;
    [SerializeField] private float hitRange = 1.6f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 1f;

    [Space(4)]
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;  //added at runtime (3D) if left empty
    [SerializeField] private AudioClip humLoop;        //loops once the drone is chasing
    [SerializeField] private AudioClip alertSound;
    [SerializeField] private AudioClip hitSound;

    private CharacterController cC;
    private sPlayer player;
    private float nextHitTime;
    private bool alerted;

    private void Awake()
    {
        cC = GetComponent<CharacterController>();
        audioSource = sUtil.SetupAudio(gameObject, audioSource, spatial: true);
    }

    private void Start()
    {
        player = FindAnyObjectByType<sPlayer>();
    }

    private void Update()
    {
        if (player == null || player.IsDead) return;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance > aggroRange) return;

        if (!alerted)
        {
            alerted = true;
            Actions.OnConsoleLog?.Invoke("WARNING: hostile drone approaching");
            sUtil.PlaySound(audioSource, alertSound);

            if (humLoop != null)
            {
                audioSource.clip = humLoop;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        if (distance > hitRange)
        {
            transform.rotation = Quaternion.LookRotation(toPlayer);
            cC.SimpleMove(toPlayer.normalized * speed);
        }
        else if (Time.time >= nextHitTime)
        {
            nextHitTime = Time.time + hitCooldown;
            player.GetComponent<sHealth>().Damage(damage);
            Actions.OnConsoleLog?.Invoke("WARNING: collision damage");
            sUtil.PlaySound(audioSource, hitSound);
        }
    }
}
