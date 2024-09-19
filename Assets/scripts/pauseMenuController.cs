#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

using System;
using UnityEngine.UIElements;
using Unity.VisualScripting;
public class pauseMenuController : MonoBehaviour
{
    private Button _quitButton;
    private Button _debugButton;

    public GameObject _debugOptionsMenu;
    //Add logic that interacts with the UI controls in the `OnEnable` methods
    private void OnEnable()
    {
        // The UXML is already instantiated by the UIDocument component
        var uiDocument = GetComponent<UIDocument>();

        // get the button labeled quit
        _quitButton = uiDocument.rootVisualElement.Q("Quit") as Button;
        _debugButton = uiDocument.rootVisualElement.Q("DebugOptions") as Button;
        _quitButton.RegisterCallback<ClickEvent>(OnQuitClick);
        _debugButton.RegisterCallback<ClickEvent>(OnDebugClick);
        _quitButton.RegisterCallback<NavigationSubmitEvent>(OnQuit);
    //     _quitButton.RegisterCallback<NavigationMoveEvent>(OnMove);
    }

    // private void OnMove(NavigationMoveEvent evt)
    // {

    //     Debug.Log($"Move: {evt.direction}");
    // }

    private void OnDebugClick(ClickEvent evt)
    {
        _debugOptionsMenu.SetActive(true);
        //deactivate self
        gameObject.SetActive(false);
        Debug.Log("Debug Button Clicked");
    }

    private void OnQuitClick(ClickEvent evt)
    {
        StopGame();
    }

    private void OnQuit(NavigationSubmitEvent evt)
    {
        StopGame();
    }

    private void OnDisable()
    {
        _quitButton.UnregisterCallback<NavigationSubmitEvent>(OnQuit);
        _quitButton.UnregisterCallback<ClickEvent>(OnQuitClick);
    }

    public static void InputMessage(ChangeEvent<string> evt)
    {
        Debug.Log($"{evt.newValue} -> {evt.target}");
    }

    void StopGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
