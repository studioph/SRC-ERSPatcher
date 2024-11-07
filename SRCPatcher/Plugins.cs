using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Synthesis.Util;
using Synthesis.Util.Quest;

namespace SRCPatcher
{
    /// <summary>
    /// Base class for plugins for ERS, which dynamically add the ERS condition to quest aliases as needed.
    ///
    /// Each specific plugin contains its own rules to determine whether a quest alias (and by extension the quest) should be patched.
    /// </summary>
    /// <param name="mod">The mod to patch</param>
    /// <param name="aliasFilter">Function to determine whether a quest alias should have the ERS condition added to it</param>
    /// <param name="pipeline">Patcher pipeline to patch records</param>
    internal abstract class ERSPlugin(
        ISkyrimModGetter mod,
        Func<IQuestAliasGetter, bool> aliasFilter,
        ConditionalTransformPatcherPipeline<ISkyrimMod, ISkyrimModGetter> pipeline
    ) : IPatcherPlugin<ISkyrimMod, ISkyrimModGetter>
    {
        protected readonly ISkyrimModGetter _mod = mod;

        private readonly ConditionalTransformPatcherPipeline<
            ISkyrimMod,
            ISkyrimModGetter
        > _pipeline = pipeline;

        /// <summary>
        /// The patcher instance to use.
        /// </summary>
        private readonly QuestAliasConditionAdder _patcher = new(SRC_ERS.Condition, aliasFilter);

        public void Run(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var newQuests = _mod
                .Quests.Select(quest =>
                    quest
                        .ToLinkGetter()
                        .ResolveContext<ISkyrimMod, ISkyrimModGetter, IQuest, IQuestGetter>(
                            state.LinkCache
                        )
                )
                .NotNull();

            _pipeline.Run(_patcher, newQuests);
        }
    }

    /// <summary>
    /// ERS Plugin for Missives
    /// </summary>
    internal sealed class MissivesPlugin(
        ISkyrimModGetter mod,
        ConditionalTransformPatcherPipeline<ISkyrimMod, ISkyrimModGetter> pipeline
    ) : ERSPlugin(mod, ShouldHaveCondition, pipeline), IPluginData
    {
        private static readonly ModKey Missives = ModKey.FromNameAndExtension("Missives.esp");

        /// <summary>
        /// The following rules result in the same selection as the existing ERS patch:
        ///
        /// - Location alias type
        /// - Has at least 1 condition
        /// - The alias name is not "OtherHold" (it would probably be fine to leave this out - the extra patched aliases are unlikely to break anything, but aiming for parity here)
        /// </summary>
        /// <param name="alias">The quest alias to evaluate</param>
        /// <returns>True if the quest alias should have the ERS condition added to it</returns>
        private static bool ShouldHaveCondition(IQuestAliasGetter alias) =>
            alias.Type == QuestAlias.TypeEnum.Location
            && alias.Conditions.Any()
            && (!alias.Name?.Equals("OtherHold") ?? true);

        public static PluginData Data => new(nameof(MissivesPlugin), Missives);
    }

    /// <summary>
    /// ERS Plugin for Notice Board
    /// </summary>
    internal sealed class NoticeBoardPlugin(
        ISkyrimModGetter mod,
        ConditionalTransformPatcherPipeline<ISkyrimMod, ISkyrimModGetter> pipeline
    ) : ERSPlugin(mod, ShouldHaveCondition, pipeline), IPluginData
    {
        private static readonly ModKey NoticeBoard = ModKey.FromNameAndExtension(
            "notice board.esp"
        );

        /// <summary>
        /// Notice Board's own location blacklist. Used for alias evaluation.
        /// </summary>
        private static readonly IFormLinkGetter<IFormListGetter> _avoidLocations = FormKey
            .Factory($"02ACAB:{NoticeBoard}")
            .ToLinkGetter<IFormListGetter>();

        private static bool HasAvoidFormList(IConditionGetter cond) =>
            cond.Data.Function == Condition.Function.GetInCurrentLocFormList
            && ((IGetInCurrentLocFormListConditionDataGetter)cond.Data).FormList.Link.Equals(
                _avoidLocations
            );

        /// <summary>
        /// The following rules result in the same selection as the existing ERS patch:
        ///
        /// - Location alias type
        /// - Alias has the "avoid locations" formlist condition (this is a more complex check than the other mods, but other criteria proved to be unreliable)
        /// </summary>
        /// <param name="alias">The quest alias to evaluate</param>
        /// <returns>True if the quest alias should have the ERS condition added to it</returns>
        private static bool ShouldHaveCondition(IQuestAliasGetter alias) =>
            alias.Type == QuestAlias.TypeEnum.Location && alias.HasCondition(HasAvoidFormList);

        public static PluginData Data => new(nameof(NoticeBoardPlugin), NoticeBoard);
    }

    /// <summary>
    /// ERS Plugin for Bounty Hunter
    ///
    /// Note that the few base game quests that BH touches are already handled by the main forwarding patcher
    /// </summary>
    internal sealed class BountyHunterPlugin(
        ISkyrimModGetter mod,
        ConditionalTransformPatcherPipeline<ISkyrimMod, ISkyrimModGetter> pipeline
    ) : ERSPlugin(mod, ShouldHaveCondition, pipeline), IPluginData
    {
        private static readonly ModKey BountyHunter = ModKey.FromNameAndExtension(
            "BountyHunter.esp"
        );

        /// <summary>
        /// The following rules result in the same selection as the existing ERS patch:
        /// - Location alias type
        /// - Has at least 1 condition
        /// </summary>
        /// <param name="alias">The quest alias to evaluate</param>
        /// <returns>True if the quest alias should have the ERS condition added to it</returns>
        private static bool ShouldHaveCondition(IQuestAliasGetter alias) =>
            alias.Type == QuestAlias.TypeEnum.Location && alias.Conditions.Any();

        public static PluginData Data => new(nameof(BountyHunterPlugin), BountyHunter);
    }
}
