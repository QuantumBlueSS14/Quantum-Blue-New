using System.Linq;
using System.Collections.Generic;
using Content.Client._DV.Traits.UI;
using Content.Server.Body.Systems;
using Content.Server.Traits;
using Content.Server._Impstation.TraitRandomizer;
using Content.Shared.Mind;
using Content.Shared.Traits.Assorted;
using Content.Shared._QB.Traits;
using Content.Shared.Preferences;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests._QB;

[TestFixture, TestOf(typeof(TraitSystem))]
public sealed class TraitsTest
{
    [TestPrototypes]
    private const string Prototypes = """
        - type: traitCategory
          id: QBTraitTest
          name: trait-category-traits
          maxTraitPoints: 0

        - type: trait
          id: QBTraitBenefit
          name: trait-pacifist-name
          category: QBTraitTest
          cost: 2
          effects:
          - !type:AddCompsEffect
            components:
            - type: Pacified

        - type: trait
          id: QBTraitDrawback
          name: trait-pacifist-name
          category: QBTraitTest
          cost: -2
          excludedSpecies:
          - Diona
          conditions:
          - !type:HasJobCondition
            job: MedicalDoctor

        - type: trait
          id: QBTraitConflict
          name: trait-pacifist-name
          conflicts:
          - QBTraitBenefit

        - type: trait
          id: QBTraitEffects
          name: trait-pacifist-name
          conditions:
          - !type:IsSpeciesCondition
            species: Human
          - !type:InDepartmentCondition
            department: Medical
          - !type:HasCompCondition
            component: Pacified
            invert: true
          effects:
          - !type:AddCompsEffect
            components:
            - type: Pacified
          - !type:RemCompsEffect
            components:
            - Pacified
          - !type:OverrideCompsEffect
            components:
            - type: Pacified
          - !type:SpawnItemInHandEffect
            item: Pen

        - type: traitCategory
          id: QBTraitRandomizerTest
          name: trait-category-traits

        - type: trait
          id: QBTraitRandomizerItem
          name: trait-pacifist-name
          category: QBTraitRandomizerTest
          effects:
          - !type:SpawnItemInHandEffect
            item: Pen

        - type: trait
          id: QBTraitRequired
          name: trait-pacifist-name
          conditions:
          - !type:HasJobCondition
            job: MedicalDoctor

        - type: trait
          id: QBTraitDependent
          name: trait-pacifist-name
          conditions:
          - !type:TraitDependencyCondition
            requires: [QBTraitRequired]
          effects:
          - !type:AddCompsEffect
            components:
            - type: Pacified

        - type: trait
          id: QBTraitDependentDrawback
          name: trait-pacifist-name
          cost: -2
          category: QBTraitTest
          conditions:
          - !type:TraitDependencyCondition
            requires: [QBTraitRequired]

        - type: trait
          id: QBTraitPriorityAdd
          name: trait-pacifist-name
          cost: 1
          priority: 10
          effects:
          - !type:AddCompsEffect
            components:
            - type: Pacified

        - type: trait
          id: QBTraitPriorityRemove
          name: trait-pacifist-name
          effects:
          - !type:RemCompsEffect
            components: [Pacified]

        - type: trait
          id: QBTraitUnknownOr
          name: trait-pacifist-name
          conditions:
          - !type:AnyOfCondition
            invert: true
            conditions:
            - !type:HasCompCondition
              component: Pacified
            - !type:IsSpeciesCondition
              species: Diona

        - type: traitCategory
          id: QBTraitCapped
          name: trait-category-traits
          maxTraits: 1
          maxTraitPoints: 0
          maxPoints: 1

        - type: trait
          id: QBTraitCapFree
          name: trait-pacifist-name
          category: QBTraitCapped

        - type: trait
          id: QBTraitCapPaid
          name: trait-pacifist-name
          category: QBTraitCapped
          cost: 1
        """;

    [Test]
    public async Task SelectionSurvivesReorderingAndProfileReload()
    {
        await using var pair = await PoolManager.GetServerClient();
        await pair.Server.WaitAssertion(() =>
        {
            var prototypes = pair.Server.ResolveDependency<IPrototypeManager>();
            ProtoId<TraitPrototype>[] selected = ["QBTraitBenefit", "QBTraitDrawback"];
            Assert.That(TraitSelection.Validate(selected, prototypes), Is.EquivalentTo(selected));
            Assert.That(TraitSelection.Validate(selected.Reverse(), prototypes), Is.EquivalentTo(selected));

            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human")
                .WithTraitPreferences(selected, prototypes);
            var serializer = pair.Server.ResolveDependency<ISerializationManager>();
            var saved = serializer.WriteValue(profile);
            var loaded = serializer.Read<HumanoidCharacterProfile>(saved);
            Assert.That(loaded.GetValidTraits(loaded.TraitPreferences, prototypes), Is.EquivalentTo(selected));
            Assert.That(TraitSelection.Validate(["QBTraitBenefit"], prototypes), Is.Empty);

            Assert.That(TraitSelection.Validate(selected, prototypes, maxCount: 1),
                Is.EquivalentTo(new[] { new ProtoId<TraitPrototype>("QBTraitDrawback") }));
            Assert.That(TraitSelection.Validate(selected, prototypes, maxPoints: 0), Is.EquivalentTo(selected));
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConflictsAndLegacySubcategories()
    {
        await using var pair = await PoolManager.GetServerClient();
        await pair.Server.WaitAssertion(() =>
        {
            var prototypes = pair.Server.ResolveDependency<IPrototypeManager>();
            ProtoId<TraitPrototype> benefitId = "QBTraitBenefit";
            ProtoId<TraitPrototype> conflictId = "QBTraitConflict";
            var benefit = prototypes.Index(benefitId);
            var conflict = prototypes.Index(conflictId);
            Assert.That(TraitSelection.Conflicts(benefit, conflict), Is.True);
            Assert.That(TraitSelection.Conflicts(conflict, benefit), Is.True);

            ProtoId<TraitPrototype>[] vision = ["Blindness", "PoorVision"];
            Assert.That(TraitSelection.Validate(vision, prototypes), Has.Count.EqualTo(1));
            Assert.That(TraitSelection.Validate(vision.Reverse(), prototypes),
                Is.EqualTo(TraitSelection.Validate(vision, prototypes)));
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task IneligibleDrawbackCannotFundOtherTraits()
    {
        await using var pair = await PoolManager.GetServerClient();
        await pair.Server.WaitAssertion(() =>
        {
            var entities = pair.Server.ResolveDependency<IEntityManager>();
            var prototypes = pair.Server.ResolveDependency<IPrototypeManager>();
            var traits = entities.System<TraitSystem>();
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human")
                .WithTraitPreferences(["QBTraitBenefit", "QBTraitDrawback"], prototypes);
            var doctor = entities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var assistant = entities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);

            traits.ApplyTraits(doctor, profile, "MedicalDoctor");
            traits.ApplyTraits(assistant, profile, "Passenger");
            var pacified = pair.Server.ResolveDependency<IComponentFactory>().GetRegistration("Pacified").Type;
            Assert.That(entities.HasComponent(doctor, pacified), Is.True);
            Assert.That(entities.HasComponent(assistant, pacified), Is.False);

            entities.DeleteEntity(doctor);
            entities.DeleteEntity(assistant);
        });
        await pair.CleanReturnAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task EffectsAndLegacyTraitsApplyTogether(bool randomized)
    {
        await using var pair = await PoolManager.GetServerClient();
        await pair.Server.WaitAssertion(() =>
        {
            var entities = pair.Server.ResolveDependency<IEntityManager>();
            var prototypes = pair.Server.ResolveDependency<IPrototypeManager>();
            var mob = entities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human")
                .WithTraitPreferences(["Unborgable", "Hemorrhage", "QBTraitEffects"], prototypes);

            var traits = entities.System<TraitSystem>();
            if (randomized)
            {
                foreach (var id in profile.TraitPreferences)
                    Assert.That(traits.TryApplyTrait(mob, prototypes.Index(id), "MedicalDoctor", null), Is.True);
            }
            else
            {
                traits.ApplyTraits(mob, profile, "MedicalDoctor");
            }

            Assert.That(entities.System<BodySystem>().GetBodyOrgans(mob)
                .Any(organ => entities.HasComponent<UnborgableComponent>(organ.Id)), Is.True);
            Assert.That(entities.HasComponent<UnborgableComponent>(mob), Is.False);
            Assert.That(entities.System<StatusEffectsSystem>()
                .TryGetStatusEffect(mob, "StatusEffectHemorrhageTrait", out _), Is.True);

            var pacified = pair.Server.ResolveDependency<IComponentFactory>().GetRegistration("Pacified").Type;
            Assert.That(entities.HasComponent(mob, pacified), Is.True);
            var hands = entities.System<Content.Shared.Hands.EntitySystems.SharedHandsSystem>();
            Assert.That(hands.EnumerateHeld(mob).Any(item =>
                entities.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID == "Pen"), Is.True);

            entities.DeleteEntity(mob);
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task EditorRefreshKeepsSelectionsWithoutChangingProfile()
    {
        await using var pair = await PoolManager.GetServerClient();
        await pair.Client.WaitAssertion(() =>
        {
            var prototypes = pair.Client.ResolveDependency<IPrototypeManager>();
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human")
                .WithTraitPreferences(["QBTraitBenefit", "QBTraitDrawback"], prototypes);
            var tab = new TraitsTab();
            var changes = 0;
            tab.OnTraitsChanged += _ => changes++;
            tab.SetProfile(profile, null);
            tab.RefreshTraits();
            Assert.That(Descendants(tab).OfType<TraitCategory>().SelectMany(c => c.GetSelectedTraitIds()),
                Is.EquivalentTo(profile.TraitPreferences));

            tab.SetProfile(profile, "Passenger");
            Assert.That(Descendants(tab).OfType<TraitCategory>().SelectMany(c => c.GetSelectedTraitIds()),
                Is.EquivalentTo(profile.TraitPreferences));
            Assert.That(changes, Is.Zero);

            ProtoId<TraitPrototype> effectsId = "QBTraitEffects";
            var entry = new TraitEntry(prototypes.Index(effectsId));
            entry.UpdateConditionsMet(null, "Human");
            Assert.That(entry.MeetsConditions, Is.True, "Server-only inverted component conditions must not disable the editor.");
            entry.SetSelected(true);
            entry.UpdateConditionsMet("Passenger", "Human");
            Assert.That(entry.MeetsConditions, Is.False);
            Assert.That(entry.IsSelected, Is.True, "Previewing another job must not erase a saved selection.");
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TooltipClearsPreviousFailedConditions()
    {
        await using var pair = await PoolManager.GetServerClient();
        await pair.Client.WaitAssertion(() =>
        {
            var prototypes = pair.Client.ResolveDependency<IPrototypeManager>();
            ProtoId<TraitPrototype> drawbackId = "QBTraitDrawback";
            using var entry = new TraitEntry(prototypes.Index(drawbackId));
            entry.UpdateConditionsMet("Passenger", "Human");
            using var failed = (Tooltip) entry.TooltipSupplier!(entry)!;
            Assert.That(failed.Text, Does.Contain("Requirements not met"));

            entry.UpdateConditionsMet("MedicalDoctor", "Diona");
            Assert.That(entry.MeetsConditions, Is.False);
            using var updated = (Tooltip) entry.TooltipSupplier!(entry)!;
            Assert.That(updated.Text, Does.Contain("unavailable for your species"));
            Assert.That(updated.Text, Does.Not.Contain("You must be a"));
        });
        await pair.CleanReturnAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RandomizerHandlesSmallAndEmptyPools(bool empty)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        await pair.Server.WaitAssertion(() =>
        {
            var entities = pair.Server.ResolveDependency<IEntityManager>();
            var session = pair.Server.ResolveDependency<ISharedPlayerManager>().Sessions.Single();
            var minds = entities.System<SharedMindSystem>();
            var mind = minds.CreateMind(session.UserId);
            var mob = entities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            minds.TransferTo(mind, mob);
            entities.AddComponent(mob, new TraitRandomizerComponent
            {
                MinTraits = 3,
                MaxTraits = 3,
                Categories = empty ? [] : ["QBTraitRandomizerTest", "QBTraitRandomizerTest"],
            });
            var hands = entities.System<Content.Shared.Hands.EntitySystems.SharedHandsSystem>();
            Assert.That(hands.EnumerateHeld(mob).Count(), Is.EqualTo(empty ? 0 : 1));
            minds.SetUserId(mind, null);
            entities.DeleteEntity(mob);
            entities.DeleteEntity(mind);
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DependenciesFollowAcceptedTraitsAtSpawn()
    {
        await using var pair = await PoolManager.GetServerClient();
        await pair.Server.WaitAssertion(() =>
        {
            var entities = pair.Server.ResolveDependency<IEntityManager>();
            var prototypes = pair.Server.ResolveDependency<IPrototypeManager>();
            var traits = entities.System<TraitSystem>();
            var pacified = pair.Server.ResolveDependency<IComponentFactory>().GetRegistration("Pacified").Type;
            ProtoId<TraitPrototype>[] selected = ["QBTraitDependent", "QBTraitRequired"];
            Assert.That(TraitSelection.Validate(selected, prototypes), Is.EquivalentTo(selected));
            Assert.That(TraitSelection.Validate(["QBTraitDependent"], prototypes), Is.Empty);

            foreach (var job in new[] { "MedicalDoctor", "Passenger" })
            {
                var mob = entities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
                var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human").WithTraitPreferences(selected, prototypes);
                traits.ApplyTraits(mob, profile, job);
                Assert.That(entities.HasComponent(mob, pacified), Is.EqualTo(job == "MedicalDoctor"));
                entities.DeleteEntity(mob);
            }

            var passenger = entities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var funded = HumanoidCharacterProfile.DefaultWithSpecies("Human")
                .WithTraitPreferences(["QBTraitRequired", "QBTraitDependentDrawback", "QBTraitBenefit"], prototypes);
            Assert.That(funded.TraitPreferences, Has.Count.EqualTo(3));
            traits.ApplyTraits(passenger, funded, "Passenger");
            Assert.That(entities.HasComponent(passenger, pacified), Is.False);
            entities.DeleteEntity(passenger);
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PriorityControlsEffectOrder()
    {
        await using var pair = await PoolManager.GetServerClient();
        await pair.Server.WaitAssertion(() =>
        {
            var entities = pair.Server.ResolveDependency<IEntityManager>();
            var prototypes = pair.Server.ResolveDependency<IPrototypeManager>();
            var mob = entities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human")
                .WithTraitPreferences(["QBTraitPriorityRemove", "QBTraitPriorityAdd"], prototypes);
            entities.System<TraitSystem>().ApplyTraits(mob, profile, "Passenger");
            var pacified = pair.Server.ResolveDependency<IComponentFactory>().GetRegistration("Pacified").Type;
            Assert.That(entities.HasComponent(mob, pacified), Is.False);
            entities.DeleteEntity(mob);
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MenuLocksUnavailableTraitsButAllowsRemoval()
    {
        await using var pair = await PoolManager.GetServerClient();
        await pair.Client.WaitAssertion(() =>
        {
            var prototypes = pair.Client.ResolveDependency<IPrototypeManager>();
            ProtoId<TraitPrototype> unknownId = "QBTraitUnknownOr";
            using var unknown = new TraitEntry(prototypes.Index(unknownId));
            unknown.UpdateConditionsMet(null, "Human");
            Assert.That(unknown.MeetsConditions, Is.True);
            unknown.UpdateConditionsMet(null, "Diona");
            Assert.That(unknown.MeetsConditions, Is.False);

            ProtoId<TraitPrototype> dependentId = "QBTraitDependent";
            using var dependent = new TraitEntry(prototypes.Index(dependentId));
            dependent.UpdateConditionsMet(null, "Human", new HashSet<ProtoId<TraitPrototype>>());
            Assert.That(dependent.MeetsConditions, Is.False);
            dependent.UpdateConditionsMet(null, "Human", new HashSet<ProtoId<TraitPrototype>> { "QBTraitRequired" });
            Assert.That(dependent.MeetsConditions, Is.True);

            ProtoId<TraitPrototype> freeId = "QBTraitCapFree";
            ProtoId<TraitPrototype> paidId = "QBTraitCapPaid";
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human").WithTraitPreferences([freeId], prototypes);
            using var tab = new TraitsTab();
            tab.SetProfile(profile, null);
            var category = Descendants(tab).OfType<TraitCategory>().Single(value => value.GetSelectedTraitIds().Contains(freeId));
            var paid = Descendants(category).OfType<TraitEntry>().Single(value => value.TraitCost == 1);
            var selected = Descendants(category).OfType<TraitEntry>().Single(value => value.IsSelected);
            Assert.That(Descendants(paid).OfType<CheckBox>().Single().Visible, Is.False);
            Assert.That(Descendants(selected).OfType<CheckBox>().Single().Disabled, Is.False);

            tab.SetProfile(HumanoidCharacterProfile.DefaultWithSpecies("Human"), null);
            Assert.That(Descendants(paid).OfType<CheckBox>().Single().Visible, Is.True);
            Assert.That(TraitSelection.Validate([paidId], prototypes), Is.EquivalentTo(new[] { paidId }),
                "maxPoints must override the legacy maxTraitPoints field.");

            using var popup = new DisabledTraitsPopup(new() { [dependentId] = ["Test reason"] });
            Assert.That(Descendants(popup).OfType<RichTextLabel>().Any(label => label.GetMessage()?.Contains("Test reason") == true), Is.True);
        });
        await pair.CleanReturnAsync();
    }

    private static IEnumerable<Control> Descendants(Control control)
    {
        foreach (var child in control.Children)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }
}
