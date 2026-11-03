using UnityEngine;

public static class sUtil
{
    private static Material tracerMaterial;

    //Raycast that skips colliders belonging to the shooter (no layers needed)
    public static bool Raycast(Ray ray, float range, Transform ignore, out RaycastHit hit)
    {
        var hits = Physics.RaycastAll(ray, range, ~0, QueryTriggerInteraction.Ignore);
        hit = default;
        float nearest = float.MaxValue;

        foreach (var h in hits)
        {
            if (ignore != null && h.transform.IsChildOf(ignore)) continue;
            if (h.distance < nearest)
            {
                nearest = h.distance;
                hit = h;
            }
        }

        return nearest < float.MaxValue;
    }

    public static void Tracer(Vector3 from, Vector3 to, Color color, float duration = 0.06f)
    {
        if (tracerMaterial == null) tracerMaterial = new Material(Shader.Find("Sprites/Default"));

        var line = new GameObject("Tracer").AddComponent<LineRenderer>();
        line.material = tracerMaterial;
        line.startColor = line.endColor = color;
        line.startWidth = 0.05f;
        line.endWidth = 0.02f;
        line.SetPositions(new[] { from, to });
        Object.Destroy(line.gameObject, duration);
    }
}
