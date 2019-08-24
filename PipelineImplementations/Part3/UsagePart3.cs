using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipelineImplementations.Part3.Disruptor;
using PipelineImplementations.Part3.TPLDataflowWithAsync;

namespace PipelineImplementations.Part3
{
    public class UsagePart3
    {
        public static void Use()
        {
            //DisruptorExample();
            DisruptorSimple();
            //DisruptorAwaitable();
        }

        private static async Task DisruptorAwaitable()
        {
            var pipeline = new DisruptorSimpleAwaitable<bool>()
                .AddStep<string, string>(FindMostCommon)
                .AddStep<string, int>(CountChars)
                .AddStep<int, bool>(IsOdd)
                .CreatePipeline();

            Console.WriteLine(await pipeline.Execute("The pipeline patter is the best pattern"));
        }

        private static void DisruptorSimple()
        {
            var pipeline = new DisruptorSimple()
                .AddStep<string, string>(FindMostCommon)
                .AddStep<string, int>(CountChars)
                .AddStep<int, bool>(IsOdd)
                // This last step is kind of a result callback. We'll solve it better in a minute
                .AddStep<bool, bool>((res) =>
                {
                    Console.WriteLine(res);
                    return res;
                })
                .CreatePipeline();
            pipeline.Execute("The pipeline pattern is the best pattern");
        }
        
        private static void Simple()
        {
            var pipeline = new TPLDataflowSteppedSimple<string, bool>();
            pipeline.AddStep<string, string>(input => FindMostCommon(input));
            pipeline.AddStep<string, int>(input => CountChars(input));
            pipeline.AddStep<int, bool>(input => IsOdd(input));
            pipeline.CreatePipeline(resultCallback: res => Console.WriteLine(res));

            pipeline.Execute("The pipeline patter is the best patter");


        }

        private static void SimpleAsyncFinal2()
        {
            var pipeline = new TPLDataflowSteppedAsyncFinal2<string, bool>();

            Func<string, Task<string>> findMostCommonAsync = async (str) => await FindMostCommonAsync(str);
            pipeline.AddStepAsync<string, string>(async input => FindMostCommon(input));

            //pipeline.AddStepAsync<string,string>(async (str) => await FindMostCommonAsync(str));
            pipeline.AddStep<string, int>(input => CountChars(input));
            pipeline.AddStepAsync<int, bool>(async input => IsOdd(input));


            pipeline.CreatePipeline(res => Console.WriteLine(res));

            pipeline.Execute("The pipeline patter is the best patter");

        }

        private static async Task<string> SomeAsync(string input)
        {
            await Task.Delay(100);
            return "asdf";
        }

        private static string FindMostCommon(string input)
        {
            return input.Split(' ')
                .GroupBy(word => word)
                .OrderBy(group => group.Count())
                .Last()
                .Key;
        }

        private static async Task<string> FindMostCommonAsync(string input)
        {
            await Task.Delay(10);
            return input.Split(' ')
                .GroupBy(word => word)
                .OrderBy(group => group.Count())
                .Last()
                .Key;
        }

        private static int CountChars(string mostCommon)
        {
            return mostCommon.Length;
        }

        private static bool IsOdd(int number)
        {
            var res = number % 2 == 1;
            //Console.WriteLine(res.ToString());
            return res;
        }


    }
}
