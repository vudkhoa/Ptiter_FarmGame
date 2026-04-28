using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneToolbarUtilities
{
    private const string kElementPath = "SceneSwitcher/Scene";

    private static string[] _scenePaths;
    private static string[] _sceneNames;

    [MainToolbarElement(kElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement CreateSceneDropdown()
    {
        RefreshSceneList();

        var activeSceneName = SceneManager.GetActiveScene().name;

        if (string.IsNullOrEmpty(activeSceneName))
            activeSceneName = "Untitled";

        var content = new MainToolbarContent(activeSceneName);
        return new MainToolbarDropdown(content, ShowDropdownMenu);
    }

    private static void ShowDropdownMenu(Rect dropDownRect)
    {
        RefreshSceneList();

        var menu = new GenericMenu();
        var currentScene = SceneManager.GetActiveScene().name;

        for (var i = 0; i < _sceneNames.Length; i++)
        {
            var index = i;
            var isActive = _sceneNames[i] == currentScene;
            menu.AddItem(new GUIContent(_sceneNames[i]), isActive, () =>
            {
                SwitchScene(index);
                MainToolbar.Refresh(kElementPath);
            });
        }

        if (_sceneNames.Length == 0)
            menu.AddDisabledItem(new GUIContent("No scenes in Build Settings"));

        menu.DropDown(dropDownRect);
    }

    private static void SwitchScene(int index)
    {
        if (index < 0 || index >= _scenePaths.Length) return;

        var scenePath = _scenePaths[index];

        if (Application.isPlaying)
        {
            var sceneName = _sceneNames[index];
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }
        else
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
    }

    private static void RefreshSceneList()
    {
        List<string> scenePaths = new();
        List<string> sceneNames = new();

        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled || scene.path == null || !scene.path.StartsWith("Assets"))
                continue;
            scenePaths.Add(scene.path);
            sceneNames.Add(Path.GetFileNameWithoutExtension(scene.path));
        }

        _scenePaths = scenePaths.ToArray();
        _sceneNames = sceneNames.ToArray();
    }
}
