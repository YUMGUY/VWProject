using UnityEngine;
using StarterAssets;

public class PlayerInteraction : MonoBehaviour
{
    private StarterAssetsInputs _input;

    private void Awake() 
    { 
        _input = GetComponent<StarterAssetsInputs>(); 
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Door") && _input.interact)
        {
            _input.interact = false; 
            if (MinimapManager.Instance != null)
            {
                MinimapManager.Instance.ToggleMinimap(true);
            }
        }
    }
}
