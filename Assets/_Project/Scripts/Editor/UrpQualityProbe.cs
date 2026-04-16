using UnityEditor.SceneManagement;
using UnityEngine;
using WorldOfVictoria.Core;

public static class UrpQualityProbe
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

    public static string SetQuality(string qualityName)
    {
        EditorSceneManager.OpenScene(GameScenePath);
        var visualManager = Object.FindAnyObjectByType<VisualManager>();
        if (visualManager == null)
        {
            return "VisualManager missing.";
        }

        visualManager.ApplyQuality(qualityName);
        return qualityName;
    }
}
