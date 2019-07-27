using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipelineImplementations.PartN.Impl;

namespace PipelineImplementations.PartN
{
    public static class UsagePartN
    {
        public static async Task Use1()
        {
            var builder = new DisruptorPipelineBuilder();

            var pipeline = builder.Build<string, string>(FindMostCommon, 2)
                .AddStep(x => x.Length, 2)
                .AddStep(x => x % 2 == 1, 2)
                .Create();

            Console.WriteLine(await pipeline.Execute("The pipeline pattern is the best pattern"));
            Console.WriteLine(await pipeline.Execute("The pipeline pattern is the best pattern"));
            Console.WriteLine(await pipeline.Execute("The pipeline pattern is the best pattern"));
            Console.WriteLine(await pipeline.Execute("The pipeline patter is the best patter"));
            Console.WriteLine(await pipeline.Execute("The pipeline pattern is the best pattern"));

            pipeline.Dispose();
        }

        public static async Task Use2()
        {
            var builder = new DisruptorPipelineBuilder();
            var results = new HashSet<string>();

            var pipeline = builder.Build<string, char>(x => x.Max(), 2)
                .AddStep(x => new string(x, 20), 2)
                .AddStep(x => $"[{x}]", 1)
                .AddStep(x => results.Add(x), 1)
                .Create();

            for (var i = 0; i < 1_000_000; i++)
            {
                _ = pipeline.Execute(i.ToString());
            }

            await pipeline.Execute("X");

            Console.WriteLine("Completed!");

            foreach (var result in results)
            {
                Console.WriteLine(result);
            }

            pipeline.Dispose();
        }

        private static string FindMostCommon(string input)
        {
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
