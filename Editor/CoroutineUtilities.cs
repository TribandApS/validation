using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.EditorCoroutines.Editor;

namespace Triband.Validation.Editor
{
    public static class CoroutineUtilities
    {
        public static void RunCoroutineSynchronously(IEnumerator coroutine)
        {
            var callstack = new Stack<IEnumerator>();
            callstack.Push(coroutine);
            while (callstack.Count > 0)
            {
                var currentEnumerator = callstack.Peek();
                if (currentEnumerator.MoveNext())
                {
                    if (currentEnumerator.Current != null)
                    {
                        if (currentEnumerator.Current is IEnumerator childEnumerator)
                        {
                            callstack.Push(childEnumerator);
                        }
                    }
                }
                else
                {
                    callstack.Pop();
                }
            }
        }

        public static IEnumerator RunCoroutineWithFixedInterval(IEnumerator coroutine,
            int intervalInMilliseconds = 10)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            yield return null;

            var callstack = new Stack<IEnumerator>();
            callstack.Push(coroutine);
            while (callstack.Count > 0)
            {
                var currentEnumerator = callstack.Peek();
                if (currentEnumerator.MoveNext())
                {
                    if (currentEnumerator.Current != null)
                    {
                        if (currentEnumerator.Current is IEnumerator childEnumerator)
                        {
                            callstack.Push(childEnumerator);
                        }
                    }
                }
                else
                {
                    callstack.Pop();
                }

                if (stopwatch.ElapsedMilliseconds > intervalInMilliseconds)
                {
                    stopwatch.Restart();

                    yield return new EditorWaitForSeconds(0);
                }
            }
        }
    }
}