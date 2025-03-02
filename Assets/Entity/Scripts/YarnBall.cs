using UnityEngine;

public class YarnBall : SnowballProjectile {
    protected override void OnHit(Collider other, Vector3 relativeVelocity) {
        base.OnHit(other, relativeVelocity);

        if (shooter.transform.tag == "PlayerTag") {
            if (Random.value * 2 > 1) {
                BotBase.Summon(Registries.bots["momohsin"], lastPosition, Quaternion.identity);
            } else {
                BotBase.Summon(Registries.bots["belbelbel"], lastPosition, Quaternion.identity);
            }
        }
    }
}
