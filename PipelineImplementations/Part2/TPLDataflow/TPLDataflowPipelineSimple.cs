using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PipelineImplementations.Part2.TPLDataflow
{
    public class TPLDataflowPipelineSimple
    {
        //private ActionBlock<string> step1;
        public static TransformBlock<string, string> CreatePipeline(Action<bool> resultCallback)
        {
            var step1 = new TransformBlock<string, string>((sentence) => FindMostCommon(sentence));
            var step2 = new TransformBlock<string, int>((word) => word.Length);
            var step3 = new TransformBlock<int, bool>((length) =>
            {
                Console.WriteLine(Utils.GetThreadPoolThreadsInUse());
                return length % 2 == 1;
            });
            var callBackStep = new ActionBlock<bool>(resultCallback);
            step1.LinkTo(step2, new DataflowLinkOptions());
            step2.LinkTo(step3, new DataflowLinkOptions());
            step3.LinkTo(callBackStep);
            return step1;
        }

        private static string FindMostCommon(string input)
        {
            return input.Split(' ')
                .GroupBy(word => word)
                .OrderBy(group => group.Count())
                .Last()
                .Key;
        }
    }
}
