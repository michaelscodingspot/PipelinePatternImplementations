using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using Disruptor.Dsl;

namespace PipelineImplementations.Part3.Disruptor
{

    public interface IPipeline<TOut>
    {
        Task<TOut> Execute(string data);
    }

public class DisruptorSimpleAwaitable<TOut> : IPipeline<TOut>
{
    public class StepPayload<TOut>
    {
        public object Value { get; set; }
        public TaskCompletionSource<TOut> TaskCompletionSource { get; set; }
    }

    public class DelegateHandler<TOut> : IWorkHandler<StepPayload<TOut>>
    {
        private readonly Func<object, object> _stepFunc;

        public DelegateHandler(Func<object, object> stepFunc)
        {
            _stepFunc = stepFunc;
        }

        public void OnEvent(StepPayload<TOut> payload)
        {
            try
            {
                if (payload.TaskCompletionSource.Task.IsFaulted)
                    return;
                payload.Value = _stepFunc(payload.Value);
            }
            catch (Exception e)
            {
                payload.TaskCompletionSource.SetException(e);
            }
        }
    }

    class SetResultHandler<TOut> :IWorkHandler<StepPayload<TOut>>
    {
        public void OnEvent(StepPayload<TOut> payload)
        {
            if (payload.TaskCompletionSource.Task.IsFaulted)
                return;
            payload.TaskCompletionSource.SetResult((TOut)payload.Value);
        }
    }

    private Disruptor<StepPayload<TOut>> _disruptor;
    private List<DelegateHandler<TOut>> _steps = new List<DelegateHandler<TOut>>();
    public DisruptorSimpleAwaitable<TOut> AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc)
    {
        _steps.Add(new DelegateHandler<TOut>((obj) => stepFunc((TLocalIn)obj)));
        return this;
    }

    public IPipeline<TOut> CreatePipeline()
    {
        _disruptor = new Disruptor<StepPayload<TOut>>(() => new StepPayload<TOut>(), 1024, TaskScheduler.Default/*, ProducerType.Multi, new BlockingSpinWaitWaitStrategy()*/);
        var handlerGroup = _disruptor.HandleEventsWithWorkerPool(_steps.First());
        for (int i = 1; i < _steps.Count; i++)
        {
            var step = _steps[i];
            var makeStepToArray = new IWorkHandler<StepPayload<TOut>>[] {step};
            handlerGroup = handlerGroup.HandleEventsWithWorkerPool(makeStepToArray);
        }
        var setResultHandler = new SetResultHandler<TOut>();
        var setResultHandlerToArray = new IWorkHandler<StepPayload<TOut>>[] {setResultHandler};
        handlerGroup.HandleEventsWithWorkerPool(setResultHandlerToArray);
        
        _disruptor.Start();
        return this;
    }


    public Task<TOut> Execute(string data)
    {
        var sequence = _disruptor.RingBuffer.Next();
        var disruptorEvent = _disruptor[sequence];
        disruptorEvent.Value = data;
        var tcs = new TaskCompletionSource<TOut>();
        disruptorEvent.TaskCompletionSource = tcs;

        _disruptor.RingBuffer.Publish(sequence);
        return tcs.Task;

    }
}
}
