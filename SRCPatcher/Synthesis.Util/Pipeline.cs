using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;

namespace Synthesis.Util
{
    public abstract class PatcherPipeline<TMod, TModGetter, TMajor, TMajorGetter>(TMod patchMod)
        where TMod : IMod, TModGetter
        where TModGetter : IModGetter
        where TMajor : TMajorGetter
        where TMajorGetter : IMajorRecordQueryableGetter
    {
        protected readonly TMod _patchMod = patchMod;
        public uint PatchedCount { get; protected set; } = 0;

        /// <summary>
        /// Patches a record based on the provided patching data and patcher instance
        /// </summary>
        /// <typeparam name="TValue">The values used for patching</typeparam>
        /// <param name="item">The record and patching data</param>
        /// <param name="patcher">The patcher instance to use</param>
        public void PatchRecord<TValue>(
            PatchingData<TMod, TModGetter, TMajor, TMajorGetter, TValue> item,
            IPatcher<TMajor, TMajorGetter, TValue> patcher
        )
            where TValue : notnull
        {
            var target = item.Context.GetOrAddAsOverride(_patchMod);
            patcher.Patch(target, item.Values);
            PatchedCount++;
            Update(target);
        }

        public void PatchRecords<TValue>(
            IPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<PatchingData<TMod, TModGetter, TMajor, TMajorGetter, TValue>> recordsToPatch
        )
            where TValue : notnull
        {
            foreach (var item in recordsToPatch)
            {
                PatchRecord(item, patcher);
            }
        }

        /// <summary>
        /// Callback when a record is patched
        /// </summary>
        /// <param name="updated"></param>
        protected virtual void Update(TMajorGetter updated) { }
    }

    /// <summary>
    /// A pipeline for applying multiple forwarding-style patchers to multiple collections of records
    /// </summary>
    /// <typeparam name="TMajor"></typeparam>
    /// <typeparam name="TMajorGetter"></typeparam>
    public abstract class ForwardPatcherPipeline<TMod, TModGetter, TMajor, TMajorGetter>(
        TMod patchMod
    ) : PatcherPipeline<TMod, TModGetter, TMajor, TMajorGetter>(patchMod)
        where TMod : IMod, TModGetter
        where TModGetter : IModGetter
        where TMajor : TMajorGetter
        where TMajorGetter : IMajorRecordQueryableGetter
    {
        /// <summary>
        /// Processes records and outputs ones that should be patched along with the new values
        /// </summary>
        /// <param name="patcher">The patcher instance to use</param>
        /// <param name="records"></param>
        /// <returns>DTO objects containing the winning records and values needed to patch</returns>
        public IEnumerable<
            PatchingData<TMod, TModGetter, TMajor, TMajorGetter, TValue>
        > GetRecordsToPatch<TValue>(
            IForwardPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<ForwardRecordContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TValue : notnull =>
            records
                .Select(item =>
                    item.Winning.WithPatchingData(patcher.Analyze(item.Source, item.Winning.Record))
                )
                .Where(result => patcher.ShouldPatch(result.Values));

        public void Run<TValue>(
            IForwardPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<ForwardRecordContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TValue : notnull => PatchRecords(patcher, GetRecordsToPatch(patcher, records));
    }

    /// <summary>
    /// A pipeline for applying multiple transform-style patchers to multiple collections of records
    /// </summary>
    /// <typeparam name="TMod"></typeparam>
    /// <typeparam name="TModGetter"></typeparam>
    /// <typeparam name="TMajor"></typeparam>
    /// <typeparam name="TMajorGetter"></typeparam>
    /// <param name="patchMod"></param>
    public abstract class TransformPatcherPipeline<TMod, TModGetter, TMajor, TMajorGetter>(
        TMod patchMod
    ) : PatcherPipeline<TMod, TModGetter, TMajor, TMajorGetter>(patchMod)
        where TMod : IMod, TModGetter
        where TModGetter : IModGetter
        where TMajor : TMajorGetter
        where TMajorGetter : IMajorRecordQueryableGetter
    {
        public IEnumerable<
            PatchingData<TMod, TModGetter, TMajor, TMajorGetter, TValue>
        > GetRecordsToPatch<TValue>(
            ITransformPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<IModContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TValue : notnull =>
            records
                .Where(context => patcher.Filter(context.Record))
                .Select(context => context.WithPatchingData(patcher.Apply(context.Record)));

        public void Run<TValue>(
            ITransformPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<IModContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TValue : notnull => PatchRecords(patcher, GetRecordsToPatch(patcher, records));
    }

    /// <summary>
    /// A pipeline for applying multiple conditional transform-style patchers to multiple collections of records
    /// </summary>
    /// <typeparam name="TMod"></typeparam>
    /// <typeparam name="TModGetter"></typeparam>
    /// <typeparam name="TMajor"></typeparam>
    /// <typeparam name="TMajorGetter"></typeparam>
    /// <param name="patchMod"></param>
    public abstract class ConditionalTransformPatcherPipeline<
        TMod,
        TModGetter,
        TMajor,
        TMajorGetter
    >(TMod patchMod) : TransformPatcherPipeline<TMod, TModGetter, TMajor, TMajorGetter>(patchMod)
        where TMod : IMod, TModGetter
        where TModGetter : IModGetter
        where TMajor : TMajorGetter
        where TMajorGetter : IMajorRecordQueryableGetter
    {
        public IEnumerable<
            PatchingData<TMod, TModGetter, TMajor, TMajorGetter, TValue>
        > GetRecordsToPatch<TValue>(
            IConditionalTransformPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<IModContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TValue : notnull =>
            base.GetRecordsToPatch(patcher, records)
                .Where(result => patcher.ShouldPatch(result.Values));

        public void Run<TValue>(
            IConditionalTransformPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<IModContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TValue : notnull => PatchRecords(patcher, GetRecordsToPatch(patcher, records));
    }
}
