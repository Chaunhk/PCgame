# Design — Plugin Skill System (3 flex slots per tank)

> Status: proposal, for review. No gameplay code changed yet.
> Target: `Assets/Scenes/Scripts/Skills/`
> Goal: a tank carries **3 skill slots**, each slot filled by a **skill asset**. Adding a
> new skill must mean *creating one asset + one behaviour script* — never editing the
> tank, the input code, or the HUD.
>
> **Companion document**: `skill-content-requirements.md` takes the owner's first real kit
> (fireball / ground slam / EX) and works out what this plumbing must gain to express it —
> shared effect modules, per-skill upgrade ladders, status effects, charges, targeting. It
> revises two things stated here: slots are recommended to be **role-typed** (Basic / Sub /
> EX) rather than free-form, which supersedes open question 3 below.
>
> **Third document**: `tank-config-tool.md` — the editor GUI a designer uses to author all
> of this (which tank has which stats and skills, and what numbers), including per-tank
> overrides so the same skill can have different numbers on a different tank. That adds one
> layer to §3.2 below: a skill's numbers resolve as *skill default → tank override → run
> modifiers*, so `SkillContext.Values` is built from the tank's binding, not from the skill
> asset alone.

---

## 1. Where we are today

Two skills exist, `Canon` (Assets/Scenes/Scripts/Projectiles/Canon.cs) and `Laser`
(`.../Laser.cs`). Both work, but they are wired in a way that does not extend to three
swappable slots:

| # | Observation | File | Consequence |
|---|---|---|---|
| 1 | The tank's aim script holds `Canon canon` and `Laser laser` as direct fields, and hardcodes `KeyCode.E` / `KeyCode.Q` | `Player/ShootPointController.cs:14-15, 47-61` | A third skill needs an edit to this file, a new field, a new key branch. Slots cannot be swapped at runtime. |
| 2 | Each skill re-implements the same 4 steps: mana check → cooldown check → start cooldown → consume mana | `Canon.cs:24-31`, `Laser.cs:71-81` | Copy-paste divergence is already visible: `Canon` never calls `OnSkillStart`, `Laser` does. `Canon` starts its cooldown on press, `Laser` on release. Neither is wrong, but nothing enforces a rule. |
| 3 | Skill tuning lives in one global stat asset: `spCost`, `spMod`, `spDamage` | `SO/Stat/Player/PlayerStatSO.cs:24-26` | **This is the actual blocker.** All skills share one cost, one size multiplier, one damage number. Three different skills cannot have three different costs or cooldowns. |
| 4 | `SkillBar.InitData(cdValue)` is never called anywhere in the project | `UI/GameBars/SkillBar.cs:22-28` | Every skill's cooldown is silently the hardcoded default `cdTime = 5f`. Cooldown is not designable today. |
| 5 | `Canon._aciveCost` is a hand-typed inspector value; `Laser` reads its cost from the stat asset | `Canon.cs:15`, `Laser.cs:22` | Two sources of truth for the same concept. |
| 6 | `SkillIconControl` is two static-style helpers that just forward to `SkillBar` | `UI/GameBars/SkillIconControl.cs` | An indirection layer with no state; the slot object below replaces it. |

**None of this is bad code for a prototype.** It is the normal shape a project takes
before the third instance of a thing shows up. The third instance is showing up now.

---

## 2. The idea in one paragraph

Stop treating a skill as *a script the tank knows about*. Treat it as **data**: a skill is
an asset in the project (cost, cooldown, icon, how it activates) plus **one behaviour
script that only knows how to do its own effect** — spawn a shell, draw a laser, drop a
mine. Everything that is the *same* for every skill (can I afford it, is it on cooldown,
is the key held, update the HUD) is done **once** by a slot runner. The tank owns three
slots. A slot holds any skill asset, or nothing.

```
        ┌──────────────────────────────────────────────┐
        │ TankSkillLoadout (on the tank prefab)        │
        │   slots[0]  slots[1]  slots[2]               │
        └───┬──────────────┬──────────────┬────────────┘
            │              │              │            each slot owns:
      ┌─────▼─────┐  ┌─────▼─────┐  ┌─────▼─────┐      · the asset it holds
      │ SkillSlot │  │ SkillSlot │  │ SkillSlot │      · its own cooldown timer
      └─────┬─────┘  └─────┬─────┘  └─────┬─────┘      · its own behaviour instance
            │              │              │            · its own HUD element
    ┌───────▼──────┐ ┌─────▼──────┐ ┌─────▼──────┐
    │ CannonShot   │ │ LaserBeam  │ │ (empty)    │  ← behaviours: effect only,
    │ (asset+code) │ │(asset+code)│ │            │    no mana/cooldown/UI code
    └──────────────┘ └────────────┘ └────────────┘
```

Adding skill #12 = make a new asset, write a behaviour with 3 methods, drop it in a slot.
No file in the list above changes.

---

## 3. The contracts

### 3.1 How a skill activates

Three shapes cover everything currently planned. The shape is declared in the asset so
the slot runner knows what to expect from the key.

| Mode | Key behaviour | Example | Cooldown starts |
|---|---|---|---|
| `Instant` | press → fires once | cannon shell, mine drop, dash | on press |
| `Channeled` | hold → runs while held, drains per tick | laser | on release |
| `Toggle` | press on, press off | shield aura, magnet field | on turn-off |
| `Passive` | **no key at all** — works from the moment it sits in the slot | +armour, burn on hit, extra pickup range | never |

`Passive` is a first-class mode, not a special case bolted on: it occupies one of the three
slots, shows up in the same HUD row, and is swapped the same way. It simply never receives
a key press. Section 3.7 covers what a passive can actually *do*.

### 3.2 The skill asset

```csharp
[CreateAssetMenu(menuName = "ScriptableObjects/Skill")]
public class SkillDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string skillId;              // stable key for save data — never rename
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Activation")]
    public SkillActivation activation = SkillActivation.Instant;
    public float cooldown = 5f;         // ignored by Passive
    public int manaCostOnActivate = 10; // ignored by Passive
    public int manaCostPerTick;         // Channeled / Toggle only
    public float tickInterval = 0.25f;  // Channeled / Toggle only

    [Header("Effect")]
    public GameObject behaviourPrefab;  // carries one ISkillBehaviour component
    public float baseDamage;
    public float baseArea = 1f;

    [Header("Scaling — which global stats touch this skill")]
    public bool scalesWithSpDamage = true;
    public bool scalesWithSpArea   = true;
    public bool scalesWithSpCost   = true;
}
```

**Note on point 3 in the table above.** `spCost` / `spMod` / `spDamage` do not disappear —
they change job. Today they *are* the skill's numbers. In this design the **asset holds the
base number** and the player stat becomes a **modifier applied on top**, which is what the
upgrade cards already want to be (`UpgradeSPMod` multiplies, `UpgradeManaCost` subtracts —
see `SO/Modifier/UpgradeGeneration.cs:148-150`). One line each:

```
effectiveCost   = max(1, def.manaCostOnActivate - (def.scalesWithSpCost   ? stat.spCost   : 0))
effectiveDamage = def.baseDamage + (def.scalesWithSpDamage ? stat.spDamage : 0)
effectiveArea   = def.baseArea   * (def.scalesWithSpArea   ? stat.spMod    : 1)
```

This keeps every existing upgrade card working unchanged, while letting each skill start
from its own numbers.

### 3.3 The behaviour a skill author writes

This is the whole surface a new skill has to implement. No mana, no cooldown, no UI.

```csharp
public interface ISkillBehaviour
{
    /// Called once when the skill enters a slot. A Passive does all its work here.
    void OnEquip(SkillContext ctx);

    /// Called once when the skill leaves a slot. A Passive undoes its work here.
    void OnUnequip(SkillContext ctx);

    /// Instant: do the effect. Channeled/Toggle: start it. Passive never receives this.
    void OnActivate(SkillContext ctx);

    /// Channeled/Toggle only, every frame while running. Instant/Passive never receive this.
    void OnTick(SkillContext ctx, float deltaTime);

    /// Channeled: key released, mana ran out, or player died. Toggle: turned off.
    void OnDeactivate(SkillContext ctx);

    /// Behaviour can end itself early (beam hit its limit, ammo spent).
    bool WantsToStop { get; }
}
```

`OnEquip` / `OnUnequip` are paired on purpose: **every** effect a skill applies must be
removable, because slots are swappable. A passive that adds armour on equip and forgets to
remove it on unequip turns skill-swapping into a stat exploit. Making the pair mandatory in
the interface is what prevents that class of bug from ever being written.

`SkillContext` is the read-only bundle a behaviour needs, handed in rather than fetched
from the singleton — so a behaviour is testable and does not care where the tank came from:

```csharp
public readonly struct SkillContext
{
    public readonly Transform Owner;        // tank root
    public readonly Transform MuzzlePoint;  // today: GameManager.shootPoint
    public readonly Vector2   AimWorldPos;  // cursor in world space, computed once per frame
    public readonly PlayerStatSO Stats;
    public readonly IResourcePool Resources;// mana spend/query — PlayerManager implements it
    public readonly SkillRuntimeValues Values; // effectiveDamage / area / cost, precomputed
}
```

### 3.4 The slot runner — the only place the shared rules live

```csharp
public class SkillSlot                      // plain C# class, not a MonoBehaviour
{
    public SkillDefinitionSO Definition { get; private set; }
    public float CooldownRemaining { get; private set; }
    public bool  IsReady   => Definition != null && CooldownRemaining <= 0f && !IsRunning;
    public bool  IsRunning { get; private set; }

    public event Action<SkillSlot> Changed; // HUD subscribes; no polling

    public void Equip(SkillDefinitionSO def, SkillContext ctx);
    public void Unequip();

    public bool TryActivate(SkillContext ctx);   // gate: ready? affordable? → spend, run, notify
    public void Tick(SkillContext ctx, float dt);// cooldown countdown + channel tick + tick cost
    public void Release(SkillContext ctx);       // channel end → start cooldown
}
```

The gate inside `TryActivate` is written **once**:

```
if (!IsReady) return false
if (!Resources.CanAfford(Values.Cost)) return false
Resources.Spend(Values.Cost)
behaviour.OnActivate(ctx)
if (activation == Instant) StartCooldown()   else IsRunning = true
Changed?.Invoke(this)
```

That single block replaces the duplicated logic in `Canon.cs:24-31` and `Laser.cs:71-81`,
and removes the divergence noted in observation 2.

### 3.5 Loadout + input

```csharp
public class TankSkillLoadout : MonoBehaviour
{
    public const int SlotCount = 3;
    [SerializeField] private SkillDefinitionSO[] startingSkills = new SkillDefinitionSO[SlotCount];
    [SerializeField] private KeyCode[] slotKeys = { KeyCode.Q, KeyCode.E, KeyCode.R };

    public SkillSlot this[int index] { get; }
    public event Action<int, SkillSlot> SlotChanged;   // HUD + save layer listen

    public bool Equip(int slotIndex, SkillDefinitionSO def);  // runtime swap, used by pickups/cards
    public int  FindSlotOf(SkillDefinitionSO def);
    public int  FirstEmptySlot();
}
```

Input reads the key list, not hardcoded letters — so remapping is a data change, and a
gamepad or mobile button layer later only has to call `TryActivate(i)` / `Release(i)`.

### 3.6 HUD

One `SkillSlotView` per slot, subscribing to `SkillSlot.Changed` and reading
`CooldownRemaining`. `SkillIconControl` (observation 6) goes away; `SkillBar` stays as the
fill-image widget but gets its cooldown length from the slot instead of its own
never-initialised `cdTime` field (observation 4).

A passive slot shows its icon with no cooldown fill and no key hint. It is visible in the
same row so the player can read their whole build in one glance.

### 3.7 Passive skills

A passive occupies a slot and runs without input. There are two kinds, and they need
different machinery — worth separating now, because the second one is where passives
usually turn into spaghetti.

**(a) Static passive — "the numbers are different now."**
`+20 max health`, `+15% area`, `+1 projectile`. The behaviour applies its change in
`OnEquip` and removes it in `OnUnequip`.

The trap: today the runtime stat asset (`playerStat`) is mutated in place — the upgrade
cards do `playerStat.damage += val` (`SO/Modifier/UpgradeGeneration.cs:146-155`). That is
fine for upgrades, which are permanent for the run and reset at run start
(`Manager/GameManager.cs:55`). It is **not** fine for passives, which must be removable
when the slot is swapped: subtracting back what you added drifts the moment two sources
touch the same stat, or a cap clamps in between (`GameManager.UpgradeCapCheck`).

The fix is a thin modifier layer, and it is small:

```csharp
public readonly struct StatModifier
{
    public readonly StatId  Stat;    // MaxHealth, Damage, SpArea, PickupRadius, ...
    public readonly StatOp  Op;      // Add | Multiply
    public readonly float   Value;
    public readonly object  Source;  // the slot that owns it — how removal finds it
}
```

Resolution order, evaluated whenever the modifier list changes (not per frame):

```
base stat (playerBaseStat)
  → + permanent upgrade values      (what the cards already do)
  → + additive modifiers            (passives)
  → × multiplicative modifiers      (passives)
  → clamp to playerStatCap          (existing UpgradeCapCheck rule)
  → write into playerStat
```

Removal is then `modifiers.RemoveAll(m => m.Source == slot)` followed by a re-resolve —
exact, order-independent, and impossible to drift. This also fixes a smaller existing
issue: recomputation currently happens only when an upgrade card is clicked, so anything
that changes a stat by another path never reaches the HUD bars.

**(b) Reactive passive — "when X happens, do Y."**
`burn on hit`, `heal on kill`, `reflect when damaged`, `drop a mine every 5s`. These need
game events, but a fat interface where every behaviour implements every hook is exactly
what we are trying to avoid. Use opt-in interfaces instead — a behaviour implements only
what it reacts to, and the dispatcher only ever calls the ones that opted in:

```csharp
public interface ISkillOnHit      { void OnHit(SkillContext ctx, IDamageable target, int damage); }
public interface ISkillOnKill     { void OnKill(SkillContext ctx, IDamageable victim); }
public interface ISkillOnDamaged  { void OnDamaged(SkillContext ctx, int amount); }
public interface ISkillPeriodic   { float Interval { get; } void OnInterval(SkillContext ctx); }
```

The loadout sorts its behaviours into these buckets once, at equip time. A slot holding a
`+20 health` passive therefore costs **zero** per frame and zero per hit — it is not in any
bucket. The existing damage interface `Interface/IDamageable.cs` is the natural place to
raise `OnHit` / `OnKill` from, so this does not need a new event system.

**Mana and passives.** Per the owner's decision, all three slots draw on the **one shared
mana pool**; a passive costs no mana. If a passive should cost sustained mana ("shield aura
drains while up"), model it as a `Toggle` that starts switched on — do not invent a fourth
resource rule for it.

**Cost of a passive = a slot.** With three slots and passives allowed in any of them, a
player can run three passives and no active skill. That is a real build, and probably a
good one to allow — but it should be a deliberate choice, not something we discover in
playtest. See open question 3.

---

## 4. Migrating the two existing skills

Neither skill is rewritten — each is **narrowed**.

**Cannon** — delete the mana check, the mana spend, and the cooldown calls from
`EnableFire`; what remains is "place at the muzzle, activate, spawn the fire effect
scaled by area". That becomes `CannonBehaviour.OnActivate`. Cost `_aciveCost` and the
cooldown move into a `Cannon.asset`.

**Laser** — `EnableLaser` → `OnActivate` (turn on the line + hitbox), `UpdateLaser` →
`OnTick` (aim it), `DisableLaser` → `OnDeactivate`. The per-tick mana drain and its
`_costDelay` coroutine are deleted: the slot runner does `manaCostPerTick` every
`tickInterval`, and stops the channel by itself when mana runs out.

Estimated: ~40 lines removed from the two files, ~0 behaviour change for the player.

---

## 5. Things worth fixing while we are in here

These are pre-existing and independent of the slot system; the first two will bite soon.

1. **`using UnityEditor;` in runtime scripts** — `Projectiles/Canon.cs:4`,
   `Player/ShootPointController.cs:3`, and `using UnityEditor.Search.Providers` in
   `DataStruct.cs:3`. These compile in the editor and **fail the moment you make a real
   build** (`UnityEditor` does not ship in a player). They look like stray auto-imports.
   One-line deletions.

2. **`ConsumeMana` reports the wrong thing** — `Player/PlayerManager.cs:143-149` deducts,
   then returns `ManaCheck(val)`, i.e. *"could I afford a second one?"*, not *"did this
   spend succeed?"*. `Laser.cs:63-67` trusts that return value, so the beam cuts out one
   full activation-cost early. Correct shape: check, spend, return `true`; return `false`
   without spending otherwise.

3. **Health regen never runs** — `HealthRegenLoop` exists but only `ManaRegenLoop` is
   started (`PlayerManager.cs:29`), so `healthRegen` and its upgrade card do nothing. The
   suppression half is missing too: `DamagedDelay` — the coroutine that pauses regen after
   being hit — has no caller, so `_isHealthRegenBlocked` is never set. **Owner's answer:
   health regen is meant to work exactly like mana regen** — tick on the same interval,
   and pause for a window after the triggering event (damage taken, mirroring how skill use
   pauses mana). So the fix is two lines: start the loop next to `ManaRegenLoop`, and call
   `DamagedDelay` from `Damage`. No new mechanic, just the wiring that was never connected.

4. **Aim is computed in the physics loop** — `ShootPointController.FixedUpdate:29-34`
   computes the cursor world position, while `Update` shoots with it. At 50 Hz physics vs
   a higher frame rate the turret visibly lags the cursor and the laser aims at a stale
   point. Compute aim once in `Update`, keep only rigidbody work in `FixedUpdate`.
   (The `SkillContext.AimWorldPos` above assumes this fix.)

5. **A new `Material` is allocated at runtime and never released** —
   `Laser.cs:29-32` calls `new Material(Shader.Find(...))`. `Shader.Find` is a string
   lookup, and the instance leaks per laser. Serialize a material asset instead.

6. **Pooling is a linear scan** — `GameManager.listBullet` / `listFire` are scanned for
   `!activeSelf` on every shot (`ShootPointController.cs:68-79`, `Canon.cs:53-62`), and
   silently do nothing when the pool is exhausted. A small shared pool keyed by prefab,
   handing out from a queue, fixes both the cost and the silent failure — and skills need
   it anyway once there are more than two spawners.

Each is small and independent; they can ship as separate commits so review stays cheap.

---

## 6. Decisions already taken

- **Three slots per tank**, any skill in any slot.
- **One shared mana pool** across all three slots — no per-slot resource, no per-slot
  charges. A sustained-cost passive is modelled as an always-on `Toggle`, not as a new
  resource rule.
- **Passive skills are in scope** and are a first-class activation mode (section 3.7).
- **Skill content — which skills exist and what they do — is designed by the game's
  owner.** This document defines the plumbing only; it deliberately does not propose a
  skill list.

## 7. Open questions

1. **Where do skills come from?** A pre-run loadout screen, or picked up in-run? The
   `CardBodySkill` upgrade-card prefab already exists but is unused — if skills are
   drafted in-run from level-up cards, what happens when all 3 slots are full: forced
   replace, offer a choice, or stop offering skill cards?
2. **Can a slot be re-filled mid-run?** This is the question that decides how strict the
   `OnEquip` / `OnUnequip` pairing has to be policed. If slots are locked once filled,
   removal is a cold path; if they can be swapped freely, every passive's removal path is
   as hot as its apply path and deserves a test.
3. **Is an all-passive build legal?** Three passives and no active skill is currently
   possible by construction. Allow, or require at least one active slot?
4. **Keys** — `Q` `E` `R`, or `1` `2` `3`? Right mouse for slot 1?
5. **Should a skill be able to modify basic attack** (e.g. "next 5 shots pierce")? A
   reactive passive can already react to a hit; changing the shot *before* it is fired is
   a different hook, and would add one more opt-in interface (`ISkillOnShoot`) plus a call
   site in the shooting path.
6. **Are enemies going to use the same system?** If yes, the slot runner should take an
   `IResourcePool` owner rather than `PlayerManager` specifically — cheap now, painful
   later.
