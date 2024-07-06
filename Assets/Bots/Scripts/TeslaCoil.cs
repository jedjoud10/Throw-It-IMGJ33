using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;

public class TeslaCoil : BotBehaviour {
    public VisualEffect vfx;
    public Transform target;
    public float radius = 5f;
    public float healingPerStep = 10f;
    public float secondsBetweenSteps = 1;
    public float delay = 0.5f;
    private float nextHealingStep;
    private float nextDeactivateTime;

    private void Start() {
        vfx.enabled = false;
    }

    private void Update() {
        if (Time.time > nextHealingStep) {
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            
            List<BotBase> bots = new List<BotBase>();
            foreach (var item in colliders) {
                BotBase bot = item.GetComponent<BotBase>();
                if (bot != null && bot != botBase) {
                    bots.Add(bot);
                }
            }

            if (bots.Count > 0) {
                var (bot, health) = bots.Select(x => (x, x._headHealth.health + x._bodyHealth.health)).OrderByDescending(x => x.Item2).First();

                bool a = bot._bodyHealth.Heal(healingPerStep);
                bool b = bot._headHealth.Heal(healingPerStep);

                if (a || b) {
                    target.position = bot.transform.position;
                    vfx.enabled = true;
                }
            }

            nextHealingStep = Time.time + secondsBetweenSteps;
            nextDeactivateTime = Time.time + delay;            
        }

        if (Time.time > nextDeactivateTime && vfx.enabled) {
            vfx.enabled = false;
        }
    }
}
