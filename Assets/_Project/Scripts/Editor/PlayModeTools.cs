using UnityEditor;

public static class PlayModeTools
{
    public static string EnterPlayMode()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = true;
            return "Entering play mode.";
        }

        return "Already in play mode.";
    }

    public static string ExitPlayMode()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return "Exiting play mode.";
        }

        return "Already out of play mode.";
    }
}
