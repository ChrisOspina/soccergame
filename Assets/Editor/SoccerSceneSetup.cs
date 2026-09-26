using Cinemachine;
using StarterAssets;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-shot wiring for the team refactor (AITeammate + TeamController):
// fixes the COM character's stale script, creates both TeamControllers and
// hooks them up to Game, the goals, and each Player.
public static class SoccerSceneSetup
{
    [MenuItem("Soccer/Wire Up Teams")]
    public static void WireUpTeams()
    {
        Game game = Object.FindFirstObjectByType<Game>();
        GameObject human = GameObject.Find("PlayerArmature");
        GameObject com = GameObject.Find("COMPlayer");
        if (game == null || human == null || com == null)
        {
            Debug.LogError("Soccer setup: need GameManager (Game), PlayerArmature and COMPlayer in the open scene.");
            return;
        }

        Player humanPlayer = human.GetComponent<Player>();
        if (humanPlayer == null)
        {
            Debug.LogError("Soccer setup: PlayerArmature has no Player component.");
            return;
        }

        Goal[] goals = Object.FindObjectsByType<Goal>(FindObjectsSortMode.None);
        if (goals.Length != 2)
        {
            Debug.LogError($"Soccer setup: expected 2 Goal components, found {goals.Length}.");
            return;
        }

        // The net nearest the human's kickoff spot is the one the human defends.
        Goal humanOwnGoal = goals[0], comOwnGoal = goals[1];
        if (Distance(human, goals[1]) < Distance(human, goals[0]))
        {
            humanOwnGoal = goals[1];
            comOwnGoal = goals[0];
        }

        // --- COM character: replace the stale script with Player + AITeammate ---
        Undo.RegisterFullObjectHierarchyUndo(com, "Wire Up Teams");
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(com);
        foreach (HumanPlayer stale in com.GetComponents<HumanPlayer>())
            Undo.DestroyObjectImmediate(stale);

        EnsureBallLocation(com.transform);

        AudioSource comAudio = GetOrAdd<AudioSource>(com);
        comAudio.playOnAwake = false;

        Player comPlayer = GetOrAdd<Player>(com);
        comPlayer.mixer = humanPlayer.mixer;
        comPlayer.dribble = humanPlayer.dribble;
        comPlayer.kick = humanPlayer.kick;
        comPlayer.passForce = humanPlayer.passForce;
        comPlayer.shootForce = humanPlayer.shootForce;

        ThirdPersonController tpc = human.GetComponent<ThirdPersonController>();
        LayerMask ground = tpc != null ? tpc.GroundLayers : (LayerMask)1;

        AITeammate comAI = GetOrAdd<AITeammate>(com);
        comAI.groundLayers = ground;
        comAI.enabled = true;

        // Human template needs a (disabled) AITeammate so spawned clones and
        // player-switching have a brain to fall back to.
        AITeammate humanAI = GetOrAdd<AITeammate>(human);
        humanAI.groundLayers = ground;
        humanAI.enabled = false;

        // --- Team controllers ---
        TeamController playerTeam = CreateTeam("PlayerTeam", TeamSide.Player, humanOwnGoal, comOwnGoal, "scoreText", "Player");
        TeamController comTeam = CreateTeam("COMTeam", TeamSide.COM, comOwnGoal, humanOwnGoal, "COMscoreText", "COM");
        playerTeam.vcam = Object.FindFirstObjectByType<CinemachineVirtualCamera>();

        Undo.RecordObject(humanPlayer, "Wire Up Teams");
        humanPlayer.team = playerTeam;
        comPlayer.team = comTeam;

        // A goal counts for the team attacking it.
        Undo.RecordObject(humanOwnGoal, "Wire Up Teams");
        humanOwnGoal.scoringTeam = comTeam;
        Undo.RecordObject(comOwnGoal, "Wire Up Teams");
        comOwnGoal.scoringTeam = playerTeam;

        Undo.RecordObject(game, "Wire Up Teams");
        game.playerTeam = playerTeam;
        game.comTeam = comTeam;
        game.playerTemplate = human;
        game.comTemplate = com;

        EditorUtility.SetDirty(humanPlayer);
        EditorUtility.SetDirty(comPlayer);
        EditorUtility.SetDirty(game);
        EditorUtility.SetDirty(humanOwnGoal);
        EditorUtility.SetDirty(comOwnGoal);
        EditorSceneManager.MarkSceneDirty(com.scene);

        Debug.Log($"Soccer setup done. Player defends '{humanOwnGoal.name}', COM defends '{comOwnGoal.name}'. Save the scene (Ctrl+S).");
    }

    // Unity's fake-null objects break ??, so check explicitly.
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(go);
    }

    private static float Distance(GameObject a, Goal b) =>
        Vector3.Distance(a.transform.position, b.transform.position);

    // Player.Awake looks for "Geometry/BallLocation"; mirror the human rig's placement.
    private static void EnsureBallLocation(Transform root)
    {
        if (root.Find("Geometry/BallLocation") != null) return;

        Transform geometry = root.Find("Geometry");
        if (geometry == null)
        {
            GameObject g = new GameObject("Geometry");
            Undo.RegisterCreatedObjectUndo(g, "Wire Up Teams");
            geometry = g.transform;
            geometry.SetParent(root, false);
        }

        GameObject ballLocation = new GameObject("BallLocation");
        Undo.RegisterCreatedObjectUndo(ballLocation, "Wire Up Teams");
        ballLocation.transform.SetParent(geometry, false);
        ballLocation.transform.localPosition = new Vector3(0.079f, 0.226f, 0.86f);
    }

    private static TeamController CreateTeam(string name, TeamSide side, Goal ownGoal, Goal opponentGoal, string scoreTextName, string label)
    {
        GameObject existing = GameObject.Find(name);
        TeamController team = existing != null ? existing.GetComponent<TeamController>() : null;
        if (team == null)
        {
            GameObject go = existing;
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Wire Up Teams");
            }
            team = Undo.AddComponent<TeamController>(go);
        }

        Undo.RecordObject(team, "Wire Up Teams");
        team.side = side;
        team.ownGoal = ownGoal.transform;
        team.opponentGoal = opponentGoal.transform;
        team.scoreLabel = label;

        GameObject scoreGO = GameObject.Find(scoreTextName);
        if (scoreGO != null) team.scoreText = scoreGO.GetComponent<TMP_Text>();

        EditorUtility.SetDirty(team);
        return team;
    }
}
