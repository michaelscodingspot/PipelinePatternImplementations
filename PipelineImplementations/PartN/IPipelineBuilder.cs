using System;

namespace PipelineImplementations.PartN
{
    public interface IPipelineBuilder
    {
        IPipelineBuilderStep<TStepIn, TStepOut> Build<TStepIn, TStepOut>(Func<TStepIn, TStepOut> stepFunc, int workerCount);
    }
}