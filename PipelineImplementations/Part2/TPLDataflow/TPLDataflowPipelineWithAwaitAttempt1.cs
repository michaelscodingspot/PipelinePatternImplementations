using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PipelineImplementations.Part2.TPLDataflow
{
public class TC<TInput, TOutput>
{
    public TC(TInput input, TaskCompletionSource<TOutput> tcs)
    {
        Input = input;
        TaskCompletionSource = tcs;
    }
    public TInput Input { get; set; } 
    public TaskCompletionSource<TOutput> TaskCompletionSource{ get; set; }
}

public class TPLDataflowPipelineWithAwaitAttempt1
{
    public static TransformBlock<TC<string, bool>, TC<string, bool>> CreatePipeline()
    {
        var step1 = new TransformBlock<TC<string, bool>, TC<string, bool>>((tc) => 
            new TC<string,bool> (FindMostCommon(tc.Input), tc.TaskCompletionSource));

        var step2 = new TransformBlock<TC<string, bool>, TC<int, bool>>((tc) =>
            new TC<int,bool> (tc.Input.Length, tc.TaskCompletionSource));

        var step3 = new TransformBlock<TC<int, bool>, TC<bool, bool>>((tc) =>
            new TC<bool,bool> (tc.Input % 2 == 1, tc.TaskCompletionSource));

        var setResultStep = new ActionBlock<TC<bool, bool>>((tc) => tc.TaskCompletionSource.SetResult(tc.Input));
        
        step1.LinkTo(step2, new DataflowLinkOptions());
        step2.LinkTo(step3, new DataflowLinkOptions());
        step3.LinkTo(setResultStep, new DataflowLinkOptions());
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
