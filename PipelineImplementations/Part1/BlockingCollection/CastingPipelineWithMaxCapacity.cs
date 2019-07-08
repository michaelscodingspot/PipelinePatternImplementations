using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineImplementations.Part1.BlockingCollection
{
    
    public class CastingPipelineWithMaxCapacity : IPipeline
    {
class StepInfo
{
    public Func<object, object> Func { get; set; }
    public int DegreeOfParallelism { get; set; }
    public int MaxCapacity { get; set; }
}

        List<StepInfo> _pipelineSteps = new List<StepInfo>();
        BlockingCollection<object>[] _buffers;

        public event Action<object> Finished;

public void AddStep(Func<object, object> stepFunc, int degreeOfParallelism, int maxCapacity)
{
    _pipelineSteps.Add(new StepInfo() {Func = stepFunc, DegreeOfParallelism = degreeOfParallelism, MaxCapacity = maxCapacity});
}

        public void Execute(object input)
        {
            var first = _buffers[0];
            first.Add(input);
        }

public IPipeline GetPipeline()
{
    _buffers = _pipelineSteps.Select(step => new BlockingCollection<object>(step.MaxCapacity)).ToArray();

            int bufferIndex = 0;
            foreach (var pipelineStep in _pipelineSteps)
            {
                var bufferIndexLocal = bufferIndex;

                for (int i = 0; i < pipelineStep.DegreeOfParallelism; i++)
                {
                    Task.Run(() => { StartStep(bufferIndexLocal, pipelineStep); });
                }

                bufferIndex++;
            }
            return this;
        }

        private void StartStep(int bufferIndexLocal, StepInfo pipelineStep)
        {
            foreach (var input in _buffers[bufferIndexLocal].GetConsumingEnumerable())
            {
                var output = pipelineStep.Func.Invoke(input);
                bool isLastStep = bufferIndexLocal == _pipelineSteps.Count - 1;
                if (isLastStep)
                {
                    // This is dangerous as the invocation is added to the last step
                    // Alternatively, you can utilize 'BeginInvoke' like here: https://stackoverflow.com/a/16336361/1229063
                    Finished?.Invoke(output);
                }
                else
                {
                    var next = _buffers[bufferIndexLocal + 1];
                    next.Add(output);
                }
            }
        }
    }
}
