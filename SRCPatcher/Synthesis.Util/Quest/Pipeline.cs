using Mutagen.Bethesda.Skyrim;

namespace Synthesis.Util.Quest
{
    public class QuestForwarderPipeline(ISkyrimMod patchMod)
        : ForwardPatcherPipeline<ISkyrimMod, ISkyrimModGetter>(patchMod);

    public class QuestPatcherPipeline(ISkyrimMod patchMod)
        : ConditionalTransformPatcherPipeline<ISkyrimMod, ISkyrimModGetter>(patchMod);
}
