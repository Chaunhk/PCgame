using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A held beam weapon: one or more rays from the muzzle along the aim direction, damaging
/// everything they cross on their own tick.
/// </summary>
// WHY: "a laser" and "a three-beam spread" are the same behaviour with a different beam count, so
// they are one script and two assets rather than two scripts. That is the whole point of skills
// being data — the second one costs an asset, not a file.
//
// Rays are cast rather than given colliders: a collider beam would need pooling, and its hits would
// land on whatever physics frame it happened to overlap. Casting on the skill's own tick means the
// damage rate is the number on the asset, not a property of the physics step.
public class BeamBehaviour : SkillBehaviour
{
    public const string P_BeamCount = "beamCount";
    public const string P_SpreadAngle = "spreadAngle";
    public const string P_Range = "range";
    public const string P_Width = "width";
    public const string P_DamagePerTick = "damagePerTick";
    public const string P_TickInterval = "tickInterval";

    [Tooltip("Material for the beam line. Leave empty to use the line renderer's own material.")]
    [SerializeField] private Material _beamMaterial;
    [SerializeField] private Color _beamColor = new Color(1f, 0.3f, 0f, 0.85f);
    [SerializeField] private LayerMask _hitMask = ~0;

    private readonly List<LineRenderer> _lines = new List<LineRenderer>();
    private readonly List<IDamageable> _hitThisTick = new List<IDamageable>();
    private float _nextTickAt;
    private bool _firing;

    public override void OnUnequip(SkillContext ctx)
    {
        for (int i = 0; i < _lines.Count; i++)
            if (_lines[i] != null) Destroy(_lines[i].gameObject);

        _lines.Clear();
    }

    public override void OnActivate(SkillContext ctx)
    {
        EnsureLines(Mathf.Max(1, ctx.Values.GetInt(P_BeamCount, 1)));
        _firing = true;
        _nextTickAt = Time.time;   // the first tick lands immediately, so the beam bites on contact
        SetVisible(true, ctx.Values.GetInt(P_BeamCount, 1));
    }

    public override void OnDeactivate(SkillContext ctx)
    {
        _firing = false;
        SetVisible(false, _lines.Count);
    }

    public override void OnTick(SkillContext ctx, float deltaTime)
    {
        if (!_firing) return;

        int count = Mathf.Max(1, ctx.Values.GetInt(P_BeamCount, 1));
        EnsureLines(count);

        float spread = ctx.Values.Get(P_SpreadAngle, 0f);
        float range = ctx.Values.Get(P_Range, 10f);
        float width = ctx.Values.Area(P_Width);
        bool damaging = Time.time >= _nextTickAt;

        if (damaging) _nextTickAt = Time.time + Mathf.Max(0.05f, ctx.Values.Get(P_TickInterval, 0.2f));

        Vector2 origin = ctx.Muzzle.position;
        float aim = Mathf.Atan2(ctx.AimDirection.y, ctx.AimDirection.x) * Mathf.Rad2Deg;

        // WHY: cleared once per tick, not per beam. Three beams overlapping on one enemy should not
        // deal triple damage to it — a spread is meant to cover more targets, not stack on one.
        if (damaging) _hitThisTick.Clear();

        for (int i = 0; i < count; i++)
        {
            float offset = count == 1 ? 0f : spread * (i - (count - 1) / 2f);
            float angle = (aim + offset) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            DrawBeam(_lines[i], origin, origin + direction * range, width);

            if (!damaging) continue;

            int damage = ctx.Values.Damage(P_DamagePerTick);
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, range, _hitMask);

            for (int h = 0; h < hits.Length; h++)
            {
                IDamageable target = hits[h].collider.GetComponent<IDamageable>();
                if (target == null || target is PlayerManager) continue;
                if (_hitThisTick.Contains(target)) continue;

                _hitThisTick.Add(target);
                target.Damage(new DamagePacket(damage, DamageTag.Burn, this));
            }
        }

        for (int i = count; i < _lines.Count; i++)
            if (_lines[i] != null) _lines[i].enabled = false;
    }

    private void EnsureLines(int count)
    {
        while (_lines.Count < count)
        {
            var go = new GameObject($"Beam_{_lines.Count}");
            go.transform.SetParent(transform, false);

            LineRenderer line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.numCapVertices = 4;
            line.textureMode = LineTextureMode.Tile;
            if (_beamMaterial != null) line.material = _beamMaterial;
            line.startColor = _beamColor;
            line.endColor = _beamColor;
            line.enabled = false;

            _lines.Add(line);
        }
    }

    private void DrawBeam(LineRenderer line, Vector2 from, Vector2 to, float width)
    {
        if (line == null) return;

        line.enabled = _firing;
        line.widthMultiplier = Mathf.Max(0.02f, width);
        line.SetPosition(0, new Vector3(from.x, from.y, 0f));
        line.SetPosition(1, new Vector3(to.x, to.y, 0f));
    }

    private void SetVisible(bool visible, int count)
    {
        for (int i = 0; i < _lines.Count; i++)
            if (_lines[i] != null) _lines[i].enabled = visible && i < count;
    }
}
