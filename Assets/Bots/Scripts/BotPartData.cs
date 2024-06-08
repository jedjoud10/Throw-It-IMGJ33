using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BotPartData {
    public BotPartData(GameObject prefab) {
        this.prefab = prefab;
        spawnChance = 0;
        modifiers = new List<BotAttributeModifier>();
    }

    public GameObject prefab;
    public float spawnChance;

    public List<BotAttributeModifier> modifiers;
}
