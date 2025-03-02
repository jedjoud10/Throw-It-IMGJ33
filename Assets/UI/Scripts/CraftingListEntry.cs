// simple icon and name thingerator

using UnityEngine;
using UnityEngine.UI;

public class CraftingListEntry : MonoBehaviour {
    public Image image;
    public Text title;
    public CraftingRecipe recipe;

    public void Initialize(CraftingRecipe recipe) {
        image.sprite = recipe.output.Data.icon;
        title.text = recipe.output.Data.name;
        this.recipe = recipe;
    }

    public void Refresh(bool active) {
        title.color = active ? Color.white : Color.gray;
    }
}