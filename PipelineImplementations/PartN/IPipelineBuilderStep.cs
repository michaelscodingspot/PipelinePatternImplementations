using System;

namespace PipelineImplementations.PartN
{
    public interface IPipelineBuilderStep<TIn, TOut>
    {
        IPipelineBuilderStep<TIn, TNewStepOut> AddStep<TNewStepOut>(Func<TOut, TNewStepOut> stepFunc, int workerCount);
        IPipeline<TIn, TOut> Create();
    }
}