# Design — Tank & skill config tool (editor GUI)

> Third document in the set. `skill-plugin-system.md` defines how a skill is wired;
> `skill-content-requirements.md` works out what the fire kit needs; this one defines
> **how a designer authors all of it without opening code or hand-making assets.**
>
> Requirement, in the owner's words: configure which tank has which stats, which skills,
> and what numbers — **and the same skill attached to a different tank may have different
> numbers.**

---

## 1. What that last sentence costs

"The same skill on a different tank has different numbers" is the requirement that decides
the whole data model. It rules out the obvious layout, where a skill asset holds its
numbers and a tank holds a list of skill assets — under that layout, two tanks sharing
`Fireball` share its cooldown, and giving one tank a faster fireball means duplicating the
asset. Duplicated assets drift: fix a bug in `Fireball` and you have fixed it in one of
the four copies.

So the numbers cannot live *only* on the skill. They live in **three layers**, and the
tool's whole job is to make those layers visible:

```
skill asset default        "Fireball costs 10 mana, 2s cooldown"     ← authored once
   ↓ overridden by
tank binding override      "on Heavy Tank, this one costs 18"        ← sparse, per tank
   ↓ modified at runtime by
upgrades + passives        "+2 area from a level-up card"            ← per run, temporary
```

**Sparse is the important word.** A tank binding stores *only* the fields it changes. If a
tank overrides nothing, it inherits everything, forever — including future edits to the
skill. An override that merely restates the default is worse than no override, because it
silently stops tracking the source. The tool must therefore show, for every field, whether
the value is inherited or overridden, and make reverting to inherited one click.

---

## 2. Data model

```csharp
[CreateAssetMenu(menuName = "ScriptableObjects/Tank")]
public class TankDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string tankId;                 // stable key for saves — never rename
    public string displayName;
    public Sprite portrait;
    public GameObject bodyPrefab;

    [Header("Base stats")]
    public PlayerStatSO baseStats;        // the existing stat asset, per tank

    [Header("Loadout")]
    public SkillBinding basicSlot;
    public SkillBinding subSlot;
    public SkillBinding exSlot;
}

[System.Serializable]
public class SkillBinding
{
    public SkillDefinitionSO skill;
    public List<ParameterOverride> overrides;   // sparse — only what differs
    public List<SkillUpgradeSO> allowedUpgrades;// this tank may not get every upgrade
}

[System.Serializable]
public struct ParameterOverride
{
    public string parameterId;   // matches a parameter the skill declares — see §3
    public float  value;
}
```

Three slots are named rather than an array of three, matching the role-typed decision in
`skill-content-requirements.md` §7. That makes an invalid loadout unrepresentable: you
cannot put an EX skill in the basic slot, and no tank can ship without a basic attack.

---

## 3. Skills declare their own parameters — or the tool rots

The naive tool hardcodes a form: cooldown field, mana field, damage field. Then someone
adds a skill with "number of meteors" and "fragment count", and the tool has to be edited.
After four skills the tool is a pile of special cases and the designer is back in the
inspector.

Instead, **the skill asset declares what is tunable about it**, and the GUI is generated
from that declaration:

```csharp
[System.Serializable]
public struct SkillParameter
{
    public string id;             // "meteorCount" — stable, used by overrides
    public string label;          // "Meteors per wave"
    public string tooltip;        // why this exists / what it affects — shown on ⓘ
    public ParamType type;        // Int | Float | Percent | Seconds
    public float defaultValue;
    public float min, max;        // slider range + validation bound
}
```

The skill's behaviour reads its numbers through `ctx.Values.Get("meteorCount")` rather than
from a hand-typed field. Adding a tunable to a skill = adding one entry to its parameter
list. **The tool never changes.** A skill written six months from now gets a full editing
form for free, with sliders, ranges and hover help.

The `tooltip` field is not decoration. Six months later nobody remembers whether
`spreadAngle` is total spread or half-spread, and the resulting mis-tune is invisible until
playtest. Requiring one sentence at authoring time is the cheapest documentation this
project can buy.

---

## 4. The window

One Unity editor window, `Tools → Tank Designer`, three panes:

```
┌────────────────┬──────────────────────────────┬───────────────────────────┐
│ TANKS          │ HEAVY TANK                   │ SKILL: Fireball  (Basic)  │
│                │                              │                           │
│ ▸ Scout        │ Base stats                   │ Cooldown    2.0s    ⓘ [↺] │
│ ▸ Heavy   ◀    │   Max health   [ 220 ]       │ Mana cost   ▓18▓    ⓘ [↺] │  ← bold = overridden
│ ▸ Artillery    │   Max mana     [ 100 ]       │ Damage      12      ⓘ     │
│                │   Move speed   [ 3.5 ]       │ Burn inherit 40%    ⓘ     │
│ [+ New tank]   │                              │                           │
│ [Duplicate]    │ Loadout                      │ Upgrades allowed          │
│                │   Basic  [Fireball      ▾] ◀ │   ☑ Burning ground        │
│                │   Sub    [Ground Slam   ▾]   │   ☑ Mark on hit           │
│                │   EX     [Meteor Rain   ▾]   │   ☐ (tier 3, not built)   │
│                │                              │                           │
│                │ ⚠ 1 issue                    │ [Revert all to default]   │
└────────────────┴──────────────────────────────┴───────────────────────────┘
```

Behaviour that matters:

- **Overridden fields are visually distinct** (bold + a marker) and carry a revert button
  that deletes the override rather than writing the default value back. Inherited fields
  show the skill's value greyed, with the source asset name on hover.
- **Dropdowns are filtered by role** — the Basic slot only lists Basic-role skills. Wrong
  assignments are not possible, so they need no error message.
- **Nothing is hand-created.** `[+ New tank]` and `[+ New skill]` create the asset, name
  the file from the id, and put it in the right folder. Hand-making ScriptableObjects
  through the project window is how you end up with two assets named `Data.asset`.
- **Duplicate tank** copies base stats and bindings including overrides — the fastest path
  to a variant, which is what balancing actually consists of.
- **Validation runs continuously**, listed inline, not thrown at build time: an empty slot,
  an override pointing at a parameter the skill no longer declares (stale — offer to
  delete), a module contribution above 100%, a value outside its declared range, a
  duplicate `tankId`, an upgrade ladder with a gap.

### 4.1 Balance table

A second tab: every tank × every parameter as a grid, one row per tank, editable in place.
This is the view that answers "is Artillery's cooldown out of line with everyone else's",
which the per-tank form cannot show. Sort by column, and a CSV export for anyone who wants
to do the maths in a spreadsheet.

### 4.2 Live tuning during play

While the editor is in play mode, edits push into the running session immediately. Tuning a
cooldown by feel is a five-second loop instead of a stop-edit-restart minute. Two rules
that keep it honest: values changed during play are marked, and on exit the window asks
whether to keep or discard them — silently persisting play-mode edits, or silently losing
them, are both ways to lose an afternoon's tuning.

---

## 5. Rules the tool must follow

1. **Assets stay plain ScriptableObjects.** The tool is a *view*; it invents no file format
   of its own. A designer who does not like the window can still edit in the inspector, and
   more importantly the data stays reviewable in a pull request. A tool that writes one
   opaque JSON blob makes every balance change an unreviewable diff.
2. **Stable ordering on write.** Overrides serialize sorted by `parameterId`, always. An
   editor that reorders a list on every save turns a one-number change into a fifty-line
   diff and makes merges between two designers conflict for no reason.
3. **Ids are never renamed silently.** `tankId`, `skillId` and `parameterId` are keys in
   save data and in overrides. The tool offers a rename that rewrites every reference, or
   it refuses — it never lets a field be edited into orphaning something.
4. **Editor-only code lives in an `Editor/` folder** with its own assembly definition. This
   project currently has `using UnityEditor;` inside runtime scripts, which breaks player
   builds (see `skill-plugin-system.md` §5.1). A config tool is exactly the thing that
   spreads that mistake if it is not fenced off from the start.
5. **The tool validates; it does not compute gameplay.** Resolution order (§1) is
   implemented once in runtime code and the window calls it to *preview* results. If the
   window has its own copy of the maths, the preview and the game disagree eventually, and
   the designer's trust in the tool is gone the first time it happens.

---

## 6. Build order

The tool is worth building early — but not before the data model it edits exists, or it
gets rewritten with it.

1. `TankDefinitionSO` + `SkillBinding` + sparse overrides, and the three-layer resolution
   in runtime code (§1, §2).
2. `SkillParameter` declarations on the skill asset, and behaviours reading through
   `ctx.Values` (§3).
3. The window: tank list, base stats, loadout dropdowns, generated parameter form with
   inherit/override display (§4).
4. Validation panel (§4, rule list).
5. Balance table + CSV (§4.1).
6. Play-mode live tuning (§4.2).

Steps 1–3 are the useful minimum. Steps 4–6 each pay for themselves once there is more than
one tank to compare.

---

## 7. Questions

1. **How many tanks are planned?** Three or four justifies the balance table early; a dozen
   makes it the primary view rather than a second tab.
2. **Do tanks share skills at all**, or does each tank get its own exclusive kit? If kits
   are exclusive, per-tank overrides matter much less and the model can be simpler. The
   requirement as stated implies sharing.
3. **Are base stats per tank, or one shared sheet with per-tank offsets?** The model above
   assumes a `PlayerStatSO` per tank, which is the simpler thing to author but duplicates
   values that are the same across every tank.
4. **Should upgrade ladders be per tank too** (the same skill offering different upgrades
   on different tanks), or is the ladder fixed to the skill? The model above allows the
   former via `allowedUpgrades`, but it is easy to drop if it is not wanted.
