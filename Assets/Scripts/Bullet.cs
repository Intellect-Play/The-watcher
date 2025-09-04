using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    [Header("Steering Limits")]
    public float maxSteerAngle = 45f;
    public float steerSpeed    = 90f;

    [Header("Attack Settings")]
    public AttackType attackType;
    public int        chainCount;
    public float      areaRadius;

    private Vector3 _direction = Vector3.up;
    private int     _damage    = 0;
    private float   _speed     = 5f;

    public static void Fire(CardSO card, Vector3 origin)
    {
        Vector3 dir = FindDirectionToClosestEnemy(origin);
        var go = Instantiate(card.fireObject, origin, Quaternion.identity);
        var b  = go.GetComponent<Bullet>();
        b.attackType = card.attackType;
        b.chainCount = card.sessionLevel;
        b.Initialize(dir, card.baseDmg, 3);
    }

    public void Initialize(Vector3 dir, int damage, float bulletSpeed)
    {
        _direction = dir.normalized;
        _damage    = damage;
        _speed     = bulletSpeed;
        FaceDirection();

        switch (attackType)
        {
            case AttackType.Samurai_Hammer:              StartCoroutine(HammerRoutine()); break;
            case AttackType.Samurai_Blades:              StartCoroutine(BladesRoutine()); break;
            case AttackType.Samurai_Shiruken_Spinning:   StartCoroutine(ShirukenRoutine()); break;
            case AttackType.Samurai_ArrowRain:           StartCoroutine(ArrowRainRoutine()); break;
            case AttackType.Inventor_FireBomb:           StartCoroutine(FireBombRoutine()); break;
            case AttackType.Inventor_Piercing_Cogs:      StartCoroutine(PiercingCogsRoutine()); break;
            case AttackType.Inventor_Drone:              StartCoroutine(DroneRoutine()); break;
            case AttackType.Inventor_Dagger:             StartCoroutine(DaggerRoutine()); break;
            case AttackType.Wizard_Dagger:               StartCoroutine(DaggerRoutine()); break; 
            case AttackType.Wizard_WindPush:             StartCoroutine(WindPushRoutine()); break;
            case AttackType.Wizard_MagicBall:            StartCoroutine(MagicBallRoutine()); break;
            case AttackType.Wizard_WizStone:             StartCoroutine(WizardStoneRoutine()); break;

            default: break;
        }
    }

    void Update()
    {
        if (attackType != AttackType.Inventor_Drone &&
            attackType != AttackType.Inventor_Dagger &&
            attackType != AttackType.Samurai_Hammer &&
            attackType != AttackType.Samurai_Blades &&
            attackType != AttackType.Samurai_Shiruken_Spinning &&
            attackType != AttackType.Samurai_ArrowRain)
        {
            MoveTowardClosestEnemy();
        }
    }

    // -------------------- Inventor Drone --------------------
    private IEnumerator DroneRoutine()
    {
        Transform target = FindClosestEnemyTransform();
        if (!target) { Destroy(gameObject); yield break; }

        for (int i = 0; i < 8; i++)
        {
            var beeGO = Instantiate(gameObject, transform.position, transform.rotation);
            var bee   = beeGO.GetComponent<Bullet>();
            bee.attackType = AttackType.Inventor_Drone;
            bee.chainCount = chainCount;
            bee.areaRadius = areaRadius;
            bee._damage    = _damage;
            bee._speed     = _speed;
            bee._direction = _direction;
            bee.FaceDirection();

            // 2–3 stings before the bee expires
            int stings = Random.Range(2, 4);
            bee.StartCoroutine(bee.BeeFlyRoutine(target, stings));
            yield return new WaitForSecondsRealtime(0.3f);
        }

        Destroy(gameObject);
    }

    private IEnumerator BeeFlyRoutine(Transform target, int stingCount)
    {
        if (!target) { Destroy(gameObject); yield break; }

        // motion tuning
        const float wiggleFreq     = 1.2f;
        const float cruiseAmp      = 5f;
        const float diveAmp        = 0.45f;
        float       cruiseSpeed    = _speed;
        float       diveSpeed      = _speed * 3f;
        const float waypointThresh = 0.12f;
        const float hitRadius      = 0.12f;

        // “bird-view” scaling
        Vector3 baseScale   = transform.localScale;
        Vector3 cruiseScale = baseScale * 1.15f;
        Vector3 diveScale   = baseScale * 0.88f;

        Vector3 HoverPoint()
            => (target ? target.position : transform.position) +
            new Vector3(Random.Range(-areaRadius, areaRadius), Random.Range(0.6f, 1.2f), 0f);

        float time = 0f;

        // approach to first hover point (grow a bit)
        Vector3 waypoint = HoverPoint();
        float approachT = 0f;
        while (Vector3.Distance(transform.position, waypoint) > waypointThresh)
        {
            MoveWithWiggle(waypoint, cruiseAmp, wiggleFreq, ref time, cruiseSpeed);
            approachT = Mathf.MoveTowards(approachT, 1f, Time.deltaTime * 1.2f);
            transform.localScale = Vector3.Lerp(baseScale, cruiseScale, approachT);
            yield return null;
        }

        // sting loops
        for (int s = 0; s < stingCount; s++)
        {
            // DIVE
            float diveT = 0f;
            while (true)
            {
                if (!target || !target.gameObject.activeInHierarchy)
                {
                    target = FindClosestEnemyTransform();
                    if (!target) break;
                }

                if (target && Vector3.Distance(transform.position, target.position) <= hitRadius)
                    break;

                MoveWithWiggle(target.position, diveAmp, wiggleFreq, ref time, diveSpeed);
                diveT = Mathf.MoveTowards(diveT, 1f, Time.deltaTime * 4f);
                transform.localScale = Vector3.Lerp(cruiseScale, diveScale, diveT);
                yield return null;
            }

            // HIT
            if (target) target.GetComponent<EnemyBehaviour>()?.ApplyDamage(_damage);

            // RETREAT to a small offset
            if (!target) break;
            Vector3 toTarget = (target.position - transform.position).normalized;
            Vector3 side     = new Vector3(-toTarget.y, toTarget.x, 0f);
            Vector3 retreat  = transform.position
                            - toTarget * Random.Range(0.8f, 1.2f)
                            + side * Random.Range(-0.6f, 0.6f);

            float retreatT = 0f;
            while (Vector3.Distance(transform.position, retreat) > waypointThresh)
            {
                MoveWithWiggle(retreat, cruiseAmp, wiggleFreq, ref time, cruiseSpeed);
                retreatT = Mathf.MoveTowards(retreatT, 1f, Time.deltaTime * 2f);
                transform.localScale = Vector3.Lerp(diveScale, cruiseScale, retreatT);
                yield return null;
            }

            // Hover before next sting
            waypoint = HoverPoint();
            float hoverT = 0f;
            while (Vector3.Distance(transform.position, waypoint) > waypointThresh)
            {
                MoveWithWiggle(waypoint, cruiseAmp, wiggleFreq, ref time, cruiseSpeed);
                hoverT = Mathf.MoveTowards(hoverT, 1f, Time.deltaTime * 1.5f);
                transform.localScale = Vector3.Lerp(diveScale, cruiseScale, hoverT);
                yield return null;
            }
        }

        // --- self-death: toggle VFX, then destroy bee ---
        Transform beeVfx = transform.Find("inv_bee_fx");
        if (beeVfx)
        {
            beeVfx.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            beeVfx.gameObject.SetActive(false);
        }

        Destroy(gameObject);
    }


    // -------------------- Inventor/Wizard Dagger (volley + multi-target) --------------------
    private IEnumerator DaggerRoutine()
    {
        int count = 2;

        // Multi-dagger: this object acts as spawner
        if (count > 1)
        {
            // Collect unique enemy targets (repeat if fewer than count)
            var targets = GetEnemyTargets(count, transform.position);
            if (targets.Count == 0) { Destroy(gameObject); yield break; }

            // Base forward using closest target (for formation)
            Transform primary = targets[0];
            Vector3 fwd = (primary ? (primary.position - transform.position).normalized
                                : (_direction.sqrMagnitude > 0f ? _direction.normalized : Vector3.up));
            Vector3 perp = new Vector3(-fwd.y, fwd.x, 0f);

            // Tunables
            float lateral     = 0.35f;
            float fwdSpacing  = 0.25f;
            float backSpacing = 0.35f;
            float smallLat    = 0.20f;
            float bigLat      = 0.35f;
            float backDelay   = 0.08f;

            var shots = new List<(Vector3 pos, float delay)>();
            switch (count)
            {
                case 2:
                    shots.Add((transform.position + perp * (+lateral * 0.5f), 0f));
                    shots.Add((transform.position + perp * (-lateral * 0.5f), 0f));
                    break;
                case 3:
                    shots.Add((transform.position + fwd * fwdSpacing, 0f));
                    shots.Add((transform.position - fwd * backSpacing + perp * (+lateral * 0.6f), backDelay));
                    shots.Add((transform.position - fwd * backSpacing + perp * (-lateral * 0.6f), backDelay));
                    break;
                default: // 4
                    shots.Add((transform.position + fwd * fwdSpacing + perp * (+smallLat), 0f));
                    shots.Add((transform.position + fwd * fwdSpacing + perp * (-smallLat), 0f));
                    shots.Add((transform.position - fwd * backSpacing + perp * (+bigLat),  backDelay));
                    shots.Add((transform.position - fwd * backSpacing + perp * (-bigLat),  backDelay));
                    break;
            }

            for (int i = 0; i < shots.Count; i++)
            {
                var (pos, delay) = shots[i];
                Transform tgt = targets[Mathf.Min(i, targets.Count - 1)]; // different enemies when available

                var go = Instantiate(gameObject, pos, Quaternion.identity);
                var b  = go.GetComponent<Bullet>();
                b.attackType = attackType;
                b.chainCount = 1;
                b._damage    = _damage;
                b._speed     = _speed;

                // Face toward its own target (or keep formation forward if null)
                Vector3 dir = (tgt ? (tgt.position - pos).normalized : fwd);
                b._direction = dir;
                b.FaceDirection();

                if (delay > 0f) b.StartCoroutine(b.DelayStartDaggerFly(tgt, delay));
                else            b.StartCoroutine(b.DaggerFlyRoutine(tgt));
            }

            Destroy(gameObject);
            yield break;
        }

        // Single dagger
        Transform single = FindClosestEnemyTransform();
        if (!single) { Destroy(gameObject); yield break; }
        yield return StartCoroutine(DaggerFlyRoutine(single));
    }
    
    // Build up to 'count' targets. If fewer enemies exist, reuse the closest ones.
    private List<Transform> GetEnemyTargets(int count, Vector3 origin)
    {
        var result = new List<Transform>(count);
        var enemies = FindObjectsOfType<EnemyBehaviour>();
        var pool = new List<Transform>();
        foreach (var e in enemies)
            if (e && e.gameObject.activeInHierarchy) pool.Add(e.transform);

        if (pool.Count == 0) return result;

        // sort by distance
        pool.Sort((a, b) =>
            (a.position - origin).sqrMagnitude.CompareTo((b.position - origin).sqrMagnitude));

        // take distinct up to count
        int take = Mathf.Min(pool.Count, count);
        for (int i = 0; i < take; i++) result.Add(pool[i]);

        // if not enough, repeat from the start
        for (int i = take; i < count; i++) result.Add(pool[i % pool.Count]);

        return result;
    }
    private IEnumerator DelayStartDaggerFly(Transform target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (this && gameObject) // still alive?
            yield return StartCoroutine(DaggerFlyRoutine(target));
    }

    private IEnumerator DaggerFlyRoutine(Transform target)
    {
        float accelTime  = 0f;
        float startSpeed = 1f;
        float accelRate  = 5f;

        while (target && target.gameObject.activeInHierarchy)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            _direction  = dir;
            FaceDirection();

            transform.position += dir * (startSpeed + accelRate * accelTime) * Time.deltaTime;

            if (Vector3.Distance(transform.position, target.position) <= 0.2f)
            {
                OnHitEnemy(target);
                Destroy(gameObject);
                yield break;
            }

            accelTime += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    // -------------------- Samurai Hammer (multi + inner-spin + VFX) --------------------
    private IEnumerator HammerRoutine()
    {
        // How many hammers in this volley
        int count = 4;

        // If we want multiple, this object acts as the spawner
        if (count > 1)
        {
            // Prefer distinct targets when possible
            var targets = GetEnemyTargets(count, transform.position);
            if (targets.Count == 0) { Destroy(gameObject); yield break; }

            // Formation (like cogs/daggers)
            Transform primary = targets[0];
            Vector3 fwd  = (primary ? (primary.position - transform.position).normalized
                                    : (_direction.sqrMagnitude > 0f ? _direction.normalized : Vector3.up));
            Vector3 perp = new Vector3(-fwd.y, fwd.x, 0f);

            float spreadDeg    = 16f;
            float forwardSpace = 0.30f;
            float sideSpace    = 0.18f;
            float delayBetween = 0.06f;

            for (int i = 0; i < count; i++)
            {
                // fan out a bit
                float ang   = (i - (count - 1) / 2f) * spreadDeg;
                Vector3 dir = (Quaternion.Euler(0f, 0f, ang) * fwd).normalized;

                Vector3 spawnPos = transform.position
                                + dir * (i * forwardSpace)
                                + perp * ((i - (count - 1) / 2f) * sideSpace);

                var go  = Instantiate(gameObject, spawnPos, Quaternion.identity);
                var hb  = go.GetComponent<Bullet>();
                hb.attackType = AttackType.Samurai_Hammer;
                hb._damage    = _damage;

                // Enable the wind trail for each hammer
                var windFx = go.transform.Find("wind_fx");
                if (windFx) windFx.gameObject.SetActive(true);

                // Start each with a tiny stagger for nicer cadence
                hb.StartCoroutine(hb.HammerFlyRoutine(targets[i % targets.Count], delayBetween * i));
            }

            Destroy(gameObject);
            yield break;
        }

        // Single hammer path
        Transform target = FindClosestEnemyTransform();
        if (!target) { Destroy(gameObject); yield break; }
        yield return StartCoroutine(HammerFlyRoutine(target, 0f));
    }

    // Per-hammer movement/impact logic
    private IEnumerator HammerFlyRoutine(Transform target, float startDelay)
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        // Spin ONLY the inner "Bullet" child
        Transform spinT = transform.Find("Bullet") ?? transform;
        int tweenId = LeanTween.rotateAroundLocal(spinT.gameObject, Vector3.forward, -360f, 0.45f)
                            .setRepeat(-1)
                            .setEaseLinear()
                            .id;

        // VFX refs
        Transform hitVFX  = transform.Find("hit");
        if (hitVFX) hitVFX.gameObject.SetActive(false);
        Transform windFx  = transform.Find("wind_fx");
        if (windFx) windFx.gameObject.SetActive(true);

        float moveSpeed   = 3.25f;
        float reTargetT   = 0f;
        float maxLife     = 2.5f;   // safety so stray hammers don't linger
        float t           = 0f;

        while (t < maxLife)
        {
            // Retarget if current dies
            if (!target || !target.gameObject.activeInHierarchy)
            {
                reTargetT += Time.deltaTime;
                if (reTargetT > 0.15f) { target = FindClosestEnemyTransform(); reTargetT = 0f; }
                if (!target) break;
            }

            // Move toward target
            Vector3 toTarget = (target.position - transform.position).normalized;
            transform.position += toTarget * moveSpeed * Time.deltaTime;

            // Impact?
            if (Vector3.Distance(transform.position, target.position) <= 0.3f)
            {
                OnHitEnemy(target);

                // stop spin tween
                LeanTween.cancel(spinT.gameObject);

                // play hit VFX by SetActive(true) then destroy whole object
                if (hitVFX) hitVFX.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.10f);
                Destroy(gameObject);
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        // Timeout: stop spin and clean up
        LeanTween.cancel(spinT.gameObject);
        Destroy(gameObject);
    }


    // -------------------- Samurai Blades (volley up to 4) --------------------
    private IEnumerator BladesRoutine()
    {
        // how many blades this volley fires
        int count = 3;

        // Collect distinct targets up to 'count' (repeats if not enough enemies)
        var targets = GetEnemyTargets(count, transform.position);
        if (targets.Count == 0) { Destroy(gameObject); yield break; }

        // Forward/perp based on closest target for formation layout
        Transform primary = targets[0];
        Vector3 fwd = (primary ? (primary.position - transform.position).normalized
                            : (_direction.sqrMagnitude > 0f ? _direction.normalized : Vector3.up));
        Vector3 perp = new Vector3(-fwd.y, fwd.x, 0f);

        // Tunables (mirroring dagger layout feel)
        float lateral     = 0.35f;
        float fwdSpacing  = 0.25f;
        float backSpacing = 0.35f;
        float smallLat    = 0.20f;
        float bigLat      = 0.35f;
        float backDelay   = 0.08f;

        // (pos, delay, phaseOffset) — phaseOffset controls zigzag mirror (0 vs π)
        var shots = new List<(Vector3 pos, float delay, float phase)>();
        switch (count)
        {
            case 1:
                shots.Add((transform.position, 0f, 0f));
                break;

            case 2: // side-by-side
                shots.Add((transform.position + perp * (+lateral * 0.5f), 0f, 0f));
                shots.Add((transform.position + perp * (-lateral * 0.5f), 0f, Mathf.PI));
                break;

            case 3: // 1 front, 2 back
                shots.Add((transform.position + fwd * fwdSpacing, 0f, 0f));
                shots.Add((transform.position - fwd * backSpacing + perp * (+lateral * 0.6f), backDelay, 0f));
                shots.Add((transform.position - fwd * backSpacing + perp * (-lateral * 0.6f), backDelay, Mathf.PI));
                break;

            default: // 4: 2 front, 2 back
                shots.Add((transform.position + fwd * fwdSpacing + perp * (+smallLat), 0f, 0f));
                shots.Add((transform.position + fwd * fwdSpacing + perp * (-smallLat), 0f, Mathf.PI));
                shots.Add((transform.position - fwd * backSpacing + perp * (+bigLat),  backDelay, 0f));
                shots.Add((transform.position - fwd * backSpacing + perp * (-bigLat),  backDelay, Mathf.PI));
                break;
        }

        // Spawn volley
        for (int i = 0; i < shots.Count; i++)
        {
            var (pos, delay, phase) = shots[i];
            Transform tgt = targets[Mathf.Min(i, targets.Count - 1)];

            var bladeGO = Instantiate(gameObject, pos, Quaternion.identity);
            var bComp   = bladeGO.GetComponent<Bullet>();
            bComp.attackType = AttackType.Samurai_Blades;
            bComp._damage    = _damage;

            if (delay > 0f) bComp.StartCoroutine(bComp.DelayStartBladeZigZag(tgt, phase, delay));
            else            bComp.StartCoroutine(bComp.BladeZigZag(tgt, phase));
        }

        Destroy(gameObject);
    }

    // helper to delay a blade's start (for back row)
    private IEnumerator DelayStartBladeZigZag(Transform target, float phaseOffset, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (this && gameObject) // still alive?
            yield return StartCoroutine(BladeZigZag(target, phaseOffset));
    }

    private IEnumerator BladeZigZag(Transform target, float phaseOffset)
    {
        if (!target) { Destroy(gameObject); yield break; }

        float time  = 0f;
        float speed = 4f;
        float amp   = 2f;
        float freq  = 4f;
        float minAmp = 0.2f;

        while (target && target.gameObject.activeInHierarchy)
        {
            Vector3 toTarget = (target.position - transform.position).normalized;
            Vector3 side     = new Vector3(-toTarget.y, toTarget.x, 0f);

            float currentAmp = Mathf.Lerp(amp, minAmp, time * 0.4f);
            Vector3 offset   = side * Mathf.Sin(time * freq + phaseOffset) * currentAmp;

            transform.position += (toTarget * speed + offset) * Time.deltaTime;

            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            if (Vector3.Distance(transform.position, target.position) <= 0.25f)
            {
                OnHitEnemy(target);
                Destroy(gameObject);
                yield break;
            }

            time += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    // -------------------- Samurai Shiruken (volley + child-only spin) --------------------
    private IEnumerator ShirukenRoutine()
    {
        // volley settings (similar vibe to Piercing Cogs)
        int   shurikenCount = 10;     // how many to throw
        float spreadDeg     = 18f;   // angular spread between shurikens
        float delayBetween  = 0.1f; // slight stagger
        float forwardSpacing = 0.25f;
        float sideSpacing    = 0.12f;

        // pick a primary forward and gather targets to distribute across
        Transform primary = FindClosestEnemyTransform();
        if (!primary) { Destroy(gameObject); yield break; }

        Vector3 forward = (primary.position - transform.position).normalized;
        Vector3 side    = new Vector3(-forward.y, forward.x, 0f);

        var targets = GetEnemyTargets(shurikenCount, transform.position);
        if (targets.Count == 0) { Destroy(gameObject); yield break; }

        for (int i = 0; i < shurikenCount; i++)
        {
            float angleOff = (i - (shurikenCount - 1) / 2f) * spreadDeg;
            Vector3 dir2D  = (Quaternion.Euler(0f, 0f, angleOff) * forward).normalized;

            float fwdOff  = i * forwardSpacing;
            float sideOff = (i - (shurikenCount - 1) / 2f) * sideSpacing;
            Vector3 spawnPos = transform.position + dir2D * fwdOff + side * sideOff;

            GameObject go = Instantiate(gameObject, spawnPos, Quaternion.identity);
            var b = go.GetComponent<Bullet>();
            b.attackType = AttackType.Samurai_Shiruken_Spinning;
            b._damage    = _damage;
            b._speed     = _speed;
            b._direction = dir2D;

            // give different targets when available
            Transform tgt = targets[Mathf.Min(i, targets.Count - 1)];
            b.StartCoroutine(b.SingleShurikenRoutine(tgt));

            yield return new WaitForSecondsRealtime(delayBetween);
        }

        // spawner cleans itself up
        Destroy(gameObject);
    }

    private IEnumerator SingleShurikenRoutine(Transform target)
    {
        // spin only the visual child named "Bullet"
        Transform spinT = transform.Find("Bullet"); // do NOT spin whole prefab
        float spinSpeed   = 1080f;
        float moveSpeed   = 8f;
        float lifeMax     = 3.0f;   // fail-safe so shurikens don't linger
        float hitRadius   = 0.25f;

        float t = 0f;
        while (t < lifeMax)
        {
            if (!target || !target.gameObject.activeInHierarchy)
            {
                target = FindClosestEnemyTransform();
                if (!target) break;
            }

            Vector3 toTarget = (target.position - transform.position).normalized;
            transform.position += toTarget * moveSpeed * Time.deltaTime;

            if (spinT) spinT.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);

            if (Vector3.Distance(transform.position, target.position) <= hitRadius)
            {
                OnHitEnemy(target);
                Destroy(gameObject);
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        // If we exit loop without a hit, just clean up
        Destroy(gameObject);
    }
// -------------------- Samurai Arrow Rain (bottom-up, multi-target + swept hits) --------------------
private IEnumerator ArrowRainRoutine()
{
    // Prefer distinct targets
    var enemies = FindObjectsOfType<EnemyBehaviour>();
    var pool = new List<Transform>();
    foreach (var e in enemies) if (e && e.gameObject.activeInHierarchy) pool.Add(e.transform);
    if (pool.Count == 0) { Destroy(gameObject); yield break; }

    // sort by distance from shooter
    pool.Sort((a, b) =>
        (a.position - transform.position).sqrMagnitude.CompareTo((b.position - transform.position).sqrMagnitude));

    int arrowCount = 4;           // volley size
    float baseDelay = 0.05f;       // slight stagger
    Vector3 fwd = Vector3.up;      // default upward
    if (pool.Count > 0) fwd = (pool[0].position - transform.position).normalized;
    Vector3 perp = new Vector3(-fwd.y, fwd.x, 0f);

    for (int i = 0; i < arrowCount; i++)
    {
        Transform tgt = pool[i % pool.Count]; // distinct when possible

        // spawn at shooter with small lateral offset band
        float sign    = (i % 2 == 0) ? 1f : -1f;
        float lateral = sign * Mathf.Lerp(0.05f, 0.6f, (float)i / Mathf.Max(1, arrowCount - 1));
        Vector3 spawnPos = transform.position + perp * lateral;

        var arrow = Instantiate(gameObject, spawnPos, Quaternion.identity);
        var bComp = arrow.GetComponent<Bullet>();
        bComp.attackType = AttackType.Samurai_ArrowRain;
        bComp._damage    = _damage;

        // activate trail right away
        Transform trailVFX = arrow.transform.Find("trail_vfx");
        if (trailVFX) trailVFX.gameObject.SetActive(true);

        float delay = i * baseDelay;
        bComp.StartCoroutine(bComp.ArrowRainArrowDelayed(tgt, lateral, delay));
    }

    Destroy(gameObject);
}

private IEnumerator ArrowRainArrowDelayed(Transform target, float lateralOffset, float delay)
{
    if (delay > 0f) yield return new WaitForSeconds(delay);
    yield return StartCoroutine(ArrowRainArrow(target, lateralOffset));
}

// 'angleOffset' repurposed as initial lateral offset already applied at spawn.
// Arrow flies UPWARD from the shooter and homes into its target.
private IEnumerator ArrowRainArrow(Transform target, float angleOffset)
{
    const float speed        = 5f;
    const float accel        = 18f;
    const float maxSpeed     = 8f;
    const float turnRateDeg  = 540f;
    const float swayFreq     = 7f;
    const float swayAmp      = 0.10f;
    const float lifeMax      = 2.0f;
    const float hitRadius    = 0.28f;

    Vector3 minScale = transform.localScale * 0.85f;
    Vector3 maxScale = transform.localScale * 1.15f;
    transform.localScale = minScale;

    if (!target || !target.gameObject.activeInHierarchy)
    {
        target = FindClosestEnemyTransform();
        if (!target) { Destroy(gameObject); yield break; }
    }

    // initial forward is up (or toward target if above)
    Vector3 forward = (target.position - transform.position).normalized;
    if (forward.sqrMagnitude < 1e-6f) forward = Vector3.up;

    _direction = forward;
    FaceDirection();

    float curSpeed = speed;
    float t = 0f;
    Vector3 prevPos = transform.position;

    while (t < lifeMax)
    {
        if (!target || !target.gameObject.activeInHierarchy)
        {
            target = FindClosestEnemyTransform();
            if (!target) break;
        }

        Vector3 toTarget = (target.position - transform.position).normalized;

        float maxRadStep = turnRateDeg * Mathf.Deg2Rad * Time.deltaTime;
        forward = Vector3.RotateTowards(forward, toTarget, maxRadStep, 0f).normalized;

        Vector3 perp = new Vector3(-forward.y, forward.x, 0f);
        float sway = Mathf.Sin(t * swayFreq) * swayAmp;

        curSpeed = Mathf.Min(maxSpeed, curSpeed + accel * Time.deltaTime);

        Vector3 newPos = transform.position + (forward * curSpeed + perp * sway) * Time.deltaTime;
        Vector3 delta  = newPos - transform.position;
        transform.position = newPos;

        if (delta.sqrMagnitude > 1e-8f)
        {
            float ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
        }

        float dist01 = Mathf.Clamp01(t / lifeMax);
        transform.localScale = Vector3.Lerp(minScale, maxScale, dist01);

        var enemies = FindObjectsOfType<EnemyBehaviour>();
        foreach (var e in enemies)
        {
            if (!e || !e.gameObject.activeInHierarchy) continue;

            Vector3 c = ClosestPointOnSegment(prevPos, newPos, e.transform.position);
            c.z = e.transform.position.z;
            if ((e.transform.position - c).sqrMagnitude <= hitRadius * hitRadius)
            {
                e.ApplyDamage(_damage);

                Transform hitVFX = transform.Find("hit");
                if (hitVFX) hitVFX.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.08f);
                if (hitVFX) hitVFX.gameObject.SetActive(false);

                Destroy(gameObject);
                yield break;
            }
        }

        prevPos = newPos;
        t += Time.deltaTime;
        yield return null;
    }

    Destroy(gameObject);
}


// -------------------- Inventor Fire Bomb (ballistic + VFX toggle) --------------------
// -------------------- Inventor Fire Bomb (exact WizardStone-style) --------------------
private IEnumerator FireBombRoutine()
{
    int count = 3;

    Transform closest = FindClosestEnemyTransform();
    Vector3 fwd = closest
        ? (closest.position - transform.position).normalized
        : (_direction.sqrMagnitude > 0f ? _direction.normalized : Vector3.up);
    Vector3 perp = new Vector3(-fwd.y, fwd.x, 0f);

    float lateral     = 0.35f;
    float fwdSpacing  = 0.25f;
    float backSpacing = 0.35f;
    float smallLat    = 0.20f;
    float bigLat      = 0.35f;
    float backDelay   = 0.08f;

    var shots = new List<(Vector3 pos, float delay)>();
    switch (count)
    {
        case 1:
            shots.Add((transform.position, 0f));
            break;
        case 2:
            shots.Add((transform.position + perp * (+lateral * 0.5f), 0f));
            shots.Add((transform.position + perp * (-lateral * 0.5f), 0f));
            break;
        case 3:
            shots.Add((transform.position + fwd * fwdSpacing, 0f));
            shots.Add((transform.position - fwd * backSpacing + perp * (+lateral * 0.6f), backDelay));
            shots.Add((transform.position - fwd * backSpacing + perp * (-lateral * 0.6f), backDelay));
            break;
        default: // 4
            shots.Add((transform.position + fwd * fwdSpacing + perp * (+smallLat), 0f));
            shots.Add((transform.position + fwd * fwdSpacing + perp * (-smallLat), 0f));
            shots.Add((transform.position - fwd * backSpacing + perp * (+bigLat),  backDelay));
            shots.Add((transform.position - fwd * backSpacing + perp * (-bigLat),  backDelay));
            break;
    }

    // --- spawn bombs, but only first one does AoE + VFX ---
    for (int i = 0; i < shots.Count; i++)
    {
        var (pos, delay) = shots[i];
        var go = Instantiate(gameObject, pos, Quaternion.identity);
        var b  = go.GetComponent<Bullet>();
        b.attackType = AttackType.Inventor_FireBomb;
        b._damage    = _damage;
        b._speed     = _speed;
        b.chainCount = 1;

        bool isMainBomb = (i == 0); // only the very first bomb plays VFX + damage

        if (delay > 0f) b.StartCoroutine(b.DelayStartFireBomb(delay, isMainBomb));
        else            b.StartCoroutine(b.FireBombSingleRoutine(isMainBomb));
    }

    Destroy(gameObject);
    yield break;
}

private IEnumerator DelayStartFireBomb(float delay, bool isMainBomb)
{
    yield return new WaitForSeconds(delay);
    yield return StartCoroutine(FireBombSingleRoutine(isMainBomb));
}

private IEnumerator FireBombSingleRoutine(bool isMainBomb)
{
    Transform bulletT = transform.Find("Bullet");
    // IMPORTANT: use same VFX node name as Wizard Stone
    Transform hitVFX  = transform.Find("hit_vfx");
    if (hitVFX) hitVFX.gameObject.SetActive(false);

    // choose strike point (same as Wizard Stone)
    var enemies = FindObjectsOfType<EnemyBehaviour>();
    Vector3 strike = transform.position + Vector3.up * 2f;
    if (enemies != null && enemies.Length > 0)
    {
        var pool = new List<Transform>();
        foreach (var e in enemies) if (e && e.gameObject.activeInHierarchy) pool.Add(e.transform);
        if (pool.Count == 1)      strike = pool[0].position;
        else if (pool.Count >= 2)
        {
            pool.Sort((a,b) =>
                (a.position - transform.position).sqrMagnitude.CompareTo(
                (b.position - transform.position).sqrMagnitude));
            strike = (pool[0].position + pool[1].position) * 0.5f;
        }
    }
    strike += new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.15f, 0.15f), 0f);

    // identical Bezier arc
    Vector3 startPos  = transform.position;
    Vector3 endPos    = strike;
    float   arcHeight = 2.2f;
    Vector3 ctrlPos   = (startPos + endPos) * 0.5f + Vector3.up * arcHeight;

    float flightTime  = 1.4f;
    Vector3 startScale = transform.localScale;
    Vector3 midScale   = startScale * 1.10f;

    float t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime / flightTime;
        float u  = Mathf.Clamp01(t);
        float eu = u * u * (3f - 2f * u);

        Vector3 p = (1-eu)*(1-eu)*startPos + 2*(1-eu)*eu*ctrlPos + eu*eu*endPos;
        transform.position   = p;
        transform.localScale = Vector3.Lerp(startScale, midScale, Mathf.Sin(eu * Mathf.PI));

        Vector3 deriv = 2*(1-eu)*(ctrlPos - startPos) + 2*eu*(endPos - ctrlPos);
        if (deriv.sqrMagnitude > 1e-4f)
        {
            float ang = Mathf.Atan2(deriv.y, deriv.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
        }

        yield return null;
    }

    // freeze (same as Wizard Stone)
    _speed = 0f;
    var rb2d = GetComponent<Rigidbody2D>();
    if (rb2d)
    {
        rb2d.velocity = Vector2.zero;
        rb2d.angularVelocity = 0f;
        rb2d.isKinematic = true;
    }

    if (isMainBomb)
    {
        // show VFX + hide mesh exactly like Wizard Stone
        if (hitVFX) hitVFX.gameObject.SetActive(true);
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (hitVFX && (r.transform == hitVFX || r.transform.IsChildOf(hitVFX)))
                continue;
            r.enabled = false;
        }

        float vfxDelay = 0.25f;
        var ps = hitVFX ? hitVFX.GetComponent<ParticleSystem>() : null;
        if (ps) vfxDelay = Mathf.Min(ps.main.duration * 0.3f, 0.5f);
        yield return new WaitForSeconds(vfxDelay);

        float aoeRadius = 1.5f;
        var victims = FindObjectsOfType<EnemyBehaviour>();
        float r2 = aoeRadius * aoeRadius;
        foreach (var v in victims)
        {
            if (!v || !v.gameObject.activeInHierarchy) continue;
            if ((v.transform.position - transform.position).sqrMagnitude <= r2)
                v.ApplyDamage(_damage);
        }

        yield return new WaitForSeconds(2f);
    }

    Destroy(gameObject);
}


// -------------------- Inventor Piercing Cogs --------------------
private IEnumerator PiercingCogsRoutine()
{
    Transform tgt = FindClosestEnemyTransform();
    float planeZ = tgt ? tgt.position.z : transform.position.z;

    Vector3 forward = tgt
        ? (tgt.position - transform.position).normalized
        : (_direction.sqrMagnitude > 0 ? _direction : Vector3.right);

    forward.z = 0f;
    forward = forward.normalized;

    int   cogCount     = 8;
    float spreadDeg    = 18f;                // angle difference
    float delayBetween = 0.5f;               // stagger

    // visual separation amounts
    float forwardSpacing = 0.35f;            // how far ahead each next cog starts
    float sideSpacing    = 0.12f;            // slight lateral separation

    // perpendicular (to get a left/right offset in 2D)
    Vector3 side = new Vector3(-forward.y, forward.x, 0f); // 90° left of forward

    Vector3 basePos = new Vector3(transform.position.x, transform.position.y, planeZ);

    for (int i = 0; i < cogCount; i++)
    {
        float angleOff = (i - (cogCount - 1) / 2f) * spreadDeg;
        Vector3 dir2D  = (Quaternion.Euler(0f, 0f, angleOff) * forward).normalized;

        // unique spawn position per cog
        float forwardOff = i * forwardSpacing;
        float sideOff    = (i - (cogCount - 1) / 2f) * sideSpacing;
        Vector3 spawnPos = basePos + dir2D * forwardOff + side * sideOff;
        spawnPos.z = planeZ;

        GameObject cogGO = Instantiate(gameObject, spawnPos, Quaternion.identity);

        Bullet cog = cogGO.GetComponent<Bullet>();
        cog.StopAllCoroutines();                  // ensure no spawner logic runs on clone
        cog.attackType = AttackType.Inventor_Piercing_Cogs;
        cog._damage    = _damage;
        cog._direction = dir2D;                   // normalized 2D heading
        cog.StartCoroutine(cog.CogFlyRoutine(planeZ));

        yield return new WaitForSecondsRealtime(delayBetween);
    }

    Destroy(gameObject);
}

private IEnumerator CogFlyRoutine(float planeZ)
{
    float moveSpeed   = 6f;
    float spinSpeedZ  = -720f;
    float lifeSeconds = 3.0f;
    float hitRadius   = 0.35f;

    var hitSet = new HashSet<EnemyBehaviour>();
    Transform visual = transform.Find("Bullet");

    // snap this cog to the motion plane once
    transform.position = new Vector3(transform.position.x, transform.position.y, planeZ);

    Vector3 prevPos = transform.position;
    float t = 0f;

    while (t < lifeSeconds)
    {
        // move strictly in XY
        Vector3 step = new Vector3(_direction.x, _direction.y, 0f).normalized * (moveSpeed * Time.deltaTime);
        Vector3 newPos = prevPos + step;
        newPos.z = planeZ; // keep Z fixed

        // swept hit test (segment)
        var enemies = FindObjectsOfType<EnemyBehaviour>();
        foreach (var eb in enemies)
        {
            if (!eb || !eb.gameObject.activeInHierarchy || hitSet.Contains(eb)) continue;

            Vector3 c = ClosestPointOnSegment(prevPos, newPos, eb.transform.position);
            // also project test to plane by zeroing z diff (since we're 2D)
            c.z = eb.transform.position.z; 
            if ((eb.transform.position - c).sqrMagnitude <= hitRadius * hitRadius)
            {
                eb.ApplyDamage(_damage);
                hitSet.Add(eb); // pierce: keep flying
            }
        }

        // apply movement + spin
        transform.position = newPos;
        if (visual) visual.Rotate(Vector3.forward, spinSpeedZ * Time.deltaTime, Space.Self);
        else        transform.Rotate(Vector3.forward, spinSpeedZ * Time.deltaTime, Space.Self);

        prevPos = newPos;
        t += Time.deltaTime;
        yield return null;
    }

    Destroy(gameObject);
}

// -------------------- Wizard Wind Push --------------------
private IEnumerator WindPushRoutine()
{
    var enemies = FindObjectsOfType<EnemyBehaviour>();
    if (enemies == null || enemies.Length == 0) { Destroy(gameObject); yield break; }

    int n = enemies.Length;

    Vector3[] startPos = new Vector3[n];
    float[] targetY    = new float[n];

    // Record starting positions and push targets
    for (int i = 0; i < n; i++)
    {
        var eb = enemies[i];
        if (!eb || !eb.gameObject.activeInHierarchy) continue;

        startPos[i] = eb.transform.position;
        targetY[i]  = startPos[i].y + 2f; // push distance
    }

    // Slower push: takes 4 seconds
    float duration = 4.0f;
    float t = 0f;

    while (t < 1f)
    {
        t += Time.deltaTime / duration;
        float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        for (int i = 0; i < n; i++)
        {
            var eb = enemies[i];
            if (!eb || !eb.gameObject.activeInHierarchy) continue;

            // Only Y changes, X/Z fixed
            eb.transform.position = new Vector3(
                startPos[i].x,
                Mathf.Lerp(startPos[i].y, targetY[i], ease),
                startPos[i].z
            );
        }

        yield return null;
    }

    Destroy(gameObject);
}


// -------------------- Wizard Magic Ball (multi-spawn homing orbs) --------------------
private IEnumerator MagicBallRoutine()
{
    // How many orbs to spawn this cast (similar spirit to daggers/cogs)
    int ballCount = Mathf.Clamp(chainCount, 2, 6);   // scale with card/session level
    float spawnDelay = 0.08f;                        // slight stagger
    float ringRadius = 0.25f;                        // small visual spread ring

    // Build preferred targets (distinct if possible)
    var targets = GetEnemyTargets(ballCount, transform.position);
    if (targets.Count == 0) { Destroy(gameObject); yield break; }

    // Forward basis for formation
    Transform primary = targets[0];
    Vector3 fwd = (primary ? (primary.position - transform.position).normalized
                           : (_direction.sqrMagnitude > 0f ? _direction.normalized : Vector3.up));
    Vector3 right = new Vector3(fwd.y, -fwd.x, 0f);

    for (int i = 0; i < ballCount; i++)
    {
        // Arrange in a small ring with slight forward bias
        float ang = (Mathf.PI * 2f) * (i / (float)ballCount);
        Vector3 radial = (Mathf.Cos(ang) * right + Mathf.Sin(ang) * fwd).normalized;
        Vector3 spawnPos = transform.position + radial * ringRadius;

        var orbGO = Instantiate(gameObject, spawnPos, Quaternion.identity);
        var orb   = orbGO.GetComponent<Bullet>();
        orb.attackType = AttackType.Wizard_MagicBall;
        orb._damage    = _damage;
        orb._speed     = _speed * 0.9f; // a bit slower than dagger, feels floaty

        // face roughly forward initially
        orb._direction = (primary ? (primary.position - spawnPos).normalized : fwd);
        orb.FaceDirection();

        // enable optional visuals
        var trail = orb.transform.Find("trail_vfx");
        if (trail) trail.gameObject.SetActive(true);

        var spin = orb.transform.Find("Bullet");
        if (spin) LeanTween.rotateAroundLocal(spin.gameObject, Vector3.forward, 360f, 0.8f).setRepeat(-1).setEaseLinear();

        // pick target (distinct when available)
        Transform tgt = targets[Mathf.Min(i, targets.Count - 1)];
        orb.StartCoroutine(orb.MagicBallFlyRoutine(tgt));

        if (spawnDelay > 0f) yield return new WaitForSeconds(spawnDelay);
    }

    // this spawner is just a launcher
    Destroy(gameObject);
}

private IEnumerator MagicBallFlyRoutine(Transform target)
{
    // homing with gentle curve + acceleration
    float lifeMax     = 3.5f;
    float accel       = 2.5f;
    float maxSpeed    = Mathf.Max(4f, _speed * 1.4f);
    float steerDeg    = 240f; // high agility
    float hitRadius   = 0.22f;

    Vector3 vel = _direction * Mathf.Max(1.5f, _speed * 0.6f);
    float t = 0f;

    while (t < lifeMax)
    {
        // retarget if lost
        if (!target || !target.gameObject.activeInHierarchy)
            target = FindClosestEnemyTransform();

        // steer toward target if any, otherwise keep current heading
        Vector3 desired = (target ? (target.position - transform.position).normalized : vel.normalized);
        float maxRad = steerDeg * Mathf.Deg2Rad * Time.deltaTime;
        Vector3 newDir = Vector3.RotateTowards(vel.normalized, desired, maxRad, 0f).normalized;

        // accelerate
        float spd = Mathf.Min(maxSpeed, vel.magnitude + accel * Time.deltaTime);
        vel = newDir * spd;

        // move
        transform.position += vel * Time.deltaTime;

        // face velocity
        if (vel.sqrMagnitude > 1e-4f)
        {
            float ang = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
        }

        // hit check
        if (target && (target.position - transform.position).sqrMagnitude <= hitRadius * hitRadius)
        {
            // apply damage
            target.GetComponent<EnemyBehaviour>()?.ApplyDamage(_damage);

            // play hit vfx then destroy whole orb
            var hitVFX = transform.Find("hit");
            if (hitVFX) hitVFX.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);

            // stop spinner tween safely
            var spin = transform.Find("Bullet");
            if (spin) LeanTween.cancel(spin.gameObject);

            Destroy(gameObject);
            yield break;
        }

        t += Time.deltaTime;
        yield return null;
    }

    // timeout — clean up
    var spinT = transform.Find("Bullet");
    if (spinT) LeanTween.cancel(spinT.gameObject);
    Destroy(gameObject);
}


// -------------------- Wizard Stone (multi-spawn -> single-behavior) --------------------
private IEnumerator WizardStoneRoutine()
{
    int count = 3;

    Transform closest = FindClosestEnemyTransform();
    Vector3 fwd = closest
        ? (closest.position - transform.position).normalized
        : (_direction.sqrMagnitude > 0f ? _direction.normalized : Vector3.up);
    Vector3 perp = new Vector3(-fwd.y, fwd.x, 0f);

    float lateral     = 0.35f;
    float fwdSpacing  = 0.25f;
    float backSpacing = 0.35f;
    float smallLat    = 0.20f;
    float bigLat      = 0.35f;
    float backDelay   = 0.08f;

    var shots = new List<(Vector3 pos, float delay)>();
    switch (count)
    {
        case 1:
            shots.Add((transform.position, 0f));
            break;
        case 2:
            shots.Add((transform.position + perp * (+lateral * 0.5f), 0f));
            shots.Add((transform.position + perp * (-lateral * 0.5f), 0f));
            break;
        case 3:
            shots.Add((transform.position + fwd * fwdSpacing, 0f));
            shots.Add((transform.position - fwd * backSpacing + perp * (+lateral * 0.6f), backDelay));
            shots.Add((transform.position - fwd * backSpacing + perp * (-lateral * 0.6f), backDelay));
            break;
        default: // 4
            shots.Add((transform.position + fwd * fwdSpacing + perp * (+smallLat), 0f));
            shots.Add((transform.position + fwd * fwdSpacing + perp * (-smallLat), 0f));
            shots.Add((transform.position - fwd * backSpacing + perp * (+bigLat),  backDelay));
            shots.Add((transform.position - fwd * backSpacing + perp * (-bigLat),  backDelay));
            break;
    }

    // --- spawn stones, but only first one does AoE + VFX ---
    for (int i = 0; i < shots.Count; i++)
    {
        var (pos, delay) = shots[i];
        var go = Instantiate(gameObject, pos, Quaternion.identity);
        var b  = go.GetComponent<Bullet>();
        b.attackType = AttackType.Wizard_WizStone;
        b._damage    = _damage;
        b._speed     = _speed;
        b.chainCount = 1;

        bool isMainStone = (i == 0); // only the very first stone plays VFX + damage

        if (delay > 0f) b.StartCoroutine(b.DelayStartWizardStone(delay, isMainStone));
        else            b.StartCoroutine(b.WizardStoneSingleRoutine(isMainStone));
    }

    Destroy(gameObject);
    yield break;
}

private IEnumerator DelayStartWizardStone(float delay, bool isMainStone)
{
    yield return new WaitForSeconds(delay);
    yield return StartCoroutine(WizardStoneSingleRoutine(isMainStone));
}

private IEnumerator WizardStoneSingleRoutine(bool isMainStone)
{
    Transform bulletT = transform.Find("Bullet");
    Transform hitVFX  = transform.Find("hit_vfx");
    if (hitVFX) hitVFX.gameObject.SetActive(false);

    // choose strike point
    var enemies = FindObjectsOfType<EnemyBehaviour>();
    Vector3 strike = transform.position + Vector3.up * 2f;
    if (enemies != null && enemies.Length > 0)
    {
        var pool = new List<Transform>();
        foreach (var e in enemies) if (e && e.gameObject.activeInHierarchy) pool.Add(e.transform);
        if (pool.Count == 1)      strike = pool[0].position;
        else if (pool.Count >= 2)
        {
            pool.Sort((a,b) =>
                (a.position - transform.position).sqrMagnitude.CompareTo(
                (b.position - transform.position).sqrMagnitude));
            strike = (pool[0].position + pool[1].position) * 0.5f;
        }
    }
    strike += new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.15f, 0.15f), 0f);

    // lob
    Vector3 startPos  = transform.position;
    Vector3 endPos    = strike;
    float   arcHeight = 2.2f;
    Vector3 ctrlPos   = (startPos + endPos) * 0.5f + Vector3.up * arcHeight;

    float flightTime  = 1.4f;
    Vector3 startScale = transform.localScale;
    Vector3 midScale   = startScale * 1.10f;

    float t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime / flightTime;
        float u  = Mathf.Clamp01(t);
        float eu = u * u * (3f - 2f * u);

        Vector3 p = (1-eu)*(1-eu)*startPos + 2*(1-eu)*eu*ctrlPos + eu*eu*endPos;
        transform.position   = p;
        transform.localScale = Vector3.Lerp(startScale, midScale, Mathf.Sin(eu * Mathf.PI));

        Vector3 deriv = 2*(1-eu)*(ctrlPos - startPos) + 2*eu*(endPos - ctrlPos);
        if (deriv.sqrMagnitude > 1e-4f)
        {
            float ang = Mathf.Atan2(deriv.y, deriv.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
        }

        yield return null;
    }

    // freeze
    _speed = 0f;
    var rb2d = GetComponent<Rigidbody2D>();
    if (rb2d)
    {
        rb2d.velocity = Vector2.zero;
        rb2d.angularVelocity = 0f;
        rb2d.isKinematic = true;
    }

    if (isMainStone)
    {
        // show VFX + damage
        if (hitVFX) hitVFX.gameObject.SetActive(true);
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (hitVFX && (r.transform == hitVFX || r.transform.IsChildOf(hitVFX)))
                continue;
            r.enabled = false;
        }

        float vfxDelay = 0.25f;
        var ps = hitVFX ? hitVFX.GetComponent<ParticleSystem>() : null;
        if (ps) vfxDelay = Mathf.Min(ps.main.duration * 0.3f, 0.5f);
        yield return new WaitForSeconds(vfxDelay);

        float aoeRadius = 1.5f;
        var victims = FindObjectsOfType<EnemyBehaviour>();
        float r2 = aoeRadius * aoeRadius;
        foreach (var v in victims)
        {
            if (!v || !v.gameObject.activeInHierarchy) continue;
            if ((v.transform.position - transform.position).sqrMagnitude <= r2)
                v.ApplyDamage(_damage);
        }

        yield return new WaitForSeconds(2f);
    }

    // non-main stones: just vanish silently
    Destroy(gameObject);
}





// helper for swept collision
private static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
{
    Vector3 ab = b - a;
    float ab2 = Vector3.Dot(ab, ab);
    if (ab2 <= 1e-6f) return a;
    float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab2);
    return a + ab * t;
}

    // Custom ease in for gravity feel
    private float easeIn(float x) => x * x;


    // -------------------- Helpers --------------------
    private static Vector3 FindDirectionToClosestEnemy(Vector3 origin)
    {
        Vector3 dir = Vector3.up;
        float best = float.MaxValue;
        foreach (var e in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (!e.activeInHierarchy) continue;
            float d = (e.transform.position - origin).sqrMagnitude;
            if (d < best) { best = d; dir = (e.transform.position - origin).normalized; }
        }
        return dir;
    }

    private Transform FindClosestEnemyTransform()
    {
        var enemies = FindObjectsOfType<EnemyBehaviour>();
        float minSqr = float.MaxValue;
        Transform best = null;
        foreach (var eb in enemies)
        {
            if (!eb.gameObject.activeInHierarchy) continue;
            float sq = (eb.transform.position - transform.position).sqrMagnitude;
            if (sq < minSqr) { minSqr = sq; best = eb.transform; }
        }
        return best;
    }

    private void MoveTowardClosestEnemy()
    {
        float maxRadStep = steerSpeed * Mathf.Deg2Rad * Time.deltaTime;
        Transform best = FindClosestEnemyTransform();
        if (best)
        {
            Vector3 rawDir = (best.position - transform.position).normalized;
            Vector3 targetDir = ClampToSteerRange(rawDir);
            _direction = Vector3.RotateTowards(_direction, targetDir, maxRadStep, 0f).normalized;
            FaceDirection();
        }
        transform.position += _direction * _speed * Time.deltaTime;
    }

    private void MoveWithWiggle(Vector3 targetPos, float amp, float freq, ref float time, float moveSpeed)
    {
        Vector3 dir  = (targetPos - transform.position).normalized;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f);
        float wig = Mathf.Sin(time * freq * Mathf.PI * 2f) * amp;
        transform.position += (dir * moveSpeed + perp * wig) * Time.deltaTime;
        FaceDirection();
        time += Time.deltaTime;
    }

    private void OnHitEnemy(Transform target)
    {
        var fx = transform.Find("inv_bee_fx");
        var bullet = transform.Find("Bullet");
        if (fx) fx.gameObject.SetActive(true);
        if (bullet) Destroy(bullet.gameObject);

        target?.GetComponent<EnemyBehaviour>()?.ApplyDamage(_damage);
    }

    private float PlayHitVFX(Transform vfx)
    {
        if (!vfx) return 0.6f;
        vfx.gameObject.SetActive(true);

        float maxDur = 0f;
        foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Play(true);
            var m = ps.main;
            maxDur = Mathf.Max(maxDur, m.duration +
                (m.startLifetime.mode == ParticleSystemCurveMode.TwoConstants ? m.startLifetime.constantMax : m.startLifetime.constant));
        }
        return maxDur > 0f ? maxDur : 0.6f;
    }

    private Vector3 ClampToSteerRange(Vector3 rawDir)
    {
        float rawAngle = Mathf.Atan2(rawDir.y, rawDir.x) * Mathf.Rad2Deg;
        float rel = rawAngle - 90f;
        float clamped = Mathf.Clamp(rel, -maxSteerAngle, maxSteerAngle);
        float final = clamped + 90f;
        float rad = final * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }

    private void FaceDirection()
    {
        float ang = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
    }

    
}
