using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PipelineImplementations.Part2.TPLDataflow
{
    public class TPLPipelineWithAwaitAttempt2<TIn, TOut>
    {
        private List<IDataflowBlock> _transformBlocks = new List<IDataflowBlock>();
        public TPLPipelineWithAwaitAttempt2<TIn, TOut> AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc)
        {
            var step = new TransformBlock<TC<TLocalIn, TOut>, TC<TLocalOut, TOut>>((tc) =>
                {
                    try
                    {
                        return new TC<TLocalOut, TOut>(stepFunc(tc.Input), tc.TaskCompletionSource);
                    }
                    catch (Exception e)
                    {
                        tc.TaskCompletionSource.SetException(e);
                        return new TC<TLocalOut, TOut>(default(TLocalOut), tc.TaskCompletionSource);
                    }
                });
            
            if (_transformBlocks.Count > 0)
            {
                var lastStep = _transformBlocks.Last();
                var targetBlock = (lastStep as ISourceBlock<TC<TLocalIn, TOut>>);
                targetBlock.LinkTo(step, new DataflowLinkOptions(), 
                    tc => !tc.TaskCompletionSource.Task.IsFaulted);
                targetBlock.LinkTo(DataflowBlock.NullTarget<TC<TLocalIn, TOut>>(), new DataflowLinkOptions(), 
                    tc => tc.TaskCompletionSource.Task.IsFaulted);
            }
            _transformBlocks.Add(step);
            return this;
        }

        public TPLPipelineWithAwaitAttempt2<TIn, TOut> CreatePipeline()
        {
            var setResultStep =
                new ActionBlock<TC<TOut, TOut>>((tc) => tc.TaskCompletionSource.SetResult(tc.Input));
            var lastStep = _transformBlocks.Last();
            var setResultBlock = (lastStep as ISourceBlock<TC<TOut, TOut>>);
            setResultBlock.LinkTo(setResultStep);
            return this;
        }

        public Task<TOut> Execute(TIn input)
        {
            var firstStep = _transformBlocks[0] as ITargetBlock<TC<TIn, TOut>>;
            var tcs = new TaskCompletionSource<TOut>();
            firstStep.SendAsync(new TC<TIn, TOut>(input, tcs));
            return tcs.Task;
        }
    }
}