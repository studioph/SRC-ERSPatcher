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
        /// <summary>
        /// Patcher pipeline for plugin instances. Lazy so that it doesn't get created if no plugins are loaded
        /// </summary>
        private static Lazy<QuestPatcherPipeline> _pluginPipeline = null!;

        /// <summary>
        /// Loader instance to register plugins with
        /// </summary>
        private static readonly PluginLoader<ISkyrimMod, ISkyrimModGetter> _loader = new();

        /// <summary>
        /// Registers the optional ERS patcher plugins with the loader
        /// </summary>
        static Program()
        {
            _loader.Register<MissivesPlugin>(mod => new MissivesPlugin(mod, _pluginPipeline.Value));
            _loader.Register<NoticeBoardPlugin>(mod => new NoticeBoardPlugin(
                mod,
                _pluginPipeline.Value
            ));
            _loader.Register<BountyHunterPlugin>(mod => new BountyHunterPlugin(
                mod,
                _pluginPipeline.Value
            ));
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
            // Add factory for lazy pipeline creation
            // Can't be done before here since it relies on patch mod
            _pluginPipeline = new Lazy<QuestPatcherPipeline>(
                () => new QuestPatcherPipeline(state.PatchMod)
            );

            // Load ERS mod and any optional plugins in user's load order
            var ersMod = state.LoadOrder.GetIfEnabledAndExists(SRC_ERS.ModKey);
            var loadedPlugins = _loader.Scan(state.LoadOrder);
            var patcher = new QuestAliasConditionForwarder(SRC_ERS.Condition);
            var pipeline = new QuestForwarderPipeline(state.PatchMod);

            // Patch base ERS mod
            var forwardContexts = ersMod.Quests.Select(quest =>
                quest.WithContext<ISkyrimMod, ISkyrimModGetter, IQuest, IQuestGetter>(
                    state.LinkCache
                )
            );
            pipeline.Run(patcher, forwardContexts);

            // Run any optional patcher plugins based on user's load order
            foreach (var plugin in loadedPlugins)
            {
                plugin.Run(state);
            }
        }
    }

    /// <summary>
    /// Contains random information about the ERS mod that can be defined ahead of time
    /// </summary>
    public static class SRC_ERS
    {
        public static readonly ModKey ModKey = ModKey.FromNameAndExtension("SRC_ERS.esp");

        /// <summary>
        /// The SRC formlist that locations get added to when cleared.
        /// </summary>
        public static readonly IFormLinkGetter<IFormListGetter> FormList = FormKey
            .Factory($"000800:{ModKey}")
            .ToLinkGetter<IFormListGetter>();

        /// <summary>
        /// The ERS formlist condition for Location quest aliases. This is what ensures cleared locations are excluded from quests.
        /// </summary>
        public static readonly IConditionGetter Condition = BuildCondition();

        /// <summary>
        /// Creates a GetInCurrentLocFormList condition object referencing the SRC formlist.
        /// The condition isn't complex so it can be statically created up-front to avoid searching for it in the ERS mod at runtime.
        /// </summary>
        /// <returns>A condition object that references the SRC formlist</returns>
        private static IConditionGetter BuildCondition()
        {
            IConditionFloat condition = new ConditionFloat();
            IGetInCurrentLocFormListConditionData data = new GetInCurrentLocFormListConditionData();
            data.FormList.Link.SetTo(FormList);
            condition.Data = (ConditionData)data;
            return condition;
        }
    }
}
