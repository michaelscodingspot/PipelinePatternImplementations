using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PipelineImplementations.Part2.TPLDataflow
{
    public class TPLPipelineWithAwaitFinal<TIn, TOut>
    {
        private List<IDataflowBlock> _steps = new List<IDataflowBlock>();
        public void AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc)
        {
            var step = new TransformBlock<TC<TLocalIn, TOut>, TC<TLocalOut, TOut>>((tc) => 
                new TC<TLocalOut,TOut> (stepFunc(tc.Input), tc.TaskCompletionSource));
            if (_steps.Count > 0)
            {
                var lastStep = _steps.Last();
                var targetBlock = (lastStep as ISourceBlock<TC<TLocalIn, TOut>>);
                targetBlock.LinkTo(step, new DataflowLinkOptions());
            }
            _steps.Add(step);
        }

        public void CreatePipeline()
        {
            var setResultStep =
                new ActionBlock<TC<TOut, TOut>>((tc) => tc.TaskCompletionSource.SetResult(tc.Input));
            var lastStep = _steps.Last();
            var targetBlock = (lastStep as ISourceBlock<TC<TOut, TOut>>);
            targetBlock.LinkTo(setResultStep);
        }

        public Task<TOut> Execute(TIn input)
        {
            var firstStep = _steps[0] as ITargetBlock<TC<TIn, TOut>>;
            var tcs = new TaskCompletionSource<TOut>();
            firstStep.SendAsync(new TC<TIn, TOut>(input, tcs));
            return tcs.Task;
        }
    }
}