using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineImplementations.Part1.BlockingCollection
{
    
public class CastingPipelineWithAwait<TOutput> : IAwaitablePipeline<TOutput>
{
    class Step
    {
        public Func<object, object> Func { get; set; }
        public int DegreeOfParallelism { get; set; }
        public int MaxCapacity { get; set; }
    }

    class Item
    {
        public object Input { get; set; }
        public TaskCompletionSource<TOutput> TaskCompletionSource { get; set; }
    }

    List<Step> _pipelineSteps = new List<Step>();
    BlockingCollection<Item>[] _buffers;

    public event Action<TOutput> Finished;

    public void AddStep(Func<object, object> stepFunc, int degreeOfParallelism, int maxCapacity)
    {
        _pipelineSteps.Add(new Step() {Func = stepFunc, DegreeOfParallelism = degreeOfParallelism, 
            MaxCapacity = maxCapacity, });
    }

    public Task<TOutput> Execute(object input)
    {
        var first = _buffers[0];
        var item = new Item()
        {
            Input = input,
            TaskCompletionSource = new TaskCompletionSource<TOutput>()
        };
        first.Add(item);
        return item.TaskCompletionSource.Task;
    }

    public IAwaitablePipeline<TOutput> GetPipeline()
    {
        _buffers = _pipelineSteps.Select(step => new BlockingCollection<Item>()).ToArray();

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

    private void StartStep(int bufferIndexLocal, Step pipelineStep)
    {
        foreach (var input in _buffers[bufferIndexLocal].GetConsumingEnumerable())
        {
            object output;
            try
            {
                output = pipelineStep.Func.Invoke(input.Input);
            }
            catch (Exception e)
            {
                input.TaskCompletionSource.SetException(e);
                continue;
            }

            bool isLastStep = bufferIndexLocal == _pipelineSteps.Count - 1;
            if (isLastStep)
            {
                input.TaskCompletionSource.SetResult((TOutput)(object)output);
            }
            else
            {
                var next = _buffers[bufferIndexLocal + 1];
                next.Add(new Item() { Input = output, TaskCompletionSource = input.TaskCompletionSource});
            }
        }
    }
}
}
