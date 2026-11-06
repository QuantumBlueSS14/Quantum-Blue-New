using System.Linq;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Shared._QB.Traits;

public static class TraitSelection
{
    public static bool Conflicts(TraitPrototype first, TraitPrototype second)
    {
        return first.Conflicts.Contains(second.ID) || second.Conflicts.Contains(first.ID) ||
               first.Category == second.Category && first.Subcategories.Overlaps(second.Subcategories);
    }

    public static List<ProtoId<TraitPrototype>> Validate(
        IEnumerable<ProtoId<TraitPrototype>> selected,
        IPrototypeManager prototypes,
        int maxCount = -1,
        int maxPoints = -1,
        Func<TraitPrototype, IReadOnlySet<ProtoId<TraitPrototype>>, bool>? isEligible = null)
    {
        var remaining = selected.ToHashSet();
        while (true)
        {
            var eligible = remaining.Where(id => prototypes.TryIndex(id, out var trait) &&
                trait.Conditions.All(condition => condition.EvaluateSelection(remaining) != false) &&
                (isEligible == null || isEligible(trait, remaining)));
            var valid = ValidateLimits(eligible, prototypes, maxCount, maxPoints);
            if (remaining.SetEquals(valid))
                return valid;

            // Recheck dependencies and budgets after rejected traits stop contributing.
            remaining = valid.ToHashSet();
        }
    }

    private static List<ProtoId<TraitPrototype>> ValidateLimits(
        IEnumerable<ProtoId<TraitPrototype>> selected,
        IPrototypeManager prototypes,
        int maxCount,
        int maxPoints)
    {
        var traits = new List<TraitPrototype>();
        foreach (var id in selected.Distinct())
        {
            if (prototypes.TryIndex(id, out var trait))
                traits.Add(trait);
        }

        // Take point-granting traits first. HashSet iteration order must not decide
        // whether an otherwise valid selection survives saving or spawning.
        traits.Sort((first, second) =>
        {
            var cost = first.Cost.CompareTo(second.Cost);
            return cost != 0 ? cost : string.Compare(first.ID, second.ID, StringComparison.Ordinal);
        });

        var accepted = new List<TraitPrototype>();
        var points = 0;
        var categoryCounts = new Dictionary<string, int>();
        var categoryPoints = new Dictionary<string, int>();
        foreach (var trait in traits)
        {
            TraitCategoryPrototype? category = null;
            if (trait.Category is { } categoryId && !prototypes.TryIndex(categoryId, out category))
                continue;

            if (maxCount >= 0 && accepted.Count >= maxCount ||
                maxPoints >= 0 && points + trait.Cost > maxPoints ||
                accepted.Any(other => Conflicts(trait, other)))
                continue;

            if (category != null)
            {
                var count = categoryCounts.GetValueOrDefault(category.ID);
                var spent = categoryPoints.GetValueOrDefault(category.ID);
                if (category.MaxTraits is >= 0 && count >= category.MaxTraits ||
                    category.PointLimit is >= 0 && spent + trait.Cost > category.PointLimit)
                    continue;

                categoryCounts[category.ID] = count + 1;
                categoryPoints[category.ID] = spent + trait.Cost;
            }

            accepted.Add(trait);
            points += trait.Cost;
        }

        return accepted.Select(t => new ProtoId<TraitPrototype>(t.ID)).ToList();
    }
}
