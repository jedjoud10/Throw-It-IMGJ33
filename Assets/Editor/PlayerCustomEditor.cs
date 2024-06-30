using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEngine;

[CustomEditor(typeof(PlayerScript))]
public class PlayerCustomEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        PlayerScript player = (PlayerScript)target;
        //ItemData snowball = (ItemData)AssetDatabase.LoadAssetAtPath("Assets/Items/ScriptableObjects/Snowball.asset", typeof(ScriptableObject));
        ItemData snowball = (ItemData)Resources.Load("Items/Snowball");
        
        if (GUILayout.Button("Give Player snow")) {
            if (snowball == null) {
                Debug.Log("Snowball wasn't loaded :(");
            } else {
                player.AddItem(new Item(1, snowball));
            }
        }
    }
}