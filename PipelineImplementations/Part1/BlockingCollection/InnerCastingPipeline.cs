using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineImplementations.Part1.BlockingCollection
{
    
    public class InnerPipelineBuilder : IPipeline
    {
        List<Func<object, object>> _pipelineSteps = new List<Func<object, object>>();
        BlockingCollection<object>[] _buffers;

        public event Action<object> Finished;

public void AddStep<TStepIn, TStepOut>(Func<TStepIn, TStepOut> stepFunc)
{
    _pipelineSteps.Add(objInput => stepFunc.Invoke((TStepIn)(object)objInput));
}

        public void Execute(object input)
        {
            var first = _buffers[0];
            first.Add(input);
        }

        public IPipeline GetPipeline()
        {
            _buffers = _pipelineSteps.Select(step => new BlockingCollection<object>()).ToArray();

            int bufferIndex = 0;
            foreach (var pipelineStep in _pipelineSteps)
            {
                var bufferIndexLocal = bufferIndex;
                Task.Run(() =>
                {
                    foreach (var input in _buffers[bufferIndexLocal].GetConsumingEnumerable())
                    {
                        var output = pipelineStep.Invoke(input);

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
                });
                bufferIndex++;
            }
            return this;
        }
    }
}
