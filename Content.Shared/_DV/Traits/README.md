# Traits

Conditions, effects, and the editor UI were ported from
[Delta-V #5208](https://github.com/DeltaV-Station/Delta-v/pull/5208)
(4364eaa388ad2589cb9a654f0da8d42e1efb2b2d).

QB keeps the existing prototypes in `Content.Shared.Traits`. Existing trait IDs,
categories, costs, organ targets, job specials, gear, and subcategory exclusions
still work. There is no database reset or required YAML migration.

New traits can use conditions and effects:

```yaml
- type: trait
  id: ExampleTrait
  name: trait-example-name
  description: trait-example-desc
  category: Traits
  cost: 1
  conditions:
  - !type:InDepartmentCondition
    department: Medical
  effects:
  - !type:AddCompsEffect
    components:
    - type: Pacified
```

All conditions must pass. Each supports `invert: true`. Available conditions
check a job, department, species, or component. The editor previews job conditions
when a job loadout is open; the server checks the actual assigned job at spawn.
Component conditions are checked on the server.

`AnyOfCondition` accepts a list of `conditions` and passes when at least one
passes. `TraitDependencyCondition` supports `requires` (all must be selected)
and `conflicts` (none may be selected). Both support inversion. Conditions that
need server information remain undecided in the lobby, including inside OR groups.
The server rechecks dependencies and budgets when another trait is rejected.

Effects add, replace, or remove components, or spawn an item in hand. Effects
target the body; keep `organ` with `components` for existing organ traits.
Existing `components` and `specials` run before `effects`, then `traitGear`.
Higher `priority` traits are applied first; equal priorities retain cost order.
When converting a trait, remove the old field for any behavior moved into an effect.
Randomized traits use the same conditions and effects, while retaining their
existing exemption from point limits.

`conflicts` lists incompatible trait IDs; either direction prevents selection.
Existing subcategory exclusions also apply. Negative costs are counted first
so saved selections do not depend on their order.

Categories retain `maxTraitPoints` and can also use `maxTraits`, `priority`,
`accentColor`, and `defaultExpanded`. Delta-V's `maxPoints` takes precedence
over `maxTraitPoints` when both are specified. Null or negative limits are unlimited.
Global `traits.max_count` and `traits.max_points` default to -1 (unlimited), leaving
QB's category budgets in control.

## Upstream follow-ups

- [#5285](https://github.com/DeltaV-Station/Delta-v/pull/5285): disabled-trait
  popup, OR conditions, component tooltips, description wrapping, and lock styling.
- [#5598](https://github.com/DeltaV-Station/Delta-v/pull/5598): application priority.
  QB already defaults to zero cost and keeps its existing category budgets instead
  of adopting Delta-V's global limit of 25.
- [#6082](https://github.com/DeltaV-Station/Delta-v/pull/6082) and
  [#6097](https://github.com/DeltaV-Station/Delta-v/pull/6097): dependency conditions,
  selection availability feedback, and server-side selected-trait context.
- [#5275](https://github.com/DeltaV-Station/Delta-v/pull/5275): keep the points
  bar inside its border on wide displays.

These are adapted to QB's shared validator and legacy fields. Preview changes
do not erase selections; unavailable selected traits remain removable. No new
gameplay traits or upstream trait YAML migrations are included. Existing job
specials continue to provide status effects. The popup can be disabled with its
checkbox (`traits.skip_disabled_popup`).
