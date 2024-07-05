using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BotBase : MonoBehaviour {
    [Header("Main")]
    public BotData data;

    [Header("Procedural Holsters")]
    public GameObject centerHolster;
    public GameObject leftHolster;
    public GameObject rightHolster;
    public GameObject leftEyeHolster;
    public GameObject rightEyeHolster;
    public GameObject hatHolster;
    public GameObject neckHolster;
    public GameObject noseHolster;

    [Header("Head Entity")]
    public GameObject headMeshHolster;
    public GameObject healthHeadEntity;

    [Header("Faces")]
    public GameObject angryFace;
    public GameObject happyFace;

    [Header("Attributes")]
    public float movementSpeed = 0;
    public float attackSpeed = 0;
    public float bodyHealth = 0;
    public float headHealth = 0;
    public float accuracy = 0;
    public float damageResistance = 0;
    public float knockbackResistance = 0;

    private EntityMovement entityMovement;
    [HideInInspector]
    public EntityHealth _headHealth;
    [HideInInspector]
    public EntityHealth _bodyHealth;
    private BotPathfinder pathfinder;
    private BotTextToSpeech tts;

    // Both bot parts and components
    private List<BotBehaviour> botBehaviours;
    private Vector3 target;

    private void OnValidate() {
        ApplyAttributes();
    }

    private void ApplyModifiers(List<BotAttributeModifier> modifiers) {
        foreach (var modifier in modifiers) {
            float delta = modifier.delta;
            switch (modifier.attribute) {
                case BotAttribute.MovementSpeed:
                    movementSpeed += delta;
                    break;
                case BotAttribute.AttackSpeed:
                    attackSpeed += delta;
                    break;
                case BotAttribute.HeadHealth:
                    headHealth += delta;
                    break;
                case BotAttribute.BodyHealth:
                    bodyHealth += delta;
                    break;
                case BotAttribute.DamageResistance:
                    damageResistance += delta;
                    break;
                case BotAttribute.KnockbackResistance:
                    knockbackResistance += delta;
                    break;
                case BotAttribute.Accuracy:
                    accuracy += delta;
                    break;
            }
        }
    }

    private void SpawnPart(GameObject holster, BotPartData part) {
        GameObject spawned = Instantiate(part.prefab, holster.transform);
        ApplyModifiers(part.modifiers);

        BotBehaviour worldPart = spawned.GetComponent<BotBehaviour>();
        if (worldPart != null) {
            botBehaviours.Add(worldPart);
        }
    }


    private BotPartData PickPartForHolsterType(GameObject holster, RngList<BotPartData> parts) {
        int index = parts.list.FindIndex(x => Random.value < x.spawnChance);

        if (index == -1 && parts.useLastAsFallback) {
            index = parts.list.Count - 1;
        }

        BotPartData part = index >= 0 ? parts.list[index] : null;
        if (part != null)
            SpawnPart(holster, part);
        return part;
    }

    private void OnPreDamage(ref float damage, bool head) {
        damage *= (1 - damageResistance);
    }

    private void OnDamaged(float damage, bool head) {
        tts.SayString(head ? "bruh" : "ouch", overwritePlaying: false);
    }

    private void OnHealed(float heal, bool head) {
        tts.SayString("thank you for healing me");
    }

    private void OnKilled(bool headshot) {
        Destroy(gameObject);
    }

    private void ApplyAttributes() {
        if (entityMovement == null)
            return;

        entityMovement.speed = Mathf.Max(movementSpeed, 0);
        entityMovement.knockbackResistance = Mathf.Clamp01(knockbackResistance);
        _bodyHealth.maxHealth = bodyHealth;
        _bodyHealth.health = bodyHealth;
        _headHealth.maxHealth = headHealth;
        _headHealth.health = headHealth;

        foreach (var part in botBehaviours) {
            part.AttributesUpdated();
        }
    }

    private void SpawnParts() {
        movementSpeed = data.baseMovementSpeed;
        attackSpeed = data.baseAttackSpeed;
        bodyHealth = data.baseBodyHealth;
        headHealth = data.baseHeadHealth;
        damageResistance = data.baseDamageResistance;
        knockbackResistance = data.baseKnockbackResistance;
        accuracy = data.baseAccuracy;

        // Base weapons / attribute modifiers
        BotPartData center = PickPartForHolsterType(centerHolster, data.center);
        if (center == null || !center.tags.Contains("disable sides")) {
            PickPartForHolsterType(leftHolster, data.left);
            PickPartForHolsterType(rightHolster, data.right);
        };

        BotPartData head = PickPartForHolsterType(headMeshHolster, data.heads);

        // Spawns at LEAST one default eye if needed
        if (data.applyFace) {
            if (data.spawnAtLeastOneDefaultEye) {
                if (Random.value > 0.5) {
                    PickPartForHolsterType(leftEyeHolster, data.leftEye);
                    SpawnPart(rightEyeHolster, new BotPartData(data.defaultEye));
                } else {
                    SpawnPart(leftEyeHolster, new BotPartData(data.defaultEye));
                    PickPartForHolsterType(rightEyeHolster, data.rightEye);
                }
            } else {
                PickPartForHolsterType(leftEyeHolster, data.leftEye);
                PickPartForHolsterType(rightEyeHolster, data.rightEye);
            }

            PickPartForHolsterType(noseHolster, data.nose);
        }
        

        // Check if we have a head we can apply hats on
        if (head.tags.Contains("hattable")) {
            PickPartForHolsterType(hatHolster, data.hat);
        }

        PickPartForHolsterType(neckHolster, data.neck);
    }

    private void ApplyAngry() {
        bool angy = Random.value < data.angryChance;

        if (data.applyFace) {
            happyFace.SetActive(!angy);
            angryFace.SetActive(angy);
        }

        if (angy) {
            ApplyModifiers(data.angryModifiers);
        }
    }

    public void Start() {
        botBehaviours = GetComponents<BotBehaviour>().ToList();
        pathfinder = GetComponent<BotPathfinder>();
        entityMovement = GetComponent<EntityMovement>();
        _bodyHealth = GetComponent<EntityHealth>();
        _headHealth = healthHeadEntity.GetComponent<EntityHealth>();
        tts = GetComponent<BotTextToSpeech>();

        healthHeadEntity.transform.position = headMeshHolster.transform.position;
        Destroy(headMeshHolster.GetComponent<MeshFilter>());
        Destroy(headMeshHolster.GetComponent<MeshRenderer>());

        // I know this looks ugly but it works for now. Will need to refactor this whole class later anyways
        _bodyHealth.OnPreDamageModifier += (ref float dmg) => OnPreDamage(ref dmg, false);
        _headHealth.OnPreDamageModifier += (ref float dmg) => OnPreDamage(ref dmg, true);
        _bodyHealth.OnDamaged += (float dmg) => OnDamaged(dmg, false);
        _headHealth.OnDamaged += (float dmg) => OnDamaged(dmg, true);
        _bodyHealth.OnHealed += (float heal) => OnHealed(heal, false);
        _headHealth.OnHealed += (float heal) => OnHealed(heal, true);
        _bodyHealth.OnKilled += () => { OnKilled(false); };
        _headHealth.OnKilled += () => { OnKilled(true); };

        SpawnParts();

        foreach (var item in botBehaviours) {
            item.botBase = this;
            item.movement = entityMovement;
            item.bodyHealth = _bodyHealth;
            item.headHealth = _headHealth;
            item.botTts = tts;
        }

        ApplyAngry();
        ApplyAttributes();
    }

    public void Update() {
        GameObject player = Player.Instance.gameObject;
        Vector3 velocity = player.GetComponent<EntityMovement>().Velocity;
        target = player.transform.position;
        pathfinder.target = target;

        foreach (var part in botBehaviours) {
            part.TargetChanged(target, velocity);
        }
    }
}