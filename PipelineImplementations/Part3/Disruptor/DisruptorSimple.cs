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
    
public class DisruptorSimple
{
    private class StepPayload
    {
        public object Value { get; set; }
    }

    private class DelegateHandler : IWorkHandler<StepPayload>
    {
        private readonly Func<object, object> _stepFunc;

        public DelegateHandler(Func<object, object> stepFunc)
        {
            _stepFunc = stepFunc;
        }

        public void OnEvent(StepPayload payload)
        {
            payload.Value = _stepFunc(payload.Value);
        }
    }


    private Disruptor<StepPayload> _disruptor;
    private List<DelegateHandler> _steps = new List<DelegateHandler>();
    public void AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc)
    {
        _steps.Add(new DelegateHandler((obj) => stepFunc((TLocalIn)obj)));
    }

    public void CreatePipeline()
    {
        _disruptor = new Disruptor<StepPayload>(() => new StepPayload(), 1024, TaskScheduler.Default/*, ProducerType.Multi, new BlockingSpinWaitWaitStrategy()*/);
        var handlerGroup = _disruptor.HandleEventsWithWorkerPool(_steps.First());
        for (int i = 1; i < _steps.Count; i++)
        {
            var step = _steps[i];
            var makeStepToArray = new IWorkHandler<StepPayload>[] {step};
            handlerGroup = handlerGroup.HandleEventsWithWorkerPool(makeStepToArray);
        }
        
        _disruptor.Start();
    }


    public void Execute(string data)
    {
        var sequence = _disruptor.RingBuffer.Next();
        var disruptorEvent = _disruptor[sequence];
        disruptorEvent.Value = data;
        _disruptor.RingBuffer.Publish(sequence);

    }
}
}
