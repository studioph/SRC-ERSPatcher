using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Synthesis.Util;
using Synthesis.Util.Quest;

namespace SRCPatcher
{
    public class Program
    {
        private static Lazy<QuestPatcherPipeline> _pluginPipeline = null!;

        static Program()
        {
            PluginLoader<ISkyrimMod, ISkyrimModGetter>.Register(
                (state, mod) => new MissivesPlugin(mod, state, _pluginPipeline.Value)
            );
            PluginLoader<ISkyrimMod, ISkyrimModGetter>.Register(
                (state, mod) => new NoticeBoardPlugin(mod, state, _pluginPipeline.Value)
            );
            PluginLoader<ISkyrimMod, ISkyrimModGetter>.Register(
                (state, mod) => new BountyHunterPlugin(mod, state, _pluginPipeline.Value)
            );
        }

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline
                .Instance.AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SRCPatcher.esp")
                .AddRunnabilityCheck(state =>
                {
                    state.LoadOrder.AssertListsMod(SRC_ERS.ModKey, $"Missing {SRC_ERS.ModKey}");
                })
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            _pluginPipeline = new Lazy<QuestPatcherPipeline>(
                () => new QuestPatcherPipeline(state.PatchMod)
            );

            var ersMod = state.LoadOrder.GetIfEnabledAndExists(SRC_ERS.ModKey);
            var loadedPlugins = PluginLoader<ISkyrimMod, ISkyrimModGetter>.Scan(state);
            var patcher = new QuestAliasConditionForwarder(SRC_ERS.Condition);
            var pipeline = new QuestForwarderPipeline(state.PatchMod);

            var forwardContexts = ersMod.Quests.Select(quest =>
                quest.WithContext<ISkyrimMod, ISkyrimModGetter, IQuest, IQuestGetter>(
                    state.LinkCache
                )
            );
            pipeline.Run(patcher, forwardContexts);

            foreach (var plugin in loadedPlugins)
            {
                plugin.Run();
            }
        }
    }

    public static class SRC_ERS
    {
        public static readonly ModKey ModKey = ModKey.FromNameAndExtension("SRC_ERS.esp");

        public static readonly IFormLinkGetter<IFormListGetter> FormList = FormKey
            .Factory($"000800:{ModKey}")
            .ToLinkGetter<IFormListGetter>();

        public static readonly IConditionGetter Condition = BuildCondition(FormList);

        private static IConditionGetter BuildCondition(IFormLinkGetter<IFormListGetter> formLink)
        {
            IConditionFloat condition = new ConditionFloat();
            IGetInCurrentLocFormListConditionData data = new GetInCurrentLocFormListConditionData();
            data.FormList.Link.SetTo(formLink);
            condition.Data = (ConditionData)data;
            return condition;
        }
    }
}
