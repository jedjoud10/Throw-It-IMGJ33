using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerBehaviour : MonoBehaviour {
    [HideInInspector]
    public Player player;

    [HideInInspector]
    public PlayerControlsSettings settings;

    protected bool Pressed(InputAction.CallbackContext context) {
        return player.Pressed(context);
    }

    protected bool Performed(InputAction.CallbackContext context) {
        return player.Performed(context);
    }
}
