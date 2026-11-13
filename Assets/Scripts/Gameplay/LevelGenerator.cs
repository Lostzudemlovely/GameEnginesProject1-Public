// Game Engines – Projekt 1
// Erzeugt Anstoßflächen und abwechslungsreiche Parcours aus sechs Streckentypen.
using UnityEngine;

public enum CourseType { Islands, Zigzag, Balance, Slalom, Rotors, Moving }

public sealed class LevelGenerator : MonoBehaviour
{
    [Header("Vorlagen und Materialien")]
    [SerializeField] private Goal goalPrefab;
    [SerializeField] private RotatingObstacle rotorPrefab;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material platformMaterial;
    [SerializeField] private Material obstacleMaterial;
    [SerializeField] private Material accentMaterial;
    [SerializeField] private Mesh timberDeckMesh;
    [SerializeField, Range(0, 31)] private int groundLayer = 8;

    [Header("Level und Schwierigkeit")]
    [SerializeField] private bool randomizeSeedEachRun = true;
    [SerializeField] private int seed = 21731;
    [SerializeField, Range(3, 8)] private int platformsPerLevel = 5;
    [SerializeField, Min(1)] private int levelsToMaxDifficulty = 10;
    [SerializeField] private Vector2 gapRange = new Vector2(0.8f, 1.7f);
    [SerializeField] private Vector2 platformWidthRange = new Vector2(5.5f, 4f);
    [SerializeField, Min(0f)] private float maxSideStep = 1.2f;
    [SerializeField] private Vector2 rotorSpeedRange = new Vector2(45f, 100f);
    [SerializeField, HideInInspector] private Transform currentRoot;

    private int? runSeed;
    public int RunSeed => runSeed ?? seed;
    public CourseType CurrentCourseType { get; private set; }
    public CourseType? SecondCourseType { get; private set; }
    public Vector3 KickoffTarget { get; private set; }
    public bool RequiresKickoffSteering { get; private set; }
    public string CourseName => SecondCourseType.HasValue
        ? NameOf(CurrentCourseType) + " / " + NameOf(SecondCourseType.Value)
        : NameOf(CurrentCourseType);

    public void BeginRun()
    {
        runSeed = randomizeSeedEachRun ? Random.Range(0, int.MaxValue) : seed;
    }

    private static string NameOf(CourseType type) => type switch
    {
        CourseType.Islands => "Sprunginseln",
        CourseType.Zigzag => "Zickzack",
        CourseType.Balance => "Balance-Stege",
        CourseType.Slalom => "Slalom",
        CourseType.Rotors => "Drehbalken",
        _ => "Bewegliche Plattformen"
    };

    public void BuildLevel(int level, GameManager manager)
    {
        if (currentRoot != null)
        {
            // Collider sofort abschalten; Unity entfernt die Objekte am Frame-Ende.
            currentRoot.gameObject.SetActive(false);
            Destroy(currentRoot.gameObject);
        }
        level = Mathf.Max(1, level);
        currentRoot = new GameObject("Generated Course").transform;
        currentRoot.SetParent(transform, false);
        CurrentCourseType = SelectCourse(level);
        var random = new System.Random(unchecked(RunSeed ^ (level * 7919)));
        // Level 1 startet bei 0; nach levelsToMaxDifficulty Steigerungen bleibt der Faktor bei 1.
        float difficulty = Mathf.Clamp01((level - 1f) / Mathf.Max(1, levelsToMaxDifficulty));
        float gap = Mathf.Lerp(gapRange.x, gapRange.y, difficulty);
        Vector3 start = BuildKickoff(random, level, difficulty);
        SecondCourseType = level >= 4 && random.Next(3) == 0
            ? (CourseType)(((int)CurrentCourseType + random.Next(1, 6)) % 6)
            : null;
        Vector3 end = BuildSection(CurrentCourseType, start, random, difficulty, gap, SecondCourseType.HasValue);
        if (SecondCourseType.HasValue)
            end = BuildSection(SecondCourseType.Value, end, random, difficulty, gap, true);

        float top = end.y + 0.15f;
        float z = end.z + Mathf.Min(gap, 1.3f) + 3f;
        Box("Landing Deck", new Vector3(end.x, top - 0.5f, z), new Vector3(9f, 1f, 6f), floorMaterial);
        // Die Ziel-Prefab wurde für eine Landefläche mit Oberkante 0,9 gebaut.
        Goal goal = Instantiate(goalPrefab, new Vector3(end.x, top - 0.9f, z + 1.2f), Quaternion.identity, currentRoot);
        goal.Initialize(manager);
        Physics.SyncTransforms();
    }

    private Vector3 BuildKickoff(System.Random random, int level, float difficulty)
    {
        Box("Kickoff Stand", new Vector3(0f, -0.5f, -3.4f), new Vector3(6.5f, 1f, 3.6f), floorMaterial);
        Box("Kickoff Line", new Vector3(0f, 0.02f, -2.2f), new Vector3(5.8f, 0.03f, 0.12f), accentMaterial, false);

        RequiresKickoffSteering = level >= 4;
        float width = level == 1 ? 6.6f : Mathf.Lerp(5.6f, 4.3f, difficulty);
        float depth = level == 1 ? 5.8f : Mathf.Lerp(5.4f, 4.8f, difficulty);
        const float top = 0.15f;
        float x = Range(random, -0.85f, 0.85f);
        float z = level == 1 ? Range(random, 6.5f, 7.2f) : Range(random, 6.7f, 8.6f);
        KickoffTarget = new Vector3(x, top, z);
        BuildKickoffPad("Kickoff Landing", KickoffTarget, width, depth);
        Vector3 end = new Vector3(x, top, z + depth / 2f);
        if (level == 1) return end;

        // Die zweite Landefläche liegt seitlich und etwas näher am weiteren Parcours.
        float direction = random.Next(2) == 0 ? -1f : 1f;
        float secondWidth = Mathf.Lerp(3.8f, 3.2f, difficulty);
        float secondDepth = Range(random, 3.9f, 4.5f);
        float sideGap = RequiresKickoffSteering ? 1.4f : 0.65f;
        float secondX = x + direction * ((width + secondWidth) / 2f + sideGap);
        if (RequiresKickoffSteering)
        {
            // Die Flugbahn beginnt zwischen beiden Flächen. Links oder rechts lenkt zur Landung.
            KickoffTarget = new Vector3(x + direction * (width + sideGap) / 2f, top, z);
        }
        end.z += 0.55f;
        BuildKickoffPad("Kickoff Side Landing", new Vector3(secondX, top + 0.15f, end.z - secondDepth / 2f),
            secondWidth, secondDepth);

        AppendDeck(ref end, "Kickoff Approach", (x + secondX) / 2f, top + 0.15f, 7.8f, 3.5f, 0.85f);
        return end;
    }

    private void BuildKickoffPad(string name, Vector3 surface, float width, float depth)
    {
        Box(name, surface + Vector3.down * 0.5f, new Vector3(width, 1f, depth), platformMaterial);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 stripe = surface + new Vector3(side * (width / 2f - 0.22f), 0.02f, 0f);
            Box("Landing Mark", stripe, new Vector3(0.13f, 0.03f, depth - 0.6f), accentMaterial, false);
        }
    }

    // Die Abschnittsbauer erhalten X, Oberkante und vordere Z-Kante der letzten Fläche.
    private Vector3 BuildSection(CourseType type, Vector3 start, System.Random random,
        float difficulty, float gap, bool shortSection) => type switch
    {
        CourseType.Islands => BuildIslands(start, random, difficulty, gap, shortSection),
        CourseType.Zigzag => BuildZigzag(start, random, difficulty, gap, shortSection),
        CourseType.Balance => BuildBalance(start, random, difficulty, gap, shortSection),
        CourseType.Slalom => BuildSlalom(start, random, difficulty, shortSection),
        CourseType.Rotors => BuildRotors(start, random, difficulty, gap, shortSection),
        _ => BuildMoving(start, random, difficulty, gap, shortSection)
    };

    private CourseType SelectCourse(int level)
    {
        int cycle = (level - 1) / 6;
        CourseType[] bag = ShuffledBag(cycle);
        // Die Korrektur tauscht nur 0/1; das letzte Element des vorherigen Beutels bleibt gleich.
        if (cycle > 0 && bag[0] == ShuffledBag(cycle - 1)[5])
        {
            CourseType swap = bag[0]; bag[0] = bag[1]; bag[1] = swap;
        }
        return bag[(level - 1) % 6];
    }

    private CourseType[] ShuffledBag(int cycle)
    {
        var bag = new[] { CourseType.Islands, CourseType.Zigzag, CourseType.Balance, CourseType.Slalom, CourseType.Rotors, CourseType.Moving };
        var random = new System.Random(unchecked(RunSeed ^ (cycle * 104729) ^ 12345));
        for (int i = bag.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            CourseType swap = bag[i]; bag[i] = bag[j]; bag[j] = swap;
        }
        if (cycle == 0)
        {
            CourseType first = (CourseType)random.Next(3);
            int index = System.Array.IndexOf(bag, first);
            bag[index] = bag[0]; bag[0] = first;
        }
        return bag;
    }

    private int PlatformCount(System.Random random, bool shortSection)
        => Mathf.Clamp(platformsPerLevel + random.Next(-1, 3) - (shortSection ? 2 : 0),
            shortSection ? 3 : 4, shortSection ? 5 : 8);

    private Vector3 BuildIslands(Vector3 start, System.Random random, float difficulty, float gap, bool shortSection)
    {
        Vector3 end = start;
        int count = PlatformCount(random, shortSection);
        float width = Mathf.Lerp(platformWidthRange.x, platformWidthRange.y, difficulty);
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Clamp(end.x + Range(random, -maxSideStep, maxSideStep), start.x - 2.8f, start.x + 2.8f);
            float top = Mathf.Clamp(end.y + Range(random, -0.25f, 0.3f), start.y - 0.1f, start.y + 0.75f);
            AppendDeck(ref end, "Island " + i, x, top, Mathf.Max(4f, width + Range(random, -0.55f, 0.55f)),
                Range(random, 2.8f, 4.6f), Range(random, Mathf.Lerp(gapRange.x, gap, 0.4f), gap));
        }
        return end;
    }

    private Vector3 BuildZigzag(Vector3 start, System.Random random, float difficulty, float gap, bool shortSection)
    {
        Vector3 end = start;
        float side = Mathf.Lerp(1.6f, 2f, difficulty);
        float direction = random.Next(2) == 0 ? -1f : 1f;
        int count = PlatformCount(random, shortSection);
        for (int i = 0; i < count; i++)
        {
            float x = start.x + direction * Range(random, 1.2f, side);
            if (random.Next(5) != 0) direction = -direction;
            AppendDeck(ref end, "Zigzag " + i, x, start.y + Range(random, 0.1f, 0.3f),
                Mathf.Lerp(4.8f, 3.9f, difficulty), Range(random, 3.2f, 4.6f),
                Range(random, gapRange.x, Mathf.Min(gap, 1.2f)));
        }
        return end;
    }

    private Vector3 BuildBalance(Vector3 start, System.Random random, float difficulty, float gap, bool shortSection)
    {
        Vector3 end = start;
        float top = start.y + 0.2f;
        AppendDeck(ref end, "Rest Island 0", start.x, top, 5f, 3.5f, Mathf.Min(gap, 1f));
        Vector3 previous = new Vector3(end.x, top, end.z - 1.75f);
        int count = shortSection ? random.Next(2, 4) : random.Next(3, 6);
        float direction = random.Next(2) == 0 ? -1f : 1f;
        for (int i = 1; i <= count; i++)
        {
            Vector3 next = new Vector3(start.x + direction * Range(random, 1.3f, 2.5f), top,
                previous.z + Range(random, 5.8f, 8f));
            if (random.Next(4) != 0) direction = -direction;
            Box("Rest Island " + i, next + Vector3.down * 0.5f, new Vector3(4.5f, 1f, 3.5f), platformMaterial);
            Vector3 delta = next - previous;
            GameObject beam = Box("Balance Beam " + i, (previous + next) / 2f + Vector3.down * 0.19f,
                new Vector3(Mathf.Lerp(2.2f, 1.35f, difficulty), 0.4f, delta.magnitude), floorMaterial);
            beam.transform.rotation = Quaternion.LookRotation(delta, Vector3.up);
            previous = next;
        }
        return new Vector3(previous.x, previous.y, previous.z + 1.75f);
    }

    private Vector3 BuildSlalom(Vector3 start, System.Random random, float difficulty, bool shortSection)
    {
        int count = shortSection ? random.Next(3, 5) : random.Next(4, 8);
        float z = start.z + 3.5f;
        float side = random.Next(2) == 0 ? -1f : 1f;
        float height = Mathf.Lerp(1.2f, 1.5f, difficulty);
        for (int i = 0; i < count; i++)
        {
            float width = Range(random, 4.2f, Mathf.Lerp(4.9f, 5.3f, difficulty));
            Box("Slalom Wall " + i, new Vector3(start.x + side * (4f - width / 2f), start.y + height / 2f, z),
                new Vector3(width, height, 0.65f), obstacleMaterial);
            if (random.Next(5) != 0) side = -side;
            z += Range(random, 4.4f, 6.1f);
        }
        float depth = z - start.z;
        Box("Slalom Runway", new Vector3(start.x, start.y - 0.5f, start.z + depth / 2f),
            new Vector3(8f, 1f, depth), platformMaterial);
        return new Vector3(start.x, start.y, z);
    }

    private Vector3 BuildRotors(Vector3 start, System.Random random, float difficulty, float gap, bool shortSection)
    {
        Vector3 end = start;
        int count = !shortSection && difficulty >= 0.6f ? random.Next(2, 4) : 2;
        float direction = random.Next(2) == 0 ? -1f : 1f;
        for (int i = 0; i < count; i++)
        {
            float x = start.x + Range(random, -1.5f, 1.5f);
            float top = start.y + 0.25f + i * 0.15f;
            float depth = Range(random, 7f, 9f);
            AppendDeck(ref end, "Rotor Arena " + i, x, top, Range(random, 7.2f, 8.5f), depth, Mathf.Min(gap, 1.4f));
            RotatingObstacle rotor = Instantiate(rotorPrefab, new Vector3(x, top + 0.45f, end.z - depth / 2f),
                Quaternion.Euler(0f, random.Next(0, 180), 0f), currentRoot);
            rotor.SetSpeed(direction * Mathf.Lerp(rotorSpeedRange.x, rotorSpeedRange.y, difficulty));
            direction = -direction;
            if (i < count - 1) AppendDeck(ref end, "Rotor Rest " + i, x, top, 5f, 3f, Mathf.Min(gap, 1.4f));
        }
        return end;
    }

    private Vector3 BuildMoving(Vector3 start, System.Random random, float difficulty, float gap, bool shortSection)
    {
        Vector3 end = start;
        int count = PlatformCount(random, shortSection);
        for (int i = 0; i < count; i++)
        {
            bool moving = i % 2 == 0;
            GameObject deck = AppendDeck(ref end, (moving ? "Moving Deck " : "Safe Deck ") + i,
                start.x + Range(random, -0.85f, 0.85f), start.y + Range(random, 0.15f, 0.3f),
                Mathf.Lerp(4.8f, 4f, difficulty), Range(random, 3.2f, 4.4f), Mathf.Min(gap, 1.3f));
            if (!moving) continue;
            Rigidbody body = deck.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            float amplitude = Range(random, 0.7f, Mathf.Lerp(0.9f, 1.2f, difficulty));
            float duration = Mathf.Lerp(4.5f, 3.5f, difficulty) + Range(random, 0f, 0.7f);
            deck.AddComponent<MovingPlatform>().ConfigureMotion(Vector3.right * (i % 4 == 0 ? amplitude : -amplitude), duration);
            for (int edge = -1; edge <= 1; edge += 2)
            {
                // Goldene Kanten ohne Collider kennzeichnen bewegliche Flächen.
                var stripe = new GameObject("Moving Edge");
                stripe.transform.SetParent(deck.transform, false);
                stripe.transform.localPosition = new Vector3(0f, 0.505f, edge * 0.46f);
                stripe.transform.localScale = new Vector3(0.95f, 0.02f, 0.07f);
                stripe.AddComponent<MeshFilter>().sharedMesh = deck.GetComponent<MeshFilter>().sharedMesh;
                stripe.AddComponent<MeshRenderer>().sharedMaterial = accentMaterial;
            }
        }
        return end;
    }

    private GameObject AppendDeck(ref Vector3 end, string name, float x, float top, float width, float depth, float gap)
    {
        float z = end.z + gap + depth / 2f;
        GameObject deck = Box(name, new Vector3(x, top - 0.5f, z), new Vector3(width, 1f, depth), platformMaterial);
        end = new Vector3(x, top, z + depth / 2f);
        return deck;
    }

    private GameObject Box(string name, Vector3 position, Vector3 scale, Material material, bool solid = true)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.layer = groundLayer;
        box.transform.SetParent(currentRoot, false);
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        box.GetComponent<BoxCollider>().enabled = solid;
        if (timberDeckMesh != null && (material == floorMaterial || material == platformMaterial))
            box.GetComponent<MeshFilter>().sharedMesh = timberDeckMesh;
        return box;
    }

    private static float Range(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
}
