using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    private const string UsernameKey = "Username";

    void Awake()
    {
        // Get the root of the UI
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Query for the elements by their assigned IDs
        Button connectButton = root.Q<Button>("connect");
        TextField usernameField = root.Q<TextField>("username");

        // Prepopulate the field if we already have a stored username
        if (usernameField != null)
        {
            if (PlayerPrefs.HasKey(UsernameKey))
                usernameField.value = PlayerPrefs.GetString(UsernameKey);
        }

        // Register click event
        connectButton?.RegisterCallback<ClickEvent>(evt =>
        {
            if (usernameField != null)
            {
                string enteredUsername = usernameField.value;
                PlayerPrefs.SetString(UsernameKey, enteredUsername);
            }

            // Load the ArenaScene
            SceneManager.LoadScene("ArenaScene");
        });
    }
}
