using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeEntry : MonoBehaviour {
    public Button button;
    public TextMeshProUGUI title;
    public CraftingRecipe craftingRecipe;

    public void Init(CraftingRecipe craftingRecipe) {
        this.craftingRecipe = craftingRecipe;

        title.text = craftingRecipe.m_name;
        button.onClick.AddListener(DoWhateverTheFuckTheButtonDoes);
    }

    private void DoWhateverTheFuckTheButtonDoes() {
        // p
    }
}
