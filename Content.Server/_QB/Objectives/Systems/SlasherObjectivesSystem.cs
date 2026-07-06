using Content.Server.Objectives.Components;
using Content.Server.QB.GameTicking.Rules;
using Content.Shared.Objectives.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Tracks progress for fixed Slasher objectives:
/// meathooks placed, effigy placed, and soul fragments fed.
/// </summary>
public sealed class SlasherObjectivesSystem : EntitySystem
{
    [Dependency] private readonly SlasherRuleSystem _rule = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherMeathookConditionComponent, ObjectiveGetProgressEvent>(OnMeathookProgress);
        SubscribeLocalEvent<SlasherEffigyConditionComponent, ObjectiveGetProgressEvent>(OnEffigyProgress);
        SubscribeLocalEvent<SlasherFeedEffigyConditionComponent, ObjectiveGetProgressEvent>(OnFeedProgress);
        SubscribeLocalEvent<SlasherMeathookConditionComponent, ObjectiveAfterAssignEvent>(OnMeathookAfterAssign);
        SubscribeLocalEvent<SlasherEffigyConditionComponent, ObjectiveAfterAssignEvent>(OnEffigyAfterAssign);
        SubscribeLocalEvent<SlasherFeedEffigyConditionComponent, ObjectiveAfterAssignEvent>(OnFeedAfterAssign);
        SubscribeLocalEvent<SlasherDoNotKillFlavorConditionComponent, ObjectiveAfterAssignEvent>(OnDoNotKillAfterAssign);
    }

    private void OnMeathookAfterAssign(EntityUid uid, SlasherMeathookConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, Loc.GetString("slasher-objective-condition-place-meathooks-title"), args.Meta);
        _metaData.SetEntityDescription(uid, Loc.GetString("slasher-objective-condition-place-meathooks-description"), args.Meta);
    }

    private void OnEffigyAfterAssign(EntityUid uid, SlasherEffigyConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, Loc.GetString("slasher-objective-condition-place-effigy-title"), args.Meta);
        _metaData.SetEntityDescription(uid, Loc.GetString("slasher-objective-condition-place-effigy-description"), args.Meta);
    }

    private void OnFeedAfterAssign(EntityUid uid, SlasherFeedEffigyConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, Loc.GetString("slasher-objective-condition-feed-fragments-title"), args.Meta);
        _metaData.SetEntityDescription(uid, Loc.GetString("slasher-objective-condition-feed-fragments-description"), args.Meta);
    }

    private void OnDoNotKillAfterAssign(EntityUid uid, SlasherDoNotKillFlavorConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, Loc.GetString("slasher-objective-condition-do-not-kill-title"), args.Meta);
        _metaData.SetEntityDescription(uid, Loc.GetString("slasher-objective-condition-do-not-kill-description"), args.Meta);
    }

    private void OnMeathookProgress(EntityUid uid, SlasherMeathookConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_rule.TryGetActiveRule(out var rule))
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = comp.Required <= 0
            ? 1f
            : MathF.Min(rule.Comp.MeathookCount / (float)comp.Required, 1f);
    }

    private void OnEffigyProgress(EntityUid uid, SlasherEffigyConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_rule.TryGetActiveRule(out var rule))
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = rule.Comp.EffigyPlacedEver ? 1f : 0f;
    }

    private void OnFeedProgress(EntityUid uid, SlasherFeedEffigyConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_rule.TryGetActiveRule(out var rule))
        {
            args.Progress = 0f;
            return;
        }

        var target = rule.Comp.TargetInsertions;
        args.Progress = target <= 0
            ? 1f
            : MathF.Min(rule.Comp.FragmentInsertions / (float)target, 1f);
    }
}