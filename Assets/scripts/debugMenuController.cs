using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using System.Reflection;

public class DebugMenuController : MonoBehaviour
{
    private VisualElement _rootVisualElement;
    private Toggle _debugModeToggle;
    private EntityManager _entityManager;
    // OnEnable is called when the object becomes enabled and active
    private void OnEnable()
    {
        // Get the UIDocument component from the same GameObject
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null)
        {
            // Store the root visual element for easier access
            _rootVisualElement = uiDocument.rootVisualElement;

            // Query all toggles in the UI (assuming all toggles have the class "toggle")
            var toggles = _rootVisualElement.Query<Toggle>().ToList();
            _debugModeToggle = _rootVisualElement.Q<Toggle>("DebugMode");

            // Loop through each toggle and register a callback for value changes
            foreach (var toggle in toggles)
            {
                string toggleKey = toggle.label;  // Use the label as the PlayerPrefs key
                toggle.value = PlayerPrefs.GetInt(toggleKey, 0) == 1; // Load the saved value
                toggle.RegisterCallback<ChangeEvent<bool>>(evt => OnToggleValueChanged(evt, toggleKey));
            }
        }
        else
        {
            Debug.LogWarning("UIDocument not found on the GameObject.");
        }

        // Get the EntityManager
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    // OnDisable is called when the object becomes disabled or inactive
    private void OnDisable()
    {
        // Unregister all callbacks from toggles to avoid memory leaks
        var toggles = _rootVisualElement?.Query<Toggle>().ToList();
        if (toggles != null)
        {
            foreach (var toggle in toggles)
            {
                toggle.UnregisterCallback<ChangeEvent<bool>>(evt => OnToggleValueChanged(evt, toggle.text));
            }
        }

        // Always update DebugSettings on menu close
        UpdateDebugSettings();
    }

    // Callback for when any toggle value changes
    private void OnToggleValueChanged(ChangeEvent<bool> evt, string toggleKey)
    {
        // Save the toggle state in PlayerPrefs using the toggleKey (label of the toggle)
        PlayerPrefs.SetInt(toggleKey, evt.newValue ? 1 : 0);
        PlayerPrefs.Save();

        // Optional: Debug message to verify which toggle was changed and its new state
        Debug.Log($"Toggle '{toggleKey}' changed to: {evt.newValue}");
    }

    private void UpdateDebugSettings()
    {
        // Check if the DebugSettings entity already exists
        EntityQuery query = _entityManager.CreateEntityQuery(typeof(DebugSettings));
        Entity debugSettingsEntity = query.GetSingletonEntity();

        // Get the current DebugSettings component data
        var debugSettings = _entityManager.GetComponentData<DebugSettings>(debugSettingsEntity);

        // Query all toggles and update their corresponding DebugSettings field
        var toggles = _rootVisualElement.Query<Toggle>().ToList();

        foreach (var toggle in toggles)
        {
            string toggleLabel = toggle.label;

            // Use reflection to dynamically set the matching field in DebugSettings
            FieldInfo field = typeof(DebugSettings).GetField(toggleLabel, BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValueDirect(__makeref(debugSettings), toggle.value);
            }
            else
            {
                Debug.LogWarning($"No matching field for {toggleLabel} found in DebugSettings.");
            }
        }

        // Set DebugMode based on the toggle value
        debugSettings.DebugMode = _debugModeToggle != null && _debugModeToggle.value;

        // Update the DebugSettings component on the entity
        _entityManager.SetComponentData(debugSettingsEntity, debugSettings);
    }
}