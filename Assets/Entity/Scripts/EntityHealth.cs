using UnityEngine;

public class EntityHealth : MonoBehaviour, IEntitySerializer {
    public float maxHealth;
    public float health;

    public bool DeleteOnKill = true;
    public delegate void Killed();
    public event Killed OnKilled;

    public delegate void HealthChanged(float percentage);
    public event HealthChanged OnHealthChanged;

    public class DamageSourceData {
        public GameObject source;
        public Vector3 direction;
    }

    public delegate void HealthDamaged(float damage, DamageSourceData source);
    public event HealthDamaged OnDamaged;

    public delegate void HealthHealed(float healing);
    public event HealthHealed OnHealed;

    public delegate void PreDamageModifier(ref float damage);
    public event PreDamageModifier OnPreDamageModifier;

    [HideInInspector]
    public bool AlreadyKilled { get; private set; }

    public void Start() {
        AlreadyKilled = false;
        health = maxHealth;
        if (DeleteOnKill) OnKilled += () => { Destroy(gameObject); };
    }

    public void Damage(float damage, DamageSourceData data = null) {
        if (AlreadyKilled)
            return;

        OnPreDamageModifier?.Invoke(ref damage);
        health = Mathf.Clamp(health - damage, 0, maxHealth);
         
        OnDamaged?.Invoke(damage, data);
        OnHealthChanged?.Invoke(health / maxHealth);
        if (health == 0) {
            AlreadyKilled = true;
            OnKilled?.Invoke();
        }
    }

    public bool Heal(float healing) {
        float healthCpy = health;
        health = Mathf.Clamp(health + healing, 0, maxHealth);

        float effectiveHealing = health - healthCpy;

        if (effectiveHealing > 0) {
            OnHealed?.Invoke(effectiveHealing);
            OnHealthChanged?.Invoke(health / maxHealth);
        }

        return effectiveHealing > 0;
    }

    public void Serialize(EntityData data) {
        data.health = health;
    }

    public void Deserialize(EntityData data) {
        health = data.health.Value;
        health = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(health / maxHealth);
        AlreadyKilled = health == 0f;

        if (AlreadyKilled)
            OnKilled?.Invoke();
    }
}