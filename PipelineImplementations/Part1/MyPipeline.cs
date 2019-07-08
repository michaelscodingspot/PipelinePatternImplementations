using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipelineImplementations.Part1
{
    public class MyPipeline
    {
        public bool Execute(string input)
        {
            string mostCommon = FindMostCommon(input);
            int characters = CountChars(mostCommon);
            bool isOdd = IsOdd(characters);
            return isOdd;
        }

        private string FindMostCommon(string input)
        {
            return input.Split(' ')
                .GroupBy(word => word)
                .OrderBy(group => group.Count())
                .Last()
                .Key;
        }

        private int CountChars(string mostCommon)
        {
            return mostCommon.Length;
        }

        private bool IsOdd(int number)
        {
            return number % 2 == 1;
        }
    }
}
