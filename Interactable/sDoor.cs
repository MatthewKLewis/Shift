using UnityEngine;

public class sDoor : sDevice
{
    [SerializeField] private bool isOpen;
    [SerializeField] private float openHeight = 3f;
    [SerializeField] private float speed = 4f;

    private Vector3 closedPosition;

    public override string Kind => "DOOR";
    public override string Status => isOpen ? "open" : "closed";
    public override string[] Verbs => new[] { "open", "close" };

    private void Awake()
    {
        closedPosition = transform.position;
    }

    private void Update()
    {
        Vector3 target = closedPosition + (isOpen ? Vector3.up * openHeight : Vector3.zero);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    public override string Execute(string verb)
    {
        switch (verb)
        {
            case "open":
                isOpen = true;
                return $"{id}: opening";
            case "close":
                isOpen = false;
                return $"{id}: closing";
            default:
                return null;
        }
    }
}
