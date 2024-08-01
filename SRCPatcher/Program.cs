using CommandLine;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Synthesis.Utils.Quests;

namespace SRCPatcher
{
    public class Program
    {
        private static readonly ModKey SRC_ERS = ModKey.FromNameAndExtension("SRC_ERS.esp");

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
            var srcErsEsp = state.LoadOrder.GetIfEnabled(SRC_ERS);
            if (srcErsEsp.Mod is null)
            {
                return;
            }

            var affectedQuests = srcErsEsp.Mod.Quests;
            var srcFormList = srcErsEsp
                .Mod.FormLists.Where(formList => formList.EditorID is not null)
                .Single(formList => formList.EditorID!.Equals("SRC_ERSList"));

            var condition = QuestAliasConditionUtil.FindAliasCondition(
                affectedQuests.First(),
                condition =>
                    condition.Data.Function == Condition.Function.GetInCurrentLocFormList
                    && condition
                        .Data.Cast<IGetInCurrentLocFormListConditionDataGetter>()
                        .FormList.Link.Equals(srcFormList.ToLinkGetter())
            );
            if (condition is null)
            {
                Console.WriteLine($"Unable to find SRC ERS condition in quest aliases, aborting");
                return;
            }
            var patcher = new QuestAliasConditionUtil(condition);
            patcher.PatchAll(affectedQuests, state);
        }
    }
}
