using UnityEditor;
using UnityEngine;
using WorldOfVictoria.Rendering.Character;

[InitializeOnLoad]
public static class ZombieModelViewRefreshHook
{
    static ZombieModelViewRefreshHook()
    {
        EditorApplication.delayCall += RefreshZombieViewsInOpenScene;
    }

    private static void RefreshZombieViewsInOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            return;
        }

        var zombieViews = Object.FindObjectsByType<ZombieModelView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var zombieView in zombieViews)
        {
            if (zombieView == null)
            {
                continue;
            }

            zombieView.ForceRefreshInEditor();
            EditorUtility.SetDirty(zombieView.gameObject);
        }
    }
}
