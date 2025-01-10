using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public UnityEngine.UI.Image icon;
    public UnityEngine.UI.Button button;
    PieceDefinition definition;

    public void Init(PieceDefinition definition) {
        this.definition = definition;
        icon.sprite = definition.icon;
        button.onClick.AddListener(Thing);
    }

    public void OnPointerExit(PointerEventData eventData) {
        UIMaster.Instance.inGameHUD.buildingMenu.Display(null);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        UIMaster.Instance.inGameHUD.buildingMenu.Display(definition);
    }

    public void Thing() {
        Player.Instance.selectedPiece = definition;
        Player.Instance.SetupPlacementTarget(definition.piecePrefab);
        UIMaster.Instance.ToggleBuilding();
    }
}
