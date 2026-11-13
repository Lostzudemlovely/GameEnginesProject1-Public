// Game Engines – Projekt 1
// Bäckt die Alpenkulisse, die Holzoberfläche, die Ballsegmente und das Tornetz im Editor.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class AlpineArt
{
    private const string Art = "Assets/Art/Alpine/";

    public static void Apply()
    {
        Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        Material scenery = MaterialAt("Scenery", "Project1/Alpine Scenery", Color.white);
        Material sky = MaterialAt("Sky", "Project1/Alpine Sky", Color.white);
        Material leather = MaterialAt("Ball Ivory", "Universal Render Pipeline/Lit", Hex("F6EEDB"), 0.32f);
        Material panels = MaterialAt("Ball Ink", "Universal Render Pipeline/Lit", Hex("172F39"), 0.28f);
        Material seams = MaterialAt("Ball Seams", "Universal Render Pipeline/Lit", Hex("696C5E"), 0.16f);
        Material frame = MaterialAt("Goal Frame", "Universal Render Pipeline/Lit", Hex("F4F1DE"), 0.34f);
        Material net = MaterialAt("Goal Net", "Universal Render Pipeline/Unlit", Hex("D7E0D4"));
        net.SetFloat("_Cull", 0f);

        Mesh mountains = SaveMesh(MountainMesh(), "Alpine Basin");
        Mesh timber = SaveMesh(TimberMesh(), "Timber Deck");
        Mesh football = SaveMesh(FootballMesh(), "Football Panels");
        Mesh netting = SaveMesh(NetMesh(), "Goal Netting");
        Texture2D atlas = BakeTimber();
        Material floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Floor.mat");
        Material platform = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Platform.mat");
        Material goalLine = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Goal.mat");
        DressTimber(floor, atlas, Color.white);
        DressTimber(platform, atlas, Hex("EDE2D4"));

        EditPrefab("Player", root => DressPlayer(root, football, leather, panels, seams));
        EditPrefab("Goal", root => DressGoal(root, frame, net, netting, timber, platform, goalLine));

        var game = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        AddScenery(mountains, scenery, sky);
        Camera camera = Camera.main;
        camera.fieldOfView = 58f;
        var follow = new SerializedObject(camera.GetComponent<CameraController>());
        follow.FindProperty("offset").vector3Value = new Vector3(0f, 7f, -11f);
        follow.FindProperty("lookHeight").floatValue = 2.25f;
        follow.ApplyModifiedPropertiesWithoutUndo();
        camera.GetComponent<CameraController>().SnapToTarget();
        var generator = Object.FindFirstObjectByType<LevelGenerator>();
        var settings = new SerializedObject(generator);
        settings.FindProperty("timberDeckMesh").objectReferenceValue = timber;
        settings.ApplyModifiedPropertiesWithoutUndo();
        foreach (MeshRenderer renderer in generator.GetComponentsInChildren<MeshRenderer>(true))
            if (renderer.sharedMaterial == floor || renderer.sharedMaterial == platform)
                renderer.GetComponent<MeshFilter>().sharedMesh = timber;
        foreach (Goal goal in generator.GetComponentsInChildren<Goal>(true))
            if (!PrefabUtility.IsPartOfPrefabInstance(goal)) DressGoal(goal.gameObject, frame, net, netting, timber, platform, goalLine);
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (!PrefabUtility.IsPartOfPrefabInstance(player)) DressPlayer(player.gameObject, football, leather, panels, seams);
        CourseHud();
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);

        var menu = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        AddScenery(mountains, scenery, sky);
        camera = Camera.main;
        camera.transform.position = new Vector3(3f, 9f, -22f);
        camera.transform.LookAt(new Vector3(0f, -6f, 55f));
        camera.fieldOfView = 55f;
        GameObject.Find("Main Menu").GetComponent<Image>().color = new Color(0.023f, 0.062f, 0.077f, 0.26f);
        GameObject.Find("Menu Subtitle").GetComponent<Text>().text = "ALPEN-CUP  ·  SECHS STRECKEN. EIN LAUF.";
        GameObject.Find("Menu Description").GetComponent<Text>().text = "Springe über Holzstege und triff ins Tor.\nWie viele Level schaffst du?";
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);
        AssetDatabase.SaveAssets();
        Debug.Log($"ALPINE_ART_PASSED: terrain {mountains.triangles.Length / 3} triangles; football {football.triangles.Length / 3}; net {netting.triangles.Length / 3}; timber 1024x512.");
    }

    private static void AddScenery(Mesh mesh, Material material, Material sky)
    {
        var old = GameObject.Find("Alpine Basin");
        if (old != null) Object.DestroyImmediate(old);
        RenderPart("Alpine Basin", null, mesh, material, Vector3.zero, Vector3.one, false);
        RenderSettings.skybox = sky;
        Camera.main.clearFlags = CameraClearFlags.Skybox;
        Camera.main.farClipPlane = 265f;
    }

    private static void CourseHud()
    {
        Text level = GameObject.Find("Level Label").GetComponent<Text>();
        Text progress = GameObject.Find("Level Progress").GetComponent<Text>();
        Transform parent = level.transform.parent;
        var old = parent.Find("Course Backplate");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var panel = new GameObject("Course Backplate", typeof(RectTransform), typeof(Image));
        var rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.SetSiblingIndex(Mathf.Min(level.transform.GetSiblingIndex(), progress.transform.GetSiblingIndex()));
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -28f);
        rect.sizeDelta = new Vector2(360f, 76f);
        panel.GetComponent<Image>().color = new Color(0.075f, 0.14f, 0.17f, 0.92f);
        panel.GetComponent<Image>().raycastTarget = false;
        level.color = Hex("F6EEDB");
        progress.color = Hex("BADAD7");
    }

    private static void DressPlayer(GameObject root, Mesh mesh, params Material[] materials)
    {
        Transform old = root.transform.Find("Football Surface");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        root.GetComponent<MeshRenderer>().enabled = false;
        var part = RenderPart("Football Surface", root.transform, mesh, materials[0], Vector3.zero, Vector3.one, true);
        part.layer = root.layer;
        part.GetComponent<MeshRenderer>().sharedMaterials = materials;
        part.transform.localRotation = Quaternion.Euler(16f, 24f, 12f);
        part.AddComponent<FootballVisual>();
    }

    private static void DressGoal(GameObject root, Material frame, Material net, Mesh netting, Mesh timber, Material wood, Material goalLine)
    {
        Transform old = root.transform.Find("Goal Details");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var details = new GameObject("Goal Details").transform;
        details.SetParent(root.transform, false);
        foreach (string part in new[] { "Left Post", "Right Post", "Top Beam" })
            root.transform.Find(part).GetComponent<MeshRenderer>().sharedMaterial = frame;
        Mesh cube = root.transform.Find("Left Post").GetComponent<MeshFilter>().sharedMesh;
        Transform pad = root.transform.Find("Finish Pad");
        pad.GetComponent<MeshFilter>().sharedMesh = timber;
        pad.GetComponent<MeshRenderer>().sharedMaterial = wood;
        RenderPart("Goal Line", details, cube, goalLine, new Vector3(0f, 0.974f, -0.70f), new Vector3(4.50f, 0.008f, 0.14f), false);
        RenderPart("Net", details, netting, net, Vector3.zero, Vector3.one, false);
        foreach (float x in new[] { -2.6f, 2.6f })
        {
            RenderPart("Ground Rail", details, cube, frame, new Vector3(x, 0.99f, 0.69f), new Vector3(0.12f, 0.15f, 1.44f), false);
            var rear = RenderPart("Net Support", details, cube, frame, new Vector3(x, 2.23f, 1.23f), new Vector3(0.09f, 2.64f, 0.09f), false);
            rear.transform.localRotation = Quaternion.Euler(-5.5f, 0f, 0f);
        }
        RenderPart("Net Crossbar", details, cube, frame, new Vector3(0f, 3.55f, 1.10f), new Vector3(5.25f, 0.10f, 0.10f), false);
    }

    private static Texture2D BakeTimber()
    {
        const int size = 512;
        var texture = new Texture2D(size * 2, size, TextureFormat.RGB24, false);
        var pixels = new Color32[size * size * 2];
        Color[] boards = new[] { "B87843", "C5864E", "B47746", "D1965D", "BD8050", "AA6E40", "C58954", "B87A48" }.Select(Hex).ToArray();
        Color joint = Hex("5A3A2C"), edge = Hex("8E593A"), side = Hex("7C543A"), band = Hex("305158"), rim = Hex("AD764D");
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
            int board = Mathf.FloorToInt(u * boards.Length);
            float within = Mathf.Repeat(u * boards.Length, 1f);
            float seam = Mathf.Min(within, 1f - within);
            float stagger = Mathf.Repeat(v * 2f + board * 0.37f, 1f);
            float warp = Mathf.PerlinNoise(u * 17f, v * 3.7f);
            float grain = 0.91f + 0.19f * Mathf.PerlinNoise(u * 105f + warp * 2.7f, v * 7.5f)
                + 0.045f * Mathf.Sin((u * 130f + warp * 2.4f) * Mathf.PI * 2f);
            Color wood = boards[board] * grain;
            wood = Color.Lerp(joint, wood, Mathf.SmoothStep(0f, 1f, Mathf.Min(seam / 0.052f, Mathf.Min(stagger, 1f - stagger) / 0.012f)));
            float border = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            wood = Color.Lerp(edge, wood, Mathf.SmoothStep(0f, 1f, (border - 0.012f) / 0.008f));
            wood.a = 1f;
            pixels[y * size * 2 + x] = wood;

            Color edgeColor = side * (0.86f + 0.25f * Mathf.PerlinNoise(u * 9f, v * 76f));
            if (v > 0.72f && v < 0.91f) edgeColor = Color.Lerp(edgeColor, band, 0.94f);
            if (v >= 0.91f) edgeColor = rim;
            if (v < 0.05f) edgeColor *= 0.68f;
            float bolt = Mathf.Min(Mathf.Abs(u - 0.12f), Mathf.Abs(u - 0.88f));
            if (Mathf.Pow(bolt / 0.009f, 2f) + Mathf.Pow((v - 0.81f) / 0.027f, 2f) < 1f) edgeColor = Hex("B8C4BA");
            edgeColor.a = 1f;
            pixels[y * size * 2 + x + size] = edgeColor;
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        string path = Art + "Timber Atlas.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Mesh TimberMesh()
    {
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        foreach (Vector3 normal in new[] { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
        {
            Vector3 horizontal = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.Cross(Vector3.up, normal);
            Vector3 vertical = Vector3.Cross(normal, horizontal);
            int i = vertices.Count;
            vertices.Add((normal - horizontal - vertical) * 0.5f);
            vertices.Add((normal + horizontal - vertical) * 0.5f);
            vertices.Add((normal + horizontal + vertical) * 0.5f);
            vertices.Add((normal - horizontal + vertical) * 0.5f);
            float u = normal == Vector3.up ? 0.004f : 0.504f;
            uv.AddRange(new[] { new Vector2(u, 0.007f), new Vector2(u + 0.492f, 0.007f), new Vector2(u + 0.492f, 0.993f), new Vector2(u, 0.993f) });
            triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh MountainMesh()
    {
        // x, z, Gipfelhöhe über dem Tal und Ausdehnung. Die mittlere Bahn bleibt frei.
        float[,] peaks = {
            {-51,14,48,39,37}, {-40,38,53,34,42}, {-61,61,57,47,43}, {-30,73,43,29,39},
            {49,8,51,39,41}, {36,38,50,31,36}, {57,56,57,44,42}, {35,78,46,32,43},
            {-83,94,59,53,45}, {82,107,64,54,44}, {-33,120,56,39,49}, {0,135,64,44,48},
            {34,139,55,39,43}, {-61,165,60,52,56}, {67,177,59,52,56}, {-4,188,57,62,47}
        };
        float Height(float x, float z)
        {
            float shape = 0f;
            for (int i = 0; i < peaks.GetLength(0); i++)
            {
                float dx = (x - peaks[i, 0]) / peaks[i, 3], dz = (z - peaks[i, 1]) / peaks[i, 4];
                shape = Mathf.Max(shape, peaks[i, 2] * Mathf.Max(0f, 1f - Mathf.Sqrt(dx * dx + dz * dz)));
            }
            float rough = (Mathf.PerlinNoise(x * 0.08f + 40f, z * 0.09f + 30f) - 0.5f) * 12f;
            return -37f + shape + rough * Mathf.Clamp01(shape / 15f);
        }

        var vertices = new List<Vector3>(); var colors = new List<Color>(); var indices = new List<int>();
        Color stone = Hex("557682"), shadow = Hex("3C5A67"), forest = Hex("446C62");
        Color water = Hex("8EBCC5"), snow = Hex("E5F0EC"), mist = Hex("A8C7CC");
        Vector3 sun = new Vector3(-0.55f, 0.78f, -0.27f).normalized;
        void Facet(Vector3 a, Vector3 b, Vector3 c, Color? tint = null)
        {
            Vector3 mid = (a + b + c) / 3f;
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            float light = Mathf.Clamp01(Vector3.Dot(normal, sun));
            float jitter = Mathf.PerlinNoise(mid.x * 0.37f + 75f, mid.z * 0.29f + 62f);
            Color color = Color.Lerp(shadow, stone, light * 0.8f + jitter * 0.18f);
            if (mid.y < -13f) color = Color.Lerp(color, forest, 0.44f * light);
            if (mid.y > -4f + jitter * 6f && normal.y > 0.41f) color = Color.Lerp(snow, stone, 0.34f * (1f - light));
            if (mid.y < -31f) color = Color.Lerp(color, water * (0.93f + 0.10f * jitter), Mathf.InverseLerp(-31f, -37f, mid.y));
            if (tint.HasValue) color = tint.Value * (0.70f + 0.30f * light);
            color = Color.Lerp(color, mist, Mathf.SmoothStep(0.04f, 0.64f, Mathf.InverseLerp(45f, 190f, mid.z)));
            color.a = 1f;
            int i = vertices.Count;
            vertices.AddRange(new[] { a, b, c }); colors.AddRange(new[] { color.linear, color.linear, color.linear }); indices.AddRange(new[] { i, i + 1, i + 2 });
        }

        Vector3 Point(float x, float z) => new Vector3(x, Height(x, z), z);
        for (int z = -64; z < 216; z += 4)
        for (int x = -168; x < 168; x += 4)
        {
            Vector3 a = Point(x, z), b = Point(x + 4, z), c = Point(x, z + 4), d = Point(x + 4, z + 4);
            if (((x + z) / 4 & 1) == 0) { Facet(a, c, b); Facet(b, c, d); }
            else { Facet(a, d, b); Facet(a, c, d); }
        }

        var random = new System.Random(634);
        for (int i = 0; i < 96; i++)
        {
            float x = random.Next(5, 20) * 4f * (i % 2 == 0 ? -1f : 1f), z = random.Next(-5, 29) * 4f;
            Vector3 root = Point(x, z) - Vector3.up * 0.2f;
            if (root.y < -29f || root.y > -10f) continue;
            float h = Mathf.Lerp(2.1f, 4.5f, (float)random.NextDouble());
            for (int tier = 0; tier < 2; tier++)
            {
                Vector3 foot = root + Vector3.up * h * (tier == 0 ? 0.07f : 0.39f);
                Vector3 tip = root + Vector3.up * h * (tier == 0 ? 0.74f : 1.10f);
                float radius = h * (tier == 0 ? 0.23f : 0.16f);
                for (int k = 0; k < 5; k++)
                {
                    float a = k * Mathf.PI * 0.4f, b = (k + 1) * Mathf.PI * 0.4f;
                    Facet(foot + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius, tip,
                        foot + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * radius, Hex("2D554D"));
                }
            }
        }
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh FootballMesh()
    {
        float g = (1f + Mathf.Sqrt(5f)) / 2f;
        Vector3[] points = {
            new Vector3(-1,g,0), new Vector3(1,g,0), new Vector3(-1,-g,0), new Vector3(1,-g,0),
            new Vector3(0,-1,g), new Vector3(0,1,g), new Vector3(0,-1,-g), new Vector3(0,1,-g),
            new Vector3(g,0,-1), new Vector3(g,0,1), new Vector3(-g,0,-1), new Vector3(-g,0,1)
        };
        int[] faces = { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
        var vertices = new List<Vector3>(); var normals = new List<Vector3>();
        var triangles = new[] { new List<int>(), new List<int>(), new List<int>() };
        Vector3 Edge(int a, int b) => Vector3.Lerp(points[a], points[b], 1f / 3f);
        void Triangle(Vector3 a, Vector3 b, Vector3 c, int group)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f) (b, c) = (c, b);
            foreach (Vector3 p in new[] { a, b, c })
            {
                triangles[group].Add(vertices.Count);
                normals.Add(p.normalized); vertices.Add(p.normalized * 0.5f);
            }
        }
        void Patch(Vector3[] rim, int group)
        {
            Vector3 center = rim.Aggregate(Vector3.zero, (sum, p) => sum + p) / rim.Length;
            for (int k = 0; k < rim.Length; k++)
            {
                Vector3 a = rim[k], b = rim[(k + 1) % rim.Length];
                Vector3 innerA = Vector3.Lerp(center, a, 0.965f), innerB = Vector3.Lerp(center, b, 0.965f);
                const int divisions = 3;
                for (int row = 0; row < divisions; row++)
                for (int col = 0; col <= row; col++)
                {
                    Vector3 At(int r, int c) => center + ((innerA - center) * (r - c) + (innerB - center) * c) / divisions;
                    Triangle(At(row, col), At(row + 1, col), At(row + 1, col + 1), group);
                    if (col < row) Triangle(At(row, col), At(row + 1, col + 1), At(row, col + 1), group);
                }
                for (int segment = 0; segment < divisions; segment++)
                {
                    float f = (float)segment / divisions, n = (float)(segment + 1) / divisions;
                    Vector3 p = Vector3.Lerp(a, b, f), q = Vector3.Lerp(a, b, n);
                    Vector3 r = Vector3.Lerp(innerA, innerB, f), s = Vector3.Lerp(innerA, innerB, n);
                    Triangle(p, q, s, 2); Triangle(p, s, r, 2);
                }
            }
        }
        for (int i = 0; i < faces.Length; i += 3)
        {
            int a = faces[i], b = faces[i + 1], c = faces[i + 2];
            Patch(new[] { Edge(a,b), Edge(b,a), Edge(b,c), Edge(c,b), Edge(c,a), Edge(a,c) }, 0);
        }
        for (int i = 0; i < points.Length; i++)
        {
            int index = i;
            Vector3 normal = points[i].normalized;
            Vector3 axis = Vector3.Cross(normal, Vector3.forward).normalized;
            Vector3 side = Vector3.Cross(normal, axis);
            Vector3[] ring = Enumerable.Range(0, points.Length).Where(j => Mathf.Abs((points[j] - points[index]).sqrMagnitude - 4f) < 0.001f)
                .Select(j => Edge(index, j)).OrderBy(p => Mathf.Atan2(Vector3.Dot(p, side), Vector3.Dot(p, axis))).ToArray();
            Patch(ring, 1);
        }
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.subMeshCount = 3;
        for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
        mesh.RecalculateBounds(); return mesh;
    }

    private static Mesh NetMesh()
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        void Ribbon(Vector3 start, Vector3 end, Vector3 normal)
        {
            Vector3 offset = Vector3.Cross(normal, (end - start).normalized) * 0.015f;
            int i = vertices.Count;
            vertices.AddRange(new[] { start - offset, end - offset, end + offset, start + offset });
            triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }
        void Grid(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int columns, int rows)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            for (int i = 0; i <= columns; i++) Ribbon(Vector3.Lerp(a, b, (float)i / columns), Vector3.Lerp(c, d, (float)i / columns), normal);
            for (int i = 0; i <= rows; i++) Ribbon(Vector3.Lerp(a, c, (float)i / rows), Vector3.Lerp(b, d, (float)i / rows), normal);
        }
        Vector3 fl = new Vector3(-2.58f, 3.89f, 0.12f), fr = new Vector3(2.58f, 3.89f, 0.12f);
        Vector3 tl = new Vector3(-2.58f, 3.55f, 1.10f), tr = new Vector3(2.58f, 3.55f, 1.10f);
        Vector3 bl = new Vector3(-2.58f, 0.97f, 1.35f), br = new Vector3(2.58f, 0.97f, 1.35f);
        Grid(bl, br, tl, tr, 16, 8);
        Grid(fl, fr, tl, tr, 16, 4);
        Grid(new Vector3(-2.58f, 0.97f, 0.12f), bl, fl, tl, 4, 9);
        Grid(new Vector3(2.58f, 0.97f, 0.12f), br, fr, tr, 4, 9);
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); return mesh;
    }

    private static GameObject RenderPart(string name, Transform parent, Mesh mesh, Material material, Vector3 position, Vector3 scale, bool shadows)
    {
        var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = shadows;
        return part;
    }

    private static void DressTimber(Material material, Texture2D atlas, Color tint)
    {
        material.SetTexture("_BaseMap", atlas); material.SetColor("_BaseColor", tint);
        material.SetFloat("_Smoothness", 0.18f); EditorUtility.SetDirty(material);
    }

    private static Mesh SaveMesh(Mesh mesh, string name)
    {
        mesh.name = name;
        string path = Art + name + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); return existing;
    }

    private static Material MaterialAt(string name, string shaderName, Color tint, float smoothness = 0f)
    {
        string path = Art + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find(shaderName);
        if (shader == null) throw new Exception("Shader fehlt: " + shaderName);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EditPrefab(string name, Action<GameObject> edit)
    {
        string path = "Assets/Prefabs/" + name + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color); return color;
    }
}
