# Design — What the fire kit needs from the plumbing

> Companion to `skill-plugin-system.md`. That document defines *how a skill is wired*;
> this one takes the owner's first real kit (fireball basic / ground-slam sub / EX) and
> asks: **what must the plumbing be able to express for this kit to be built as data
> instead of as one-off code?**
>
> Status: proposal. The kit itself is the owner's design and is not being second-guessed
> here — only the machinery underneath it.

---

## 0. The kit being modelled

| Role | Effect | Upgrade ladder |
|---|---|---|
| **Basic** | Throw a fireball every 2s in the aimed direction | **1** leaves a burning zone on the ground · **2** marks enemies hit or burning; a marked enemy that dies explodes. Higher level = longer mark, bigger AoE and explosion damage |
| **Sub** | Slam the ground: an explosion, then a burning zone with its own base damage and AoE, **plus 30–50% of Basic upgrade 1's** damage and AoE | **1** bigger initial explosion damage + AoE · **2** the zone also inherits Basic upgrade 1's base damage, AoE **and** modifiers |
| **EX (idea 1)** | Lay a carpet of fire from behind the tank to in front, along the joystick / facing direction; leaves several fire zones along the path | **1** more charges · **2** zone inherits from Basic upgrade 1 · **3** more damage and width |
| **EX (idea 2)** | Meteor rain onto targets currently on screen (random among equals, prioritising big mobs / bosses / special targets); each meteor's blast spawns fragments that explode on landing for 20–30% of the meteor's damage and AoE | **1** more meteors per wave · **2** more fragments · **3** more damage and width |

---

## 1. The single most important structural signal

Three of the four entries above end with the same sentence: **"the burning zone inherits
from Basic upgrade 1"**.

That is not three skills each doing their own thing. It is **one shared effect — the
burning zone — owned by the Basic attack and reused by the others at a percentage.**

If each skill spawns its own private zone code, that sentence has to be re-implemented
three times, and every future tuning change to "the burn" has to be made in three places
that will drift. The plumbing must therefore have a concept the original design
(`skill-plugin-system.md`) does not yet have:

### Effect modules — a skill is not the smallest unit

```csharp
[CreateAssetMenu(menuName = "ScriptableObjects/Effect Module")]
public class GroundZoneModuleSO : EffectModuleSO
{
    public float baseDamagePerTick;
    public float tickInterval;
    public float baseRadius;
    public float duration;
    public GameObject visualPrefab;
}
```

A skill then **references** the module and declares how much of it it gets:

```csharp
[System.Serializable]
public class ModuleContribution
{
    public EffectModuleSO module;          // "the burning zone"
    [Range(0f,1f)] public float inherit;   // 0.4 = "30~50% of the owner's version"
    public bool inheritModifiers;          // Sub upgrade 2 / EX upgrade 2 flip this on
    public float ownBaseDamage;            // the zone's own base, before inheritance
    public float ownBaseRadius;
}
```

Resolution when a zone is spawned:

```
damage = ownBaseDamage + inherit × (owner module's resolved damage)
radius = ownBaseRadius + inherit × (owner module's resolved radius)
if (inheritModifiers) apply the owner's modifiers too
```

Now "the burn zone" exists once. Basic upgrade 1 raises **the module**; Sub and EX
automatically follow at their percentage, exactly as written in the kit — no cross-skill
lookups, no duplicated tuning. Rebalancing the burn is one asset.

**This also answers a question the plumbing doc left open**: skills are not fully
independent plugins. They are independent in *behaviour*, and shared in *effect modules*.

---

## 2. Upgrade ladders belong to the skill, not to the global stat sheet

Every entry in the kit has "Nâng cấp 1 / 2 / 3", and those upgrades **change behaviour**,
not just numbers — upgrade 1 of Basic *adds a burning zone that did not exist before*;
upgrade 2 *adds a marking mechanic*. The current upgrade system cannot express this: it is
a flat list of stat names, each mapped to a global stat with a `+=`
(`SO/Modifier/UpgradeGeneration.cs:104-155`).

What the skill asset needs:

```csharp
public class SkillDefinitionSO : ScriptableObject
{
    // ... identity / activation / cost as in skill-plugin-system.md ...
    public List<SkillUpgradeSO> upgradeLadder;   // index = tier, 1..N
}

public class SkillUpgradeSO : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;        // the card text
    public int maxLevel = 5;                     // "cấp càng cao thì mark càng lâu"
    public AnimationCurve valuePerLevel;         // level → value; designer-authored, no code
    public List<ModuleContribution> unlocks;     // upgrade 1 of Basic unlocks the zone module
    public List<StatModifier> modifiers;         // flat/percentage changes
    public bool requiresPreviousTier = true;     // tier 2 needs tier 1 taken
}
```

Two things this buys:

- **Per-level scaling is authored as a curve, not typed as code.** "Cấp độ càng cao thì
  thời gian đánh dấu càng dài, AoE và sát thương nổ càng cao" becomes three curves on one
  asset. The designer tunes them without a programmer.
- **The level-up card generator stops being a hardcoded switch.** Today
  `UpgradeGeneration.SetupCard` has a `switch (name)` with ten string cases wiring ten
  methods. Once upgrades are assets, card generation is: take the equipped skills, collect
  their available next tiers, pick 3. Adding an upgrade never touches that file again —
  which is the same win as the slot system, applied to progression.

---

## 3. Status effects on enemies — currently there is nowhere to put them

"Đánh dấu kẻ địch trúng đòn hoặc bị thiêu đốt, gây 1 vụ nổ nếu kẻ địch chết khi bị đánh
dấu" needs three things the enemy does not have (`Enemy/GeneralEnemy.cs`):

1. **A place to hold a status** — no status container exists; the enemy has health, speed,
   iframes.
2. **Who applied it, and with what numbers** — `IDamageable.Damage(int damageAmount)`
   (`Interface/IDamageable.cs`) carries a bare integer. The mark's explosion has to know
   the mark's own AoE and damage, which came from the skill that applied it, at the level
   it was at. A bare int cannot carry that.
3. **A death hook with attribution** — `GeneralEnemy.Dead()` drops loot and returns to the
   pool. Nothing can react to "this enemy died **while marked**".

Minimum shape:

```csharp
public readonly struct DamagePacket          // replaces the bare int
{
    public readonly int    Amount;
    public readonly object Source;           // which slot / skill / module dealt it
    public readonly DamageTag Tag;           // Direct | Burn | Explosion — for iframe rules, see §6
}

public interface IStatusHost                 // GeneralEnemy implements
{
    void Apply(StatusInstance status);       // Burn, Mark, Slow, ...
    bool Has(StatusId id);
    event Action<IStatusHost, DamagePacket> Died;   // mark explosion listens here
}
```

`StatusInstance` carries `id`, `source`, `expiresAt`, and the resolved numbers it was
applied with — so a mark applied at skill level 4 still explodes for level-4 values even if
the player levels up in between, and two sources can mark independently without one
overwriting the other's numbers.

---

## 4. Charges — "tăng số lượng tái sử dụng chiêu thức"

EX idea 1 upgrade 1 adds **re-uses**, not cooldown reduction. The slot runner in
`skill-plugin-system.md` §3.4 tracks a single cooldown; it needs a charge count alongside:

```csharp
public int  MaxCharges     { get; }   // from the skill asset + its upgrade ladder
public int  ChargesLeft    { get; }
public float RechargeTimer { get; }   // one charge recovers per cooldown period
public bool IsReady => ChargesLeft > 0 && !IsRunning;
```

Single-charge skills are the `MaxCharges = 1` case, so this is not a special path — it
replaces the boolean cooldown rather than sitting beside it. Cheaper to build now than to
retrofit once three skills assume the boolean.

---

## 5. Targeting — meteors need an enemy registry, and enemies need a rank

EX idea 2 selects "targets currently on screen, random among equals, prioritising big
mobs / bosses / special targets". Neither half is available today:

- **No queryable list of live enemies.** `GameManager.enemyCount` is a counter
  (`Manager/GameManager.cs:43`); the pool in `Manager/MobPoolManager.cs` hands enemies out
  but nothing keeps a list to query. A meteor cannot ask "who is on screen?"
- **No rank on an enemy.** `SO/Stat/Enemy/EnemyStatSO.cs` has no elite/boss/special flag,
  so "ưu tiên quái to, trùm" has nothing to sort by.

Both are small and pay for themselves immediately (a HUD minimap, aggro logic, "kill the
elite" objectives all want the same registry):

```csharp
public interface ITargetProvider
{
    IReadOnlyList<IDamageable> ActiveEnemies { get; }
    // on-screen filter + weighted pick, priority by rank then random among equals
    IReadOnlyList<IDamageable> PickTargets(TargetQuery query);
}

public enum EnemyRank { Normal, Big, Elite, Boss, Special }
```

Registration is two lines in the existing pool get/release path — enemies are already
pooled through `IObjectPool<GeneralEnemy>`, so there is exactly one place to hook.

---

## 6. Two problems in the current code that this kit will hit immediately

These are not style notes. A fire kit built on damage-over-time will hit both on day one.

### 6.1 Invulnerability frames will eat almost all burn damage

`GeneralEnemy.Damage` returns early while `isIframe` is true, and **any** damage starts an
i-frame window (`Enemy/GeneralEnemy.cs:70, 81`). A burning zone ticking through
`OnTriggerStay2D` (`Projectiles/DamagePerTick.cs:25-33`) will therefore be silently
swallowed whenever the fireball also connects — and the whole kit is burn zones.

The fix is the `DamageTag` in §3: i-frames should apply to `Direct` hits only. `Burn` ticks
are already rate-limited by their own tick interval and do not need contact immunity.
Without this, upgrade 1 of the Basic attack will feel like it does nothing, and the cause
will not be obvious.

### 6.2 Zone damage cannot have its own base value

`DamagePerTick` deals `manager.playerStat.spDamage` — the one global skill-damage stat
(`Projectiles/DamagePerTick.cs:31`). The kit explicitly requires a zone with **its own base
damage and AoE, plus an inherited percentage**. That is unreachable while every ticking
thing reads the same global number. The effect module in §1 is what replaces it.

(It also damages anything tagged `Ground`, which looks unintended.)

---

## 7. Are the three slots free-form, or role-typed?

`skill-plugin-system.md` assumed any skill fits any of the three slots. This kit reads as
**one Basic + one Sub + one EX**, which is a different rule:

- **Role-typed** — slot 0 accepts Basic-role skills only, slot 1 Sub, slot 2 EX. Guarantees
  every build has an attack, keeps the HUD legible, and makes the mark/burn interlocks
  above dependable (the Sub can *always* assume a Basic exists to inherit from).
- **Free-form** — any skill anywhere. More build variety, but "inherit from Basic upgrade
  1" has to define what happens when there is no Basic equipped.

**Recommendation: role-typed**, precisely because this kit's inheritance chain assumes a
Basic is present. It is also the cheaper thing to relax later — going role-typed → free-form
is deleting a check, while the reverse means auditing every inheritance rule.

This directly supersedes open question 3 of the plumbing doc ("is an all-passive build
legal?"): under role-typing, it cannot happen by construction.

---

## 8. What this adds to the build order

Ordered so nothing is built twice:

1. `DamagePacket` + `DamageTag` replacing the bare int on `IDamageable` — everything else
   depends on attribution, and it is the smallest change that unblocks the most.
2. I-frame rule split (§6.1) — one condition, prevents the kit feeling broken.
3. Effect modules + `ModuleContribution` (§1) — the burning zone becomes one thing.
4. Slot runner with charges (§4) and the skill asset with an upgrade ladder (§2).
5. Status host + mark/burn (§3).
6. Enemy registry + rank + target query (§5) — only EX idea 2 needs this; it can lag.

Steps 1, 2 and 6 are useful even if the whole slot system were rejected.

---

## 9. Questions back to the kit's designer

1. **Do burning zones from different sources stack?** Basic upgrade 1, the Sub slam and
   the EX carpet can all leave zones on the same tile. Full stacking (three zones ticking
   at once), highest-only, or merge into one?
2. **What does the mark explosion scale from** — the mark's own stored values at the moment
   it was applied, or the current level at the moment of death? (§3 assumes stored-at-apply;
   it is the version that cannot be exploited by levelling mid-fight.)
3. **Can an enemy carry more than one mark** from different sources, and do they explode
   separately?
4. **"30~50%" — is that a designed range** (i.e. it becomes a tunable per skill) or a
   placeholder for one number to be picked later?
5. ~~Does the Basic attack auto-fire, or does the player press to throw?~~ **Answered: it
   auto-fires.** See §10.
6. ~~Is "joystick direction" a real gamepad path, or shorthand for aim direction?~~
   **Answered: move on the stick, aim with the mouse.** See §10.

---

## 10. Control scheme (answered by the owner)

| Input | Controls | Current code |
|---|---|---|
| Movement stick / WASD | tank body moves, body turns toward movement | `Player/MovementController.cs:28-48` — already this |
| Mouse cursor | aim: the turret and every skill point at the cursor | `Player/ShootPointController.cs:29-34` — already this |
| — | **basic attack fires by itself** on its own timer | **not this yet** — see below |

So the game is an auto-shooter with manual aim: the player steers and points, and never
presses a fire button.

**What has to change.** Shooting is currently gated on the mouse button being held
(`ShootPointController.cs:42`). Auto-fire means dropping that gate and letting the
attack-rate timer drive the loop on its own. Two consequences worth stating before it is
built:

- **"Attack rate" becomes the auto-fire interval**, i.e. the kit's "every 2 seconds". It is
  no longer a floor on how fast a player can click, so the upgrade that improves it is now
  strictly a rate change with no skill-expression component. That is the normal auto-shooter
  trade and matches the kit as written — just confirming it is intended.
- **The mouse button is now free.** If it stays unused, aiming is the only thing the mouse
  does. Worth deciding whether one of the three slots binds to it (right or left click)
  instead of a keyboard key — natural on this control scheme, and it is the difference
  between reaching for `R` mid-dodge or not.

**Aim source stays a single value, not a branch.** Because aim is the mouse and movement is
the stick, `SkillContext.AimWorldPos` (`skill-plugin-system.md` §3.3) is the cursor
position, resolved once per frame, and every skill reads it. The EX fire carpet's "along
the joystick or current facing direction" therefore resolves to **the aim direction**, which
is the same value — one source, no per-skill input branching. If a gamepad build ever
happens, only the code that fills `AimWorldPos` changes; no skill is touched.

One thing this makes cheap and worth doing now: keep aim in `Update`, not `FixedUpdate`
(`skill-plugin-system.md` §5.4). With no fire button, aim *is* the player's entire moment
to moment expression, and it currently updates at physics rate while the frame rate is
higher — the turret visibly trails the cursor. That fix matters more under auto-fire than it
did before.
