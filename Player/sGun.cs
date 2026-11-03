using UnityEngine;

[RequireComponent(typeof(sPlayer))]

public class sGun : MonoBehaviour
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float shotsPerSecond = 5f;
    [SerializeField] private float range = 100f;
    [SerializeField] private Color tracerColor = new(1f, 0.85f, 0.3f);

    private sPlayer player;
    private InputSystem_Actions playerControls;
    private float nextShotTime;

    private void Awake()
    {
        player = GetComponent<sPlayer>();
        playerControls = new InputSystem_Actions();
    }

    private void OnEnable() => playerControls.Player.Attack.Enable();
    private void OnDisable() => playerControls.Player.Attack.Disable();

    void Update()
    {
        //Only the FPS mode can shoot - that's the trade-off for standing still
        if (player.IsDead || player.IsShifting || player.PlayerMode != PLAYER_MODE.firstPerson) return;

        if (playerControls.Player.Attack.IsPressed() && Time.time >= nextShotTime)
            Fire();
    }

    void Fire()
    {
        nextShotTime = Time.time + 1f / shotsPerSecond;

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
    }
}
