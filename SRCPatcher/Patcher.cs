using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Synthesis.Util;
using Synthesis.Util.Quest;

namespace SRCPatcher
{
    public class QuestAliasConditionAdder(
        IConditionGetter condition,
        Func<IQuestAliasGetter, bool> aliasFilter
    )
        : QuestAliasConditionPatcher(condition),
            IConditionalTransformPatcher<IQuest, IQuestGetter, IEnumerable<uint>>
    {
        private static readonly ModKey Skyrim = ModKey.FromNameAndExtension("Skyrim.esm");

        private readonly Func<IQuestAliasGetter, bool> _aliasFilter = aliasFilter;

        public IEnumerable<uint> Apply(IQuestGetter quest) =>
            quest
                .Aliases.Where(_aliasFilter)
                .Where(alias => !alias.HasCondition(Condition))
                .Select(alias => alias.ID);

        // Can't really narrow down quests without performing the exact logic, so may as well not waste time doing it twice
        public bool Filter(IQuestGetter quest) => quest.FormKey.ModKey != Skyrim; // Base game quests are handled by forwarding patcher

        public bool ShouldPatch(IEnumerable<uint> aliasIDs) => aliasIDs.Any();
    }
}
