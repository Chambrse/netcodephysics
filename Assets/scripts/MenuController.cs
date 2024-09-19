using System.Collections;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private GameObject _debugOptionsMenu;

    private PlayerControls _playerControls;
    private bool _menuActive = false;

    // Initialize method to pass PlayerControls from GetPlayerInputSystem
    public void Initialize(PlayerControls playerControls)
    {
        _playerControls = playerControls;
        _playerControls.Hover.ToggleMenu.performed += ctx => ToggleMenu();

    }

    private void OnEnable()
    {
        // Set up the pause menu controller
        var pauseMenuController = _pauseMenu.GetComponent<pauseMenuController>();
        pauseMenuController._debugOptionsMenu = _debugOptionsMenu;
    }

    private void OnDisable()
    {
        // Ensure PlayerControls is set before unsubscribing from events
        if (_playerControls != null)
        {
            _playerControls.Hover.ToggleMenu.performed -= ctx => ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        if (_menuActive)
        {
            _pauseMenu.SetActive(false);
            _debugOptionsMenu.SetActive(false);
            _menuActive = false;
        }
        else
        {
            _pauseMenu.SetActive(true);
            _menuActive = true;
        }
    }

    void Update()
    {
        // Update logic for menu navigation or other checks can go here
    }
}
