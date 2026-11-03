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

    #region Audio

    //Returns the AudioSource to play sound effects from. If none is assigned/present, adds one:
    //spatial = 3D falloff (enemies), otherwise 2D (the player's own sounds).
    //Linear falloff to 40m because the overhead camera (with the AudioListener) sits ~15m away.
    public static AudioSource SetupAudio(GameObject owner, AudioSource assigned, bool spatial)
    {
        if (assigned != null) return assigned; //respect whatever was set up in the inspector

        var source = owner.GetComponent<AudioSource>();
        if (source != null) return source;

        source = owner.AddComponent<AudioSource>();
        ConfigureSource(source, spatial);
        return source;
    }

    //pitchVariance: +/- random pitch, so repeated sounds (gunshots) don't sound robotic
    public static void PlaySound(AudioSource source, AudioClip clip, float volume = 1f, float pitchVariance = 0f)
    {
        if (source == null || clip == null) return;
        source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        source.PlayOneShot(clip, volume);
    }

    //For sounds that must outlive their object (death sounds) - plays from a temporary object
    public static void PlaySoundAt(AudioClip clip, Vector3 position, float volume = 1f, bool spatial = true)
    {
        if (clip == null) return;

        var source = new GameObject($"OneShot ({clip.name})").AddComponent<AudioSource>();
        source.transform.position = position;
        ConfigureSource(source, spatial);
        source.PlayOneShot(clip, volume);
        Object.Destroy(source.gameObject, clip.length + 0.1f);
    }

    static void ConfigureSource(AudioSource source, bool spatial)
    {
        source.playOnAwake = false;
        source.spatialBlend = spatial ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 5f;
        source.maxDistance = 40f;
    }

    #endregion
}
