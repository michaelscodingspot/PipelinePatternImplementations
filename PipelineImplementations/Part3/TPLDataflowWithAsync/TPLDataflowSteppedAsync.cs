using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using PipelineImplementations.Part2.TPLDataflow;

namespace PipelineImplementations.Part3.TPLDataflowWithAsync
{
    public class TPLDataflowSteppedAsync<TIn, TOut>
    {
        private List<IDataflowBlock> _steps = new List<IDataflowBlock>();
        
        public void AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc)
        {
            if (_steps.Count == 0)
            {
                var step = new TransformBlock<TLocalIn, TLocalOut>((input) =>
                    stepFunc(input));
                _steps.Add(step);
            }
            else
            {

                var lastStep = _steps.Last();
                var targetBlock = (lastStep as ISourceBlock<TLocalIn>);
                if (targetBlock != null)
                {
                    var step = new TransformBlock<TLocalIn, TLocalOut>((input) =>
                        stepFunc(input));
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(step);
                }
                else
                {
                    var step = new TransformBlock<Task<TLocalIn>, TLocalOut>((input) => 
                        stepFunc(input.Result));
                    var targetBlock1 = (lastStep as ISourceBlock<Task<TLocalIn>>);
                    targetBlock1.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(step);
                    //var step = new TransformBlock<Task<TLocalIn>, TLocalOut>((input) =>
                    //    stepFunc(input.Result));
                    //var targetBlock1 = lastStep as ISourceBlock<Task<TLocalIn>>;
                    //targetBlock1.LinkTo(step, new DataflowLinkOptions());
                    //_steps.Add(step);

                }
            }

        }

        public void AddStepAsync<TLocalIn, TLocalOut>(Func<TLocalIn, Task<TLocalOut>> stepFunc)
        {
            if (_steps.Count == 0)
            {
                var step = new TransformBlock<TLocalIn, Task<TLocalOut>>(async (input) => await stepFunc(input));
                _steps.Add(step);
            }
            else 
            {
                var lastStep = _steps.Last();
                var targetBlock = (lastStep as ISourceBlock<Task<TLocalIn>>);
                if (targetBlock != null)
                {
                    var step = new TransformBlock<Task<TLocalIn>, Task<TLocalOut>>(async (input) =>
                        await stepFunc(input.Result));

                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(step);
                }
                else
                {
                    var targetBlock1 = (lastStep as ISourceBlock<TLocalIn>);
                    var step = new TransformBlock<TLocalIn, Task<TLocalOut>>(async (input) =>
                        await stepFunc(input));

                    targetBlock1.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(step);
                }
            }
            
        }

        public void CreatePipeline(Action<TOut> resultCallback)
        {
            var lastStep = _steps.Last();
            var targetBlock = (lastStep as ISourceBlock<Task<TOut>>);
            if (targetBlock != null)
            {
                var callBackStep = new ActionBlock<Task<TOut>>(t => resultCallback(t.Result)); 
                targetBlock.LinkTo(callBackStep);
            }
            else
            {
                var callBackStep1 = new ActionBlock<TOut>(t => resultCallback(t)); 
                var targetBlock1 = (lastStep as ISourceBlock<TOut>);
                targetBlock1.LinkTo(callBackStep1);
            }
        }

        public void Execute(TIn input)
        {
            var firstStep = _steps[0] as ITargetBlock<TIn>;
            firstStep.SendAsync(input);
        }
    }
}
