using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipelineImplementations.Part1.BlockingCollection
{
    public class UsagePart1
    {
        public static void Use()
        {
            //GenericBCPipeline();
            //var pipeline = CastingPipeline();
            //var pipeline = InnerCastingPipeline();
            //var pipeline = GenericBCPipeline();
            //var pipeline = CastingPipelineWithParallelism();
            //var pipeline = CastingPipelineWithAwait();
            var pipeline = CreateGenericBCPipelineAwait();

            var tsk = System.Threading.Tasks.Task.Run(async () =>
            {
                Console.WriteLine(await pipeline.Execute("The pipeline pattern is the best pattern"));
                Console.WriteLine(await pipeline.Execute("The pipeline pattern is the best pattern"));
                Console.WriteLine(await pipeline.Execute("The pipeline pattern is the best pattern"));
                Console.WriteLine(await pipeline.Execute("The pipeline patter is the best patter"));
                Console.WriteLine(await pipeline.Execute("The pipeline pattern is the best pattern"));
            });
            tsk.Wait();

            //pipeline.Execute("The pipeline pattern is the best pattern");
            //pipeline.Execute("The pipeline pattern is the best pattern");
            //pipeline.Execute("The pipeline patter is the best patter");
            //pipeline.Execute("The pipeline pattern is the best pattern");

            //pipeline.Finished += res => Console.WriteLine(res);
        }

        private static GenericBCPipelineAwait<string, bool> CreateGenericBCPipelineAwait()
        {
            var pipeline = new GenericBCPipelineAwait<string, bool>((inputFirst, builder) =>
                inputFirst.Step2(builder, input => FindMostCommon(input))
                    .Step2(builder, input => input.Length)
                    .Step2(builder, input => input % 2 == 1));
            return pipeline;
        }

        private static IAwaitablePipeline<bool> CastingPipelineWithAwait()
        {
            var builder = new CastingPipelineWithAwait<bool>();
            builder.AddStep(input => FindMostCommon(input as string), 2, 10);
            builder.AddStep(input => (input as string).Length, 2, 10);
            builder.AddStep(input => ((int)input) % 2 == 1, 2, 10);
            var pipeline = builder.GetPipeline();
            return pipeline;
        }

        private static IPipeline CastingPipelineWithParallelism()
        {
            var builder = new CastingPipelineWithParallelism();
            builder.AddStep(input => FindMostCommon(input as string), 2);
            builder.AddStep(input => (input as string).Length, 2);
            builder.AddStep(input => ((int)input) % 2 == 1, 2);
            var pipeline = builder.GetPipeline();
            return pipeline;
        }

        private static IPipeline CastingPipeline()
        {
            var builder = new CastingPipelineBuilder();
            builder.AddStep(input => FindMostCommon(input as string));
            builder.AddStep(input => (input as string).Length);
            builder.AddStep(input => ((int)input) % 2 == 1);
            var pipeline = builder.GetPipeline();
            return pipeline;
        }

        private static IPipeline InnerCastingPipeline()
        {
            var builder = new InnerPipelineBuilder();
            builder.AddStep<string, string>(input => FindMostCommon(input));
            builder.AddStep<string, int>(input => CountChars(input));
            builder.AddStep<int, bool>(input => IsOdd(input));
            var pipeline = builder.GetPipeline();
            return pipeline;
        }

        private static GenericBCPipeline<string,bool> GenericBCPipeline()
        {
var pipeline = new GenericBCPipeline<string, bool>((inputFirst, builder) =>
    inputFirst.Step(builder, input => FindMostCommon(input))
        .Step(builder, input => input.Length)
        .Step(builder, input => input % 2 == 1));
            return pipeline;

            //pipeline.Execute("The pipeline pattern is the best pattern");
            //pipeline.Execute("The pipeline pattern is the best pattern");
            //pipeline.Execute("The pipeline pattern is the best pattern");
            //pipeline.Execute("The pipeline patter is the best patter");
            //pipeline.Execute("The pipeline pattern is the best pattern");

            //pipeline.Finished += res => Console.WriteLine(res);
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
