using System.Collections.Generic;
using UnityEngine;

public class HotbarUI : MonoBehaviour {
    public VisualSlot[] slots;
    public readonly Color deselected = new Color(0f, 0f, 0f, .73f);
    public readonly Color selected = new Color(.1f, .1f, .1f, .73f);

    // Start is called before the first frame update
    void Start() {
        if (Player.Instance != null) {
            Player.Instance.inventory.selectedEvent?.AddListener(Select);
            Select(0); // might wanna make this a saved and loaded value (save scum maxxing)
            Player.Instance.inventory.container.onUpdate.AddListener(Refresh);
        }
    }

    // This code kinda sucks but it's okay
    void Refresh(List<ItemStack> items) {
        int i = 0;
        foreach(VisualSlot slot in slots) {
            slot.Refresh(items[i]);
            i++;
        }
    }

    public void Select(int slot) {
        foreach(VisualSlot _slot in slots) {
            _slot.background.color = deselected;
        }

        slots[slot].background.color = selected;
    }
}
