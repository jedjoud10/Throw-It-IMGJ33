using UnityEngine;

public class UIMaster : MonoBehaviour {
    public IngameHUD inGameHUD;
    public GameObject menu;
    public HealthBar healthBar;

    public static UIMaster Instance;

    // Start is called before the first frame update
    void Start() {
        Instance = this;
    }
}
