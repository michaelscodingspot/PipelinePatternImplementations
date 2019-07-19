using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipelineImplementations.Part1;
using PipelineImplementations.Part1.BlockingCollection;
using PipelineImplementations.Part2;

namespace PipelineImplementations
{
    class Program
    {
        static void Main(string[] args)
        {
            //string input = "The pipeline pattern is the best pattern";
            //var pipeline = new MyPipeline();
            //Console.Write(pipeline.Execute(input)); // Returns 'True' because 'pattern' is the most common, with 7 characters and it's indeed an odd number
            //UsagePart1.Use();
            UsagePart2.Use();
            Console.ReadKey();
        }
    }
}
