using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerInteraction : MonoBehaviour
{
    private InputAction _interactAction;

    // Set while the player's trigger collider is actually overlapping a Door.
    private bool _inDoorTrigger;

    private void Awake()
    {
        _interactAction = GetComponent<PlayerInput>().actions["Interact"];
    }

    private void OnEnable()
    {
        _interactAction.performed += OnInteractPerformed;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        _interactAction.performed -= OnInteractPerformed;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // The player persists across scene loads (DontDestroyOnLoad), but a room's Door collider
    // does not - if the player is still overlapping it when the old scene is unloaded, Unity
    // never calls OnTriggerExit for it, leaving _inDoorTrigger stuck true in the new scene.
    // Force it back to false on every scene load so a stale flag can't open the map from
    // nowhere near a door.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _inDoorTrigger = false;
    }

    // Fires once on press (not every frame it's held), so there's no leftover state to clear
    // if the player presses Interact somewhere that isn't a door.
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (_inDoorTrigger && MinimapManager.Instance != null)
        {
            MinimapManager.Instance.ToggleMinimap(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Door")) _inDoorTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Door")) _inDoorTrigger = false;
    }
}
