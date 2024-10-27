using Mutagen.Bethesda.Skyrim;

namespace Synthesis.Util.Quest
{
    public class QuestForwarderPipeline(ISkyrimMod patchMod)
        : ForwardPatcherPipeline<ISkyrimMod, ISkyrimModGetter, IQuest, IQuestGetter>(patchMod)
    {
        protected override void Update(IQuestGetter quest) =>
            Console.WriteLine($"Patched QUST:{quest.FormKey}({quest.EditorID})");
    }

    public class QuestPatcherPipeline(ISkyrimMod patchMod)
        : ConditionalTransformPatcherPipeline<ISkyrimMod, ISkyrimModGetter, IQuest, IQuestGetter>(
            patchMod
        )
    {
        protected override void Update(IQuestGetter quest) =>
            Console.WriteLine($"Patched QUST:{quest.FormKey}({quest.EditorID})");
    }
}
