using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disruptor;
using Disruptor.Dsl;

namespace PipelineImplementations.PartN.Impl
{
    public class DisruptorPipelineBuilder : IPipelineBuilder
    {
        public IPipelineBuilderStep<TStepIn, TStepOut> Build<TStepIn, TStepOut>(Func<TStepIn, TStepOut> stepFunc, int workerCount)
        {
            var state = new State<TStepIn>();

            return state.AddStep(stepFunc, workerCount);
        }

        private interface IStep : IWorkHandler<DisruptorEvent>, IEventHandler<DisruptorEvent>
        {
            bool IsWorkerPool { get; }

            IWorkHandler<DisruptorEvent>[] AsWorkerPool();
        }

        private class Step<TIn, TStepIn, TStepOut> : IPipelineBuilderStep<TIn, TStepOut>, IStep
        {
            private readonly Func<TStepIn, TStepOut> _stepFunc;
            private readonly int _threadCount;

            public Step(Func<TStepIn, TStepOut> stepFunc, int threadCount)
            {
                _stepFunc = stepFunc;
                _threadCount = threadCount;
            }

            public State<TIn> State { get; set; }

            public bool IsWorkerPool => _threadCount != 1;

            public IWorkHandler<DisruptorEvent>[] AsWorkerPool()
                => Enumerable.Repeat(this, _threadCount).Cast<IWorkHandler<DisruptorEvent>>().ToArray();

            public IPipelineBuilderStep<TIn, TNewStepOut> AddStep<TNewStepOut>(Func<TStepOut, TNewStepOut> stepFunc, int workerCount)
            {
                return State.AddStep(stepFunc, workerCount);
            }

            public IPipeline<TIn, TStepOut> Create()
            {
                return State.Create<TStepOut>();
            }

            public void OnEvent(DisruptorEvent evt)
            {
                evt.Write(_stepFunc.Invoke(evt.Read<TStepIn>()));
            }

            public void OnEvent(DisruptorEvent data, long sequence, bool endOfBatch)
            {
                OnEvent(data);
            }
        }

        private class State<TIn>
        {
            private readonly List<IStep> _steps = new List<IStep>();

            public IPipelineBuilderStep<TIn, TStepOut> AddStep<TStepIn, TStepOut>(Func<TStepIn, TStepOut> stepFunc, int workerCount)
            {
                var step = new Step<TIn, TStepIn, TStepOut>(stepFunc, workerCount);

                _steps.Add(step);
                step.State = this;

                return step;
            }

            public IPipeline<TIn, TOut> Create<TOut>()
            {
                var disruptor = new Disruptor<DisruptorEvent>(() => new DisruptorEvent(), 1024, TaskScheduler.Default, ProducerType.Multi, new BlockingSpinWaitWaitStrategy());

                var firstStep = _steps.First();
                var currentGroup = firstStep.IsWorkerPool
                    ? disruptor.HandleEventsWithWorkerPool(firstStep.AsWorkerPool())
                    : disruptor.HandleEventsWith(firstStep);

                foreach (var step in _steps.Skip(1))
                {
                    currentGroup = step.IsWorkerPool
                        ? currentGroup.HandleEventsWithWorkerPool(step.AsWorkerPool())
                        : currentGroup.HandleEventsWith(step);
                }

                currentGroup.HandleEventsWith(new Releaser<TOut>());

                return new DisruptorPipeline<TIn, TOut>(disruptor);
            }
        }

        private class Releaser<TOut> : IEventHandler<DisruptorEvent>
        {
            public void OnEvent(DisruptorEvent data, long sequence, bool endOfBatch)
            {
                var tcs = (TaskCompletionSource<TOut>)data.TaskCompletionSource;
                tcs.SetResult(data.Read<TOut>());
            }
        }
    }
}
