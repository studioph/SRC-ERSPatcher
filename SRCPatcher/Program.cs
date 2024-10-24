using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Synthesis.Util;
using Synthesis.Util.Quest;

namespace SRCPatcher
{
    public class Program
    {
        private static readonly ModKey SRC_ERS = ModKey.FromNameAndExtension("SRC_ERS.esp");

        private static readonly IFormLinkGetter<IFormListGetter> ERS_FormList = FormKey
            .Factory("000800:SRC_ERS.esp")
            .ToLinkGetter<IFormListGetter>();

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline
                .Instance.AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SRCPatcher.esp")
                .AddRunnabilityCheck(state =>
                {
                    state.LoadOrder.AssertListsMod(SRC_ERS, $"Missing {SRC_ERS}");
                })
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var ersMod = state.LoadOrder.GetIfEnabledAndExists(SRC_ERS);
            var ersFormList = ERS_FormList.Resolve(state.LinkCache).ToLinkGetter();

            var affectedQuests = ersMod.Quests;

            var condition =
                QuestAliasUtil.FindAliasCondition(
                    affectedQuests,
                    condition =>
                        condition.Data.Function == Condition.Function.GetInCurrentLocFormList
                        && (
                            (IGetInCurrentLocFormListConditionDataGetter)condition.Data
                        ).FormList.Link.Equals(ersFormList)
                ) ?? throw new Exception("Unable to find ERS condition in quest aliases, aborting");

            var pipeline = new QuestPatcherPipeline(state.PatchMod);
            var patcher = new QuestAliasConditionPatcher(condition);

            var forwardContexts = affectedQuests.Select(quest =>
                quest.WithContext<ISkyrimMod, ISkyrimModGetter, IQuest, IQuestGetter>(
                    state.LinkCache
                )
            );

            var patchData = pipeline.GetRecordsToPatch(patcher, forwardContexts);
            pipeline.PatchRecords(patcher, patchData);
        }
    }
}
