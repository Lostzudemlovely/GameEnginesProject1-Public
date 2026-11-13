// Game Engines – Projekt 1
// Animiert den sichtbaren Fußballschuh zum physikalischen Anstoß.
using UnityEngine;

public sealed class KickoffFoot : MonoBehaviour
{
    [SerializeField] private Material bootMaterial;
    [SerializeField] private Material soleMaterial;
    [SerializeField] private Material sockMaterial;

    private const float FollowThroughTime = 0.48f;
    private static readonly Vector3 Ready = new Vector3(-0.92f, -0.16f, -1.45f);
    private static readonly Vector3 Backswing = new Vector3(-0.94f, 0.22f, -2.15f);
    private static readonly Vector3 Contact = new Vector3(0f, 0.06f, -1.28f);
    private static readonly Vector3 Finish = new Vector3(0.14f, 0.57f, -0.46f);

    private GameObject rig;
    private Transform shoe;
    private Transform sock;
    private Transform cuff;
    private Mesh upperMesh;
    private Mesh soleMesh;
    private PlayerController player;
    private float countdownDuration;
    private float followThrough;
    private bool struck;

    public bool IsVisible => rig != null && rig.activeSelf;

    private void Awake()
    {
        rig = new GameObject("Kickoff Foot");
        rig.transform.SetParent(transform, false);
        shoe = new GameObject("Football Boot").transform;
        shoe.SetParent(rig.transform, false);

        upperMesh = BuildBootMesh(false);
        soleMesh = BuildBootMesh(true);
        AddMesh("Boot Upper", upperMesh, bootMaterial);
        AddMesh("Studded Sole", soleMesh, soleMaterial);
        for (int i = 0; i < 3; i++)
        {
            Transform lace = AddPart(PrimitiveType.Cube, "Lace", shoe, sockMaterial);
            lace.localPosition = new Vector3(0f, 0.177f - i * 0.031f, 0.035f + i * 0.10f);
            lace.localRotation = Quaternion.Euler(17f, 0f, 0f);
            lace.localScale = new Vector3(0.29f, 0.022f, 0.036f);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 2; i++)
            {
                Transform stud = AddPart(PrimitiveType.Cylinder, "Stud", shoe, soleMaterial);
                stud.localPosition = new Vector3(side * 0.16f, -0.30f, -0.12f + i * 0.63f);
                stud.localScale = new Vector3(0.11f, 0.042f, 0.11f);
            }
        }
        sock = AddPart(PrimitiveType.Capsule, "Football Sock", rig.transform, sockMaterial);
        cuff = AddPart(PrimitiveType.Cylinder, "Sock Band", rig.transform, soleMaterial);
        Hide();
    }

    public void Prepare(PlayerController kickerBall, Vector3 target, float delay)
    {
        player = kickerBall;
        countdownDuration = delay;
        followThrough = 0f;
        struck = false;
        Vector3 origin = player.transform.position;
        Vector3 direction = Vector3.ProjectOnPlane(target - origin, Vector3.up);
        rig.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
        rig.SetActive(true);
        Pose(Ready, 8f);
    }

    public void Strike(Vector3 ballPosition)
    {
        rig.transform.position = ballPosition;
        struck = true;
        Pose(Contact, -8f);
    }

    public void Hide()
    {
        rig.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!IsVisible) return;
        if (struck)
        {
            followThrough += Time.deltaTime;
            float t = Mathf.Clamp01(followThrough / FollowThroughTime);
            Pose(Vector3.Lerp(Contact, Finish, 1f - (1f - t) * (1f - t)), Mathf.Lerp(-8f, -34f, t));
            if (t >= 1f) Hide();
            return;
        }

        float progress = 1f - Mathf.Clamp01(player.KickoffCountdown / countdownDuration);
        Vector3 origin = rig.transform.position;
        origin.y = player.transform.position.y;
        rig.transform.position = origin;
        if (progress < 0.68f)
        {
            float t = Mathf.SmoothStep(0f, 1f, progress / 0.68f);
            Pose(Vector3.Lerp(Ready, Backswing, t), Mathf.Lerp(8f, 30f, t));
        }
        else
        {
            float t = Mathf.InverseLerp(0.68f, 1f, progress);
            Pose(Vector3.Lerp(Backswing, Contact, t * t), Mathf.Lerp(30f, -8f, t));
        }
    }

    private void Pose(Vector3 ankle, float pitch)
    {
        shoe.SetLocalPositionAndRotation(ankle, Quaternion.Euler(pitch, 0f, 0f));
        Vector3 knee = new Vector3(ankle.x * 0.45f - 0.32f, 1.19f, -1.77f);
        Vector3 shin = knee - ankle;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, shin);
        sock.SetLocalPositionAndRotation(Vector3.Lerp(ankle, knee, 0.5f), rotation);
        sock.localScale = new Vector3(0.39f, shin.magnitude * 0.51f, 0.39f);
        cuff.SetLocalPositionAndRotation(Vector3.Lerp(ankle, knee, 0.85f), rotation);
        cuff.localScale = new Vector3(0.39f, 0.07f, 0.39f);
    }

    private void AddMesh(string partName, Mesh mesh, Material material)
    {
        var part = new GameObject(partName, typeof(MeshFilter), typeof(MeshRenderer));
        part.transform.SetParent(shoe, false);
        part.GetComponent<MeshFilter>().sharedMesh = mesh;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Transform AddPart(PrimitiveType shape, string partName, Transform parent, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = partName;
        part.transform.SetParent(parent, false);
        Collider collider = part.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        return part.transform;
    }

    private static Mesh BuildBootMesh(bool sole)
    {
        // Jede Querschnittsebene enthält Länge, halbe Breite und Höhe des Schuhs.
        var sections = new[]
        {
            new Vector3(-0.32f, 0.15f, 0.05f), new Vector3(-0.18f, 0.24f, 0.20f),
            new Vector3(0.12f, 0.27f, 0.15f), new Vector3(0.52f, 0.28f, 0.04f),
            new Vector3(0.81f, 0.17f, -0.07f)
        };
        const int sides = 6;
        var vertices = new Vector3[sections.Length * sides];
        var triangles = new System.Collections.Generic.List<int>();
        for (int i = 0; i < sections.Length; i++)
        {
            float z = sections[i].x;
            float w = sections[i].y * (sole ? 1.05f : 1f);
            float bottom = sole ? -0.29f : -0.23f;
            float top = sole ? -0.22f : sections[i].z;
            float middle = Mathf.Lerp(bottom, top, 0.43f);
            int start = i * sides;
            vertices[start] = new Vector3(-w * 0.8f, bottom, z);
            vertices[start + 1] = new Vector3(-w, middle, z);
            vertices[start + 2] = new Vector3(-w * 0.7f, top, z);
            vertices[start + 3] = new Vector3(w * 0.7f, top, z);
            vertices[start + 4] = new Vector3(w, middle, z);
            vertices[start + 5] = new Vector3(w * 0.8f, bottom, z);
            if (i == 0) continue;
            for (int side = 0; side < sides; side++)
            {
                int a = start - sides + side;
                int b = start - sides + (side + 1) % sides;
                triangles.Add(a); triangles.Add(b + sides); triangles.Add(b);
                triangles.Add(a); triangles.Add(a + sides); triangles.Add(b + sides);
            }
        }
        int front = vertices.Length - sides;
        for (int i = 1; i < sides - 1; i++)
        {
            triangles.Add(0); triangles.Add(i); triangles.Add(i + 1);
            triangles.Add(front); triangles.Add(front + i + 1); triangles.Add(front + i);
        }
        var mesh = new Mesh { name = sole ? "Football Boot Sole" : "Football Boot Upper" };
        mesh.vertices = vertices;
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    private void OnDestroy()
    {
        Destroy(upperMesh);
        Destroy(soleMesh);
    }
}
