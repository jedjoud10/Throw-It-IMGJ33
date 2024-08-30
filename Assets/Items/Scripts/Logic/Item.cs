using Newtonsoft.Json;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

[Serializable]
public class Item : ICloneable {
    public virtual void PrimaryAction(InputAction.CallbackContext context, Player player) { 
        
    }

    public virtual void SecondaryAction(InputAction.CallbackContext context, Player player) { 
        
    }

    public virtual void Equipped(Player player) {
        
    }

    public virtual void Unequipped(Player player) {
        
    }

    public virtual void Update(Player player) {

    }

    public virtual void EquippedUpdate(Player player) { 
    
    }

    public virtual void OnWorldItemSpawned(WorldItem wi) { }

    public object Clone() {
        return MemberwiseClone();
    }
}
