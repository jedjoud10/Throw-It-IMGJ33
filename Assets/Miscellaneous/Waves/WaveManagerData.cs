using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Scriptable Objects/New Wave", order = 1)]
public class WaveManagerData : ScriptableObject {
    public List<WaveData> waves;
}
