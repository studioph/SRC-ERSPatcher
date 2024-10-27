using System.Collections.Immutable;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Exceptions;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Synthesis.Util.Quest;

namespace SRCPatcher
{
    public static class PluginLoader<TMod, TModGetter>
        where TMod : class, IMod, TModGetter
        where TModGetter : class, IModGetter
    {
        private static readonly IDictionary<
            PluginData,
            Func<IPatcherState<TMod, TModGetter>, TModGetter, PatcherPluginBase<TMod, TModGetter>>
        > _registry =
            new Dictionary<
                PluginData,
                Func<
                    IPatcherState<TMod, TModGetter>,
                    TModGetter,
                    PatcherPluginBase<TMod, TModGetter>
                >
            >();

        public static ImmutableArray<PluginData> RegisteredPlugins
        {
            get => [.. _registry.Keys];
        }

        public static void Register<TPlugin>(
            Func<IPatcherState<TMod, TModGetter>, TModGetter, TPlugin> factory
        )
            where TPlugin : PatcherPluginBase<TMod, TModGetter>, IPatcherPlugin =>
            _registry.Add(TPlugin.Data, factory);

        public static ImmutableArray<PatcherPluginBase<TMod, TModGetter>> Scan(
            IPatcherState<TMod, TModGetter> state
        )
        {
            IList<PatcherPluginBase<TMod, TModGetter>> loaded = [];

            foreach (var (pluginData, factory) in _registry)
            {
                if (state.LoadOrder.TryGetIfEnabledAndExists(pluginData.ModKey, out var found))
                {
                    loaded.Add(factory(state, found));
                }
                else if (pluginData.Required)
                {
                    throw new MissingModException(pluginData.ModKey);
                }
            }

            Console.WriteLine(
                $"Detected and loaded the following plugins: [{string.Join(",", loaded)}]"
            );

            return [.. loaded];
        }
    }

    public record PluginData(ModKey ModKey, bool Required = false);

    public interface IPatcherPlugin
    {
        static abstract PluginData Data { get; }
    }

    public abstract class PatcherPluginBase<TMod, TModGetter>(
        TModGetter mod,
        IPatcherState<TMod, TModGetter> state
    )
        where TMod : class, IMod, TModGetter
        where TModGetter : class, IModGetter
    {
        protected readonly IPatcherState<TMod, TModGetter> _state = state;

        protected readonly TModGetter _mod = mod;

        public abstract void Run();
    }

    internal sealed record ERSPluginData(ModKey ModKey, Func<IQuestAliasGetter, bool> Evaluator)
        : PluginData(ModKey);

    internal abstract class ERSPlugin(
        ISkyrimModGetter mod,
        IPatcherState<ISkyrimMod, ISkyrimModGetter> state,
        Func<IQuestAliasGetter, bool> aliasFilter,
        QuestPatcherPipeline pipeline
    ) : PatcherPluginBase<ISkyrimMod, ISkyrimModGetter>(mod, state)
    {
        private readonly QuestPatcherPipeline _pipeline = pipeline;

        private readonly QuestAliasConditionAdder _patcher = new(SRC_ERS.Condition, aliasFilter);

        public override void Run()
        {
            var newQuests = _mod
                .Quests.Select(quest =>
                    quest
                        .ToLinkGetter()
                        .ResolveContext<ISkyrimMod, ISkyrimModGetter, IQuest, IQuestGetter>(
                            _state.LinkCache
                        )
                )
                .NotNull();

            _pipeline.Run(_patcher, newQuests);
        }
    }

    internal sealed class MissivesPlugin(
        ISkyrimModGetter mod,
        IPatcherState<ISkyrimMod, ISkyrimModGetter> state,
        QuestPatcherPipeline pipeline
    ) : ERSPlugin(mod, state, ShouldHaveCondition, pipeline), IPatcherPlugin
    {
        private static readonly ModKey Missives = ModKey.FromNameAndExtension("Missives.esp");

        private static bool ShouldHaveCondition(IQuestAliasGetter alias) =>
            alias.Type == QuestAlias.TypeEnum.Location
            && alias.Conditions.Any()
            && (!alias.Name?.Equals("OtherHold") ?? true);

        public static PluginData Data => new(Missives);
    }

    internal sealed class NoticeBoardPlugin(
        ISkyrimModGetter mod,
        IPatcherState<ISkyrimMod, ISkyrimModGetter> state,
        QuestPatcherPipeline pipeline
    ) : ERSPlugin(mod, state, ShouldHaveCondition, pipeline), IPatcherPlugin
    {
        private static readonly ModKey NoticeBoard = ModKey.FromNameAndExtension(
            "notice board.esp"
        );

        private static readonly IFormLinkGetter<IFormListGetter> _avoidLocations = FormKey
            .Factory($"02ACAB:{NoticeBoard}")
            .ToLinkGetter<IFormListGetter>();

        private static bool HasAvoidFormList(IConditionGetter cond) =>
            cond.Data.Function == Condition.Function.GetInCurrentLocFormList
            && ((IGetInCurrentLocFormListConditionDataGetter)cond.Data).FormList.Link.Equals(
                _avoidLocations
            );

        private static bool ShouldHaveCondition(IQuestAliasGetter alias) =>
            alias.Type == QuestAlias.TypeEnum.Location && alias.HasCondition(HasAvoidFormList);

        public static PluginData Data => new(NoticeBoard);
    }

    internal sealed class BountyHunterPlugin(
        ISkyrimModGetter mod,
        IPatcherState<ISkyrimMod, ISkyrimModGetter> state,
        QuestPatcherPipeline pipeline
    ) : ERSPlugin(mod, state, ShouldHaveCondition, pipeline), IPatcherPlugin
    {
        private static readonly ModKey BountyHunter = ModKey.FromNameAndExtension(
            "BountyHunter.esp"
        );

        private static bool ShouldHaveCondition(IQuestAliasGetter alias) =>
            alias.Type == QuestAlias.TypeEnum.Location && alias.Conditions.Any();

        public static PluginData Data => new(BountyHunter);
    }
}
