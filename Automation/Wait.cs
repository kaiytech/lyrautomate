using System.Diagnostics;

namespace LyrAutomate.Automation;

/// <summary>
/// Helper that lets you synchronously await conditions.
/// </summary>
public static class Wait
{
    /// <summary>
    /// Specify the action. This will be repeated over and over until the expected value is met.
    /// </summary>
    /// <param name="condition">Action to repeat</param>
    /// <param name="name">Friendly name (for logging purposes)</param>
    /// <typeparam name="T">Type (the return type of the action has to match the return type of the expected value)</typeparam>
    public static Condition<T> Until<T>(Func<T> condition, string name)
    {
        return new Condition<T>(condition, name);
    }
    
    public class Condition<T>(Func<T> condition, string name)
    {
        /// <summary>
        /// Specify the expected value.
        /// </summary>
        /// <param name="result">Expected value (has to match the return type of the action)</param>
        /// <param name="timeoutSeconds">Optional: timeout</param>
        /// <exception cref="TimeoutException">Thrown if the timeout is hit</exception>
        public void Is(T result, int timeoutSeconds = 30)
        {
            TestContext.Out.WriteLine($"Waiting for {name} to be {result} ({timeoutSeconds}s timeout)...");
            var sw = Stopwatch.StartNew();
            var output = condition();
            while (sw.Elapsed.TotalSeconds <= timeoutSeconds)
            {
                if (output is not null && output.Equals(result))
                {
                    TestContext.Out.WriteLine($"After waiting for {sw.Elapsed.TotalSeconds}s, {name} is {result}.");
                    return;
                }

                Thread.Sleep(100);

                output = condition();
            }

            throw new TimeoutException($"Timed out while waiting for {name} to be {result}. End result: {output}");
        }   
    }
}