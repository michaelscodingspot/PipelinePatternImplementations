using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineImplementations.Part1.BlockingCollection
{
    public static class GenericBCPipelineAwaitExtensions
    {
        public static TOutput Step2<TInput, TOutput, TInputOuter, TOutputOuter>(this TInput inputType,
            GenericBCPipelineAwait<TInputOuter, TOutputOuter> pipelineBuilder,
            Func<TInput, TOutput> step)
        {
            var pipelineStep = pipelineBuilder.GenerateStep<TInput, TOutput>();
            pipelineStep.StepAction = step;
            return default(TOutput);
        }
    }

    public class GenericBCPipelineAwait<TPipeIn, TPipeOut>
    {
        public interface IPipelineAwaitStep<TStepIn>
        {
            BlockingCollection<Item<TStepIn>> Buffer { get; set; }
        }

        public class GenericBCPipelineAwaitStep<TStepIn, TStepOut> : IPipelineAwaitStep<TStepIn>
        {
            public BlockingCollection<Item<TStepIn>> Buffer { get; set; } = new BlockingCollection<Item<TStepIn>>();
            public Func<TStepIn, TStepOut> StepAction { get; set; }
        }

        public class Item<T>
        {
            public T Input { get; set; }
            public TaskCompletionSource<TPipeOut> TaskCompletionSource { get; set; }
        }
        
        List<object> _pipelineSteps = new List<object>();


        public GenericBCPipelineAwait(Func<TPipeIn, GenericBCPipelineAwait<TPipeIn, TPipeOut>, TPipeOut> steps)
        {
            steps.Invoke(default(TPipeIn), this);//Invoke just once to build blocking collections
        }

        public Task<TPipeOut> Execute(TPipeIn input)
        {
            var first = _pipelineSteps[0] as IPipelineAwaitStep<TPipeIn>;
            TaskCompletionSource<TPipeOut> tsk = new TaskCompletionSource<TPipeOut>();
            first.Buffer.Add(/*input*/new Item<TPipeIn>()
            {
                Input = input,
                TaskCompletionSource = tsk
            });
            return tsk.Task;
        }

        public GenericBCPipelineAwaitStep<TStepIn, TStepOut> GenerateStep<TStepIn, TStepOut>()
        {
            var pipelineStep = new GenericBCPipelineAwaitStep<TStepIn, TStepOut>();
            var stepIndex = _pipelineSteps.Count;

            Task.Run(() =>
            {
                IPipelineAwaitStep<TStepOut> nextPipelineStep = null;

                foreach (var input in pipelineStep.Buffer.GetConsumingEnumerable())
                {
                    bool isLastStep = stepIndex == _pipelineSteps.Count - 1;
                    TStepOut output;
                    try
                    {
                        output = pipelineStep.StepAction(input.Input);
                    }
                    catch (Exception e)
                    {
                        input.TaskCompletionSource.SetException(e);
                        continue;
                    }
                    if (isLastStep)
                    {
                        input.TaskCompletionSource.SetResult((TPipeOut)(object)output);
                    }
                    else
                    {
                        nextPipelineStep = nextPipelineStep ?? (isLastStep ? null : _pipelineSteps[stepIndex + 1] as IPipelineAwaitStep<TStepOut>);
                        nextPipelineStep.Buffer.Add(new Item<TStepOut>() { Input  = output, TaskCompletionSource = input.TaskCompletionSource });
                    }
                }
            });

            _pipelineSteps.Add(pipelineStep);
            return pipelineStep;

        }
    }
}
