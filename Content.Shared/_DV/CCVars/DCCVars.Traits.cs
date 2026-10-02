using Robust.Shared.Configuration;

namespace Content.Shared._DV.CCVars;

public sealed partial class DCCVars
{
    public static readonly CVarDef<bool> SkipDisabledTraitsPopup =
        CVarDef.Create("traits.skip_disabled_popup", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    // Disabled by default to preserve QB's existing category budgets.
    public static readonly CVarDef<int> MaxTraitCount =
        CVarDef.Create("traits.max_count", -1, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> MaxTraitPoints =
        CVarDef.Create("traits.max_points", -1, CVar.SERVER | CVar.REPLICATED);
}
