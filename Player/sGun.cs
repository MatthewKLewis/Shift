using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(sPlayer))]

public class sGun : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float shotsPerSecond = 5f;
    [SerializeField] private float range = 100f;
    [SerializeField] private Color tracerColor = new(1f, 0.85f, 0.3f);

    [Space(4)]
    [Header("Ammo")]
    [SerializeField] private int magazineSize = 12;
    [SerializeField] private float reloadTime = 1.5f;

    private sPlayer player;
    private InputSystem_Actions playerControls;
    private float nextShotTime;
    private int inMagazine;
    private bool reloading;

    private void Awake()
    {
        player = GetComponent<sPlayer>();
        playerControls = new InputSystem_Actions();
        inMagazine = magazineSize;
    }

    private void OnEnable() => playerControls.Player.Attack.Enable();
    private void OnDisable() => playerControls.Player.Attack.Disable();

    private void Start()
    {
        BroadcastAmmo();
    }

    void Update()
    {
        //Only the FPS mode can shoot - that's the trade-off for standing still
        if (player.IsDead || player.IsShifting || player.PlayerMode != PLAYER_MODE.firstPerson) return;
        if (reloading) return;

        //No Reload action in the input map yet, so read R directly
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && inMagazine < magazineSize)
        {
            StartCoroutine(Reload());
            return;
        }

        if (playerControls.Player.Attack.IsPressed() && Time.time >= nextShotTime)
            Fire();
    }

    void Fire()
    {
        nextShotTime = Time.time + 1f / shotsPerSecond;
        inMagazine--;

        Transform cam = player.Cam.transform;
        var ray = new Ray(cam.position, cam.forward);
        Vector3 end = ray.GetPoint(range);

        if (sUtil.Raycast(ray, range, transform, out RaycastHit hit))
        {
            end = hit.point;
            hit.collider.GetComponentInParent<sHealth>()?.Damage(damage);
        }

        //Start the tracer a little below the eye so it's visible
        sUtil.Tracer(cam.position - cam.up * 0.15f + cam.right * 0.1f, end, tracerColor);

        if (inMagazine <= 0) StartCoroutine(Reload());
        else BroadcastAmmo();
    }

    IEnumerator Reload()
    {
        reloading = true;
        BroadcastAmmo();

        //Reload carries on while you're in other modes - shifting away is a way to wait it out
        yield return new WaitForSeconds(reloadTime);

        inMagazine = magazineSize;
        reloading = false;
        BroadcastAmmo();
    }

    void BroadcastAmmo()
    {
        Actions.OnAmmoChanged?.Invoke(inMagazine, magazineSize, reloading);
    }
}
