// Game Engines – Projekt 1
// Repräsentative native Physikprüfungen und begrenzte Layoutprüfungen, keine vollständigen Spieltests.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PhysicsChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static void Run()
    {
        foreach (int seed in new[] { 21731, 0, -129, 912233 })
        {
            VerifyLayouts(seed);
            VerifyKickoff(seed, 1, false);
            VerifyKickoff(seed, 2, false);
            VerifyKickoff(seed, 2, true);
            VerifyKickoff(seed, 4, false);
            VerifyKickoff(seed, 4, true);
            VerifyKickoff(seed, 13, false);
            VerifyKickoff(seed, 13, true);
        }
        VerifyVariableJump();
        VerifyWorstJump();
        VerifyMovingSupport();
    }
    private static void VerifyLayouts(int seed)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var generator = Object.FindFirstObjectByType<LevelGenerator>();
        var manager = Object.FindFirstObjectByType<GameManager>();
        Set(generator, "seed", seed);
        Set(generator, "randomizeSeedEachRun", false);
        generator.BeginRun();
        Require(generator.RunSeed == seed, "Fixed seed not respected.");
        string first = null;
        var fingerprints = new System.Collections.Generic.HashSet<string>();
        CourseType previous = (CourseType)(-1);
        var bag = new System.Collections.Generic.HashSet<CourseType>();
        var signature = new System.Collections.Generic.List<string>();
        int mixed = 0;
        for (int index = 1; index <= 122; index++)
        {
            int level = index == 121 ? 1000 : index == 122 ? 1000000 : index;
            ClearEditRoot(generator);
            generator.BuildLevel(level, manager);
            Transform root = (Transform)Get(generator, "currentRoot");
            CourseType type = generator.CurrentCourseType;
            CourseType? second = generator.SecondCourseType;
            if (second.HasValue)
            {
                Require(level >= 4 && second != type, "Invalid secondary course type.");
                if (level <= 120) mixed++;
            }
            Require(level != 1 || type == CourseType.Islands || type == CourseType.Zigzag || type == CourseType.Balance,
                "First level does not have an introductory course.");
            if (index <= 120)
            {
                Require(type != previous, "Consecutive course type repeated at " + level);
                previous = type;
                Require(bag.Add(type), "Duplicate type inside six-level bag.");
                if (level % 6 == 0) { Require(bag.Count == 6, "Missing type."); bag.Clear(); }
            }
            var children = root.Cast<Transform>().ToArray();
            var rotors = root.GetComponentsInChildren<RotatingObstacle>();
            var movers = root.GetComponentsInChildren<MovingPlatform>();
            Require((type == CourseType.Rotors || second == CourseType.Rotors) == (rotors.Length >= 2), "Rotor course silhouette invalid.");
            Require((type == CourseType.Moving || second == CourseType.Moving) == (movers.Length >= 2), "Moving course silhouette invalid.");
            Require((type == CourseType.Balance || second == CourseType.Balance) == children.Any(t => t.name.StartsWith("Balance Beam")), "Balance silhouette invalid.");
            Require((type == CourseType.Slalom || second == CourseType.Slalom) == children.Any(t => t.name == "Slalom Runway"), "Slalom silhouette invalid.");
            foreach (var rotor in rotors)
            {
                float speed = (float)Get(rotor, "rotationSpeed");
                Require(Mathf.Abs(speed) >= 45f && Mathf.Abs(speed) <= 100f, "Unbounded rotor speed.");
            }
            foreach (var mover in movers)
            {
                Require(mover.GetComponent<Rigidbody>().isKinematic && mover.gameObject.layer == 8, "Moving floor not kinematic Ground.");
                Require(((Vector3)Get(mover, "travelOffset")).magnitude <= 1.201f, "Unbounded moving amplitude.");
            }
            Require((root.Find("Kickoff Side Landing") != null) == (level > 1), "Wrong number of kickoff pads.");
            Require(generator.RequiresKickoffSteering == (level >= 4), "Unexpected kickoff assistance.");
            Require(children.Where(t => t.name == "Landing Mark").All(t => !t.GetComponent<Collider>().enabled),
                "A painted landing marker has a collider.");
            Transform start = root.Find(level == 1 ? "Kickoff Landing" : "Kickoff Approach");
            float front = start.position.z + start.localScale.z / 2f;
            float x = start.position.x, top = start.position.y + start.localScale.y / 2f;
            var route = children.Where(t => t.position.z > front && t.TryGetComponent(out BoxCollider collider) &&
                collider.enabled && !collider.isTrigger && !t.name.StartsWith("Balance Beam") &&
                !t.name.StartsWith("Slalom Wall")).OrderBy(t => t.position.z - t.localScale.z / 2f).ToArray();
            foreach (Transform step in route)
            {
                float gap = step.position.z - step.localScale.z / 2f - front;
                float nextTop = step.position.y + step.localScale.y / 2f;
                bool connectedByBeam = step.name.StartsWith("Rest Island ") && step.name != "Rest Island 0";
                bool flushRunway = step.name == "Slalom Runway" && Mathf.Abs(gap) < 0.01f;
                Require(connectedByBeam || flushRunway || (gap >= 0.79f && gap <= 1.71f),
                    "Gap outside tested bounds: " + level + " / " + step.name);
                Require((connectedByBeam || Mathf.Abs(step.position.x - x) <= 4.001f) && nextTop - top <= 0.301f,
                    "Untested jump offset/rise.");
                Require(step.localScale.x >= 3.89f && step.gameObject.layer == 8, "Floor width/layer invalid.");
                front = step.position.z + step.localScale.z / 2f; x = step.position.x; top = nextTop;
            }
            Require(root.GetComponentsInChildren<Goal>().Length == 1, "Goal count invalid.");
            Require(root.GetComponentsInChildren<Collider>().All(c => !c.isTrigger || c.GetComponent<Goal>() != null), "Unexpected trigger.");
            string fingerprint = Layout(root);
            if (level == 1) first = fingerprint;
            if (level <= 120) fingerprints.Add(fingerprint);
            if (level <= 6) signature.Add(level + ": " + generator.CourseName);
        }
        Require(fingerprints.Count == 120, "First 120 level layouts are not distinct.");
        Require(mixed > 0 && mixed < 117, "A run should contain both mixed and single courses.");
        ClearEditRoot(generator);
        generator.BeginRun();
        generator.BuildLevel(1, manager);
        Require(Layout((Transform)Get(generator, "currentRoot")) == first, "Same seed/level is not repeatable.");
        ClearEditRoot(generator);
        Set(generator, "randomizeSeedEachRun", true);
        generator.BeginRun();
        int newSeed = generator.RunSeed;
        generator.BuildLevel(1, manager);
        Require(Layout((Transform)Get(generator, "currentRoot")) != first, "New seed reproduced old opening.");
        generator.BeginRun();
        Require(generator.RunSeed != newSeed, "Randomized restart did not select a fresh seed.");
        File.WriteAllLines("Logs/JumpCourseOrder.txt", signature);
        Debug.Log("JUMP_LAYOUTS_PASSED: seed=" + seed + "; 120 distinct layouts, " + mixed +
            " mixed courses, all six main types per bag, bounded routes and fixed/random restart. " + string.Join(", ", signature));
    }

    private static void VerifyKickoff(int seed, int level, bool steerSide)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var generator = Object.FindFirstObjectByType<LevelGenerator>();
        var manager = Object.FindFirstObjectByType<GameManager>();
        var player = Object.FindFirstObjectByType<PlayerController>();
        Set(generator, "seed", seed);
        ClearEditRoot(generator);
        generator.BuildLevel(level, manager);
        Transform root = (Transform)Get(generator, "currentRoot");
        Transform pad = root.Find(steerSide ? "Kickoff Side Landing" : "Kickoff Landing");
        Vector2 input = steerSide || generator.RequiresKickoffSteering
            ? Vector2.right * Mathf.Sign(pad.position.x - generator.KickoffTarget.x)
            : Vector2.zero;
        Call(player, "Awake");
        player.Respawn();
        Rigidbody body = player.GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.None;
        SimulationMode before = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        try
        {
            int launches = 0;
            player.KickedOff += _ => launches++;
            player.PrepareKickoff(generator.KickoffTarget, 0.2f, generator.RequiresKickoffSteering);
            Step(player, 5, input);
            Require(launches == 0 && player.IsWaitingForKickoff && Mathf.Abs(body.position.x) < 0.01f,
                "Input moved the ball before the kickoff.");
            for (int step = 0; step < 120 && (player.IsWaitingForKickoff || player.IsKickoffFlight); step++)
                Step(player, 1, input);
            Step(player, 18, Vector2.zero);
            Require(launches == 1 && !player.IsKickoffFlight &&
                Physics.Raycast(body.position, Vector3.down, out RaycastHit hit, 0.57f, 1 << 8) &&
                hit.collider == pad.GetComponent<Collider>(),
                "Kickoff missed " + pad.name + ": seed=" + seed + ", level=" + level + ", position=" + body.position);
            Debug.Log("KICKOFF_PASSED: seed=" + seed + ", level=" + level + ", landed on " + pad.name + ".");
        }
        finally { Physics.simulationMode = before; }
    }

    private static void VerifyWorstJump()
    {
        foreach (Vector3 test in new[] { new Vector3(4f, 1.2f, 0.25f), new Vector3(1.2f, 1.7f, 0.3f), new Vector3(2.7f, 1.3f, 0.25f) })
        {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Box(new Vector3(0f, -0.5f, 0f), new Vector3(4f, 1f, 6f));
        float targetZ = 3f + test.y + 1.75f;
        Box(new Vector3(test.x, test.z - 0.5f, targetZ), new Vector3(4f, 1f, 3.5f));
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        var player = Object.Instantiate(source).GetComponent<PlayerController>();
        Rigidbody body = player.GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.None;
        body.position = new Vector3(0f, 0.5f, 2.25f);
        Call(player, "Awake");
        SimulationMode before = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        Physics.SyncTransforms();
        try
        {
            Step(player, 20, Vector2.zero);
            Require((bool)Call(player, "IsGrounded"), "Ground detection regression.");
            Vector3 direction = new Vector3(test.x, 0f, targetZ - 2.25f).normalized;
            Vector2 input = new Vector2(direction.x, direction.z);
            body.linearVelocity = direction * 6f;
            Set(player, "jumpHeld", true);
            Set(player, "jumpRequested", true);
            Step(player, 1, input);
            float upward = body.linearVelocity.y;
            Set(player, "jumpRequested", true);
            Step(player, 1, input);
            Require(body.linearVelocity.y < upward, "Air jump was accepted.");
            Step(player, 42, input);
            Step(player, 90, Vector2.zero);
            Require((bool)Call(player, "IsGrounded") && body.position.z > 3f + test.y && Mathf.Abs(body.position.y - (0.5f + test.z)) < 0.07f, "Gap/side step/rise cannot be crossed: " + test + " -> " + body.position);
            int falls = 0;
            player.Fell += () => falls++;
            body.position = new Vector3(9f, -5.2f, 0f);
            Physics.SyncTransforms();
            Step(player, 5, Vector2.one);
            Require(falls == 1 && body.position.y < -5f && body.isKinematic,
                "Fall did not stop player exactly once without auto-respawn: count=" + falls + ", position=" + body.position + ", constraints=" + body.constraints);
            Debug.Log("JUMP_PHYSICS_PASSED: dx/gap/rise=" + test + " reachable; air jump blocked and fall freezes body.");
        }
        finally { Physics.simulationMode = before; }
        }
    }

    private static void VerifyMovingSupport()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Box(new Vector3(0f, -0.5f, 0f), new Vector3(4.2f, 1f, 3.5f));
        GameObject deck = Object.FindFirstObjectByType<BoxCollider>().gameObject;
        Rigidbody floor = deck.AddComponent<Rigidbody>();
        floor.useGravity = false; floor.isKinematic = true;
        floor.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        var moving = deck.AddComponent<MovingPlatform>();
        Call(moving, "Awake");
        moving.ConfigureMotion(Vector3.right * 1.2f, 3.5f);
        var player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab")).GetComponent<PlayerController>();
        Rigidbody body = player.GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.None;
        body.position = new Vector3(0f, 0.5f, 0f);
        Call(player, "Awake");
        SimulationMode before = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        Physics.SyncTransforms();
        float maxDrift = 0f;
        try
        {
            for (int i = 0; i < 525; i++)
            {
                Call(moving, "FixedUpdate");
                Step(player, 1, Vector2.zero);
                maxDrift = Mathf.Max(maxDrift, Mathf.Abs(body.position.x - floor.position.x));
                Require(Mathf.Abs(floor.position.x) <= 1.201f, "Moving platform escaped amplitude.");
                Require(body.position.y > 0.45f && (bool)Call(player, "IsGrounded"), "Player fell off moving floor.");
            }
            Require(maxDrift < 0.4f, "Moving floor did not carry idle player: " + maxDrift);
            Set(player, "jumpRequested", true);
            Call(moving, "FixedUpdate");
            Set(player, "jumpHeld", true);
            Step(player, 1, Vector2.zero);
            Require(body.linearVelocity.y > 5f, "Cannot jump from moving floor.");
            Debug.Log("JUMP_MOVING_PASSED: 10.5 seconds / three cycles, idle player carried through reversals; max relative drift=" + maxDrift + "; jump works.");
        }
        finally { Physics.simulationMode = before; }
    }

    private static void VerifyVariableJump()
    {
        float[] heights = new float[4];
        int[] holdSteps = { 0, 3, 10, 120 };
        SimulationMode before = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        try
        {
            for (int test = 0; test < holdSteps.Length; test++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Box(new Vector3(0f, -0.5f, 0f), new Vector3(200f, 1f, 200f));
                var player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab")).GetComponent<PlayerController>();
                Rigidbody body = player.GetComponent<Rigidbody>();
                body.interpolation = RigidbodyInterpolation.None;
                body.position = new Vector3(0f, 0.5f, 0f);
                Call(player, "Awake");
                Physics.SyncTransforms();
                Step(player, 15, Vector2.zero);
                Require((bool)Call(player, "IsGrounded"), "Hop test did not start grounded.");
                Set(player, "jumpRequested", true);
                for (int i = 0; i < 160; i++)
                {
                    Set(player, "jumpHeld", i < holdSteps[test]);
                    Step(player, 1, Vector2.zero);
                    heights[test] = Mathf.Max(heights[test], body.position.y - 0.5f);
                }
                Require((bool)Call(player, "IsGrounded") && Mathf.Abs(body.position.y - 0.5f) < 0.05f, "Held jump auto-repeated or failed to land.");
                Debug.Log("JUMP_HEIGHT: held=" + holdSteps[test] * Time.fixedDeltaTime + "s, apex=" + heights[test] + "m");

                if (test != 3) continue;
                // Release after the apex must not change downward speed.
                Set(player, "jumpRequested", true);
                Set(player, "jumpHeld", true);
                Step(player, 35, Vector2.zero);
                float downward = body.linearVelocity.y;
                Require(downward < 0f, "Late-release check is not falling.");
                Set(player, "jumpHeld", false);
                Step(player, 1, Vector2.zero);
                Require(Mathf.Abs(body.linearVelocity.y - (downward + Physics.gravity.y * Time.fixedDeltaTime)) < 0.02f, "Releasing during fall changed gravity.");
                Step(player, 60, Vector2.zero);

                // Only the first release cuts; repressing in air cannot add another jump.
                Set(player, "jumpRequested", true);
                Set(player, "jumpHeld", true);
                Step(player, 3, Vector2.right);
                float horizontal = body.linearVelocity.x;
                Set(player, "jumpHeld", false);
                Set(player, "movementInput", Vector2.right);
                Call(player, "FixedUpdate");
                Require(body.linearVelocity.x == horizontal && body.linearVelocity.y > 0f, "Release modified horizontal speed.");
                Physics.Simulate(Time.fixedDeltaTime);
                float cut = body.linearVelocity.y;
                Step(player, 1, Vector2.right);
                Require(Mathf.Abs(body.linearVelocity.y - (cut + Physics.gravity.y * Time.fixedDeltaTime)) < 0.02f, "Release cut jump more than once.");
                float airborne = body.linearVelocity.y;
                Set(player, "jumpRequested", true);
                Set(player, "jumpHeld", true);
                Step(player, 1, Vector2.right);
                Require(body.linearVelocity.y < airborne, "Air repress added a jump.");
                player.SetMovementEnabled(false);
                Require(!(bool)Get(player, "canCutJump") && !(bool)Get(player, "jumpHeld") && !(bool)Get(player, "jumpRequested"), "Disabled player kept stale jump state.");
                player.Respawn();
                player.SetMovementEnabled(true);
                Require(!(bool)Get(player, "canCutJump") && !(bool)Get(player, "jumpHeld"), "Respawn retained jump state.");
            }
            Require(heights[0] > 0.3f && heights[0] < 0.6f, "Short tap height outside expected range.");
            Require(heights[0] < heights[1] && heights[1] < heights[2] && heights[2] < heights[3], "Holding longer did not increase height.");
            Require(heights[3] > 1.6f && heights[3] < 1.9f, "Full jump no longer matches existing impulse.");
            File.WriteAllText("Logs/VariableJumpHeights.txt", string.Join("\n", heights.Select((height, i) => "Hold " + holdSteps[i] * Time.fixedDeltaTime + "s: " + height + "m")));
            Debug.Log("JUMP_VARIABLE_PASSED: short press before physics, progressive hold heights, unchanged full jump/falling/horizontal speed, no air jump or repeated cut, reset clears state.");
        }
        finally { Physics.simulationMode = before; }
    }

    private static void ClearEditRoot(LevelGenerator generator)
    {
        Transform root = (Transform)Get(generator, "currentRoot");
        if (root != null) Object.DestroyImmediate(root.gameObject);
        Set(generator, "currentRoot", null);
    }
    private static string Layout(Transform root) => string.Join("|", root.Cast<Transform>().Select(t => t.name + t.position.ToString("F3") + t.localScale.ToString("F3") + t.rotation.ToString("F3")));
    private static void Box(Vector3 position, Vector3 scale)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.layer = 8;
        box.transform.position = position;
        box.transform.localScale = scale;
    }
    private static void Step(PlayerController player, int count, Vector2 input)
    {
        for (int i = 0; i < count; i++) { Set(player, "movementInput", input); Call(player, "FixedUpdate"); Physics.Simulate(Time.fixedDeltaTime); }
    }
    private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    private static object Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
