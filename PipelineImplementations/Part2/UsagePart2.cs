using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using PipelineImplementations.Part2.TPLDataflow;

namespace PipelineImplementations.Part2
{
    public class UsagePart2
    {
        public static void Use()
        {
            Console.WriteLine(Utils.GetThreadPoolThreadsInUse());
            //UseSimple();
            UseSimpleAsync();
            //UseTCS();
            //UseBuilder();
        }

        private static void UseBuilder()
        {
            var pipeline = new TPLPipelineWithAwaitAttempt2<string, bool>();
            pipeline.AddStep<string, string>(sentence => FindMostCommon(sentence));
            pipeline.AddStep<string, int>(word => word.Length);
            pipeline.AddStep<int, bool>(length => length % 2 == 1);
            pipeline.CreatePipeline();

            System.Threading.Tasks.Task.Run(async () =>
            {
                bool res;
                try
                {
                    res = await pipeline.Execute("The pipeline pattern is the best pattern");
                    Console.WriteLine(res);
                }
                catch (Exception e)
                {
                }

                try
                {
                    res = await pipeline.Execute("The pipeline pattern is the best pattern");
                    Console.WriteLine(res);
                }
                catch (Exception e)
                {
                }

                try
                {
                    res = await pipeline.Execute("abcd");
                    Console.WriteLine(res);
                }
                catch (Exception e)
                {
                }

                try
                {
                    res = await pipeline.Execute("abcd");
                    Console.WriteLine(res);
                }
                catch (Exception e)
                {
                }

                res = await pipeline.Execute("The pipeline pattern is the best pattern");
                Console.WriteLine(res);
                res = await pipeline.Execute("The pipeline pattern is the best pattern");
                Console.WriteLine(res);
                res = await pipeline.Execute("The pipeline pattern is the best pattern");
                Console.WriteLine(res);


            }).Wait();
        }

        private static void UseSimple()
        {
            var pipeline = TPLDataflowPipelineSimple.CreatePipeline(resultCallback: res =>
             {
                 Console.WriteLine(res);
                 Console.WriteLine(Utils.GetThreadPoolThreadsInUse());
             });
            for (int i = 0; i < 50; i++)
            {
                pipeline.Post("The pipeline pattern is the best pattern");
                
            }
        }

        private static void UseSimpleAsync()
        {
            var pipeline = TPLDataflowPipelineSimple.CreatePipeline(resultCallback: res =>
            {
                Console.WriteLine(res);
                Console.WriteLine(Utils.GetThreadPoolThreadsInUse());
            });

            Task.Run(async () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    await pipeline.SendAsync("The pipeline pattern is the best pattern");
                }
            });
        }

        private static void UseTCS()
        {
            var pipeline = TPLDataflowPipelineWithAwaitAttempt1.CreatePipeline();

            var tsk = System.Threading.Tasks.Task.Run(async () =>
            {
var tcs = new TaskCompletionSource<bool>();
var tc = new TC<string, bool>(
    "The pipeline patter is the best patter", tcs);
var task = tcs.Task;
await pipeline.SendAsync(tc);
var result = await task;
Console.WriteLine(result);
                //await Task.Delay(1000);
            });
            tsk.Wait(CancellationToken.None);
        }

        private static string FindMostCommon(string input)
        {
            if (input == "abcd")
                throw new Exception();

            return input.Split(' ')
                .GroupBy(word => word)
                .OrderBy(group => group.Count())
                .Last()
                .Key;
        }
    }
}
