using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineImplementations.Part1.BlockingCollection
{
    public interface IPipelineStep<TStepIn>
    {
        BlockingCollection<TStepIn> Buffer { get; set; }
    }

    public class GenericBCPipelineStep<TStepIn, TStepOut> : IPipelineStep<TStepIn>
    {
        public BlockingCollection<TStepIn> Buffer { get; set; } = new BlockingCollection<TStepIn>();
        public Func<TStepIn, TStepOut> StepAction { get; set; }
    }

    public static class GenericBCPipelineExtensions
    {

        public static TOutput Step<TInput, TOutput, TInputOuter, TOutputOuter>(this TInput inputType,
            GenericBCPipeline<TInputOuter, TOutputOuter> pipelineBuilder,
            Func<TInput, TOutput> step)
        {
            var pipelineStep = pipelineBuilder.GenerateStep<TInput, TOutput>();
            pipelineStep.StepAction = step;
            return default(TOutput);
        }
    }

    public class GenericBCPipeline<TPipeIn, TPipeOut>
    {
        List<object> _pipelineSteps = new List<object>();

        public event Action<TPipeOut> Finished;

        public GenericBCPipeline(Func<TPipeIn, GenericBCPipeline<TPipeIn, TPipeOut>, TPipeOut> steps)
        {
            steps.Invoke(default(TPipeIn), this);//Invoke just once to build blocking collections
        }

        public void Execute(TPipeIn input)
        {
            var first = _pipelineSteps[0] as IPipelineStep<TPipeIn>;
            first.Buffer.Add(input);
        }

        public GenericBCPipelineStep<TStepIn, TStepOut> GenerateStep<TStepIn, TStepOut>()
        {
            var pipelineStep = new GenericBCPipelineStep<TStepIn, TStepOut>();
            var stepIndex = _pipelineSteps.Count;

            Task.Run(() =>
            {
                IPipelineStep<TStepOut> nextPipelineStep = null;

                foreach (var input in pipelineStep.Buffer.GetConsumingEnumerable())
                {
                    bool isLastStep = stepIndex == _pipelineSteps.Count - 1;
                    var output = pipelineStep.StepAction(input);
                    if (isLastStep)
                    {
                    // This is dangerous as the invocation is added to the last step
                    // Alternatively, you can utilize BeginInvoke like here: https://stackoverflow.com/a/16336361/1229063
                    Finished?.Invoke(output is TPipeOut v ? v : default(TPipeOut));
                    }
                    else
                    {
                        nextPipelineStep = nextPipelineStep ?? (isLastStep ? null : _pipelineSteps[stepIndex + 1] as IPipelineStep<TStepOut>);
                        nextPipelineStep.Buffer.Add(output);
                    }
                }
            });

            _pipelineSteps.Add(pipelineStep);
            return pipelineStep;

        }
    }
}
