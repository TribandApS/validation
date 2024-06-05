using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Triband.Validation.Editor.Data;

namespace Triband.Validation.Editor
{
    sealed class SourceCodeParser : IDisposable
    {
        Dictionary<SourceInfo, string[]> _cachedArguments;

        internal SourceCodeParser()
        {
            _cachedArguments = new Dictionary<SourceInfo, string[]>();
        }

        public void Dispose()
        {
            _cachedArguments = null;
        }

        internal static string GetFileNameFromPath(string filePath)
        {
            var fileSplit = filePath.Split('\\');
            return fileSplit[fileSplit.Length - 1].Replace(".cs", "");
        }

        internal string[] ParseArguments(int argumentCount, string filePath, int lineNumber, out SourceInfo sourceInfo, [CallerMemberName] string callerMemberName = "")
        {
            try
            {
                sourceInfo = new SourceInfo(filePath, lineNumber);

                string[] result;
                
                if (_cachedArguments.TryGetValue(sourceInfo, out result))
                {
                    return result;
                }
             
                // Parse line one frame down
                var line = GetRemainingCodeInFile(filePath, callerMemberName, lineNumber);

                // Read arguments
                // Use first and last index to preserve internal function calls
                var firstIndex = line.IndexOf('(') + 1;

                int lastIndex = firstIndex;
                int depth = 1;
                for (int i = firstIndex; i < line.Length; i++)
                {
                    if (line[i] == '(')
                    {
                        depth++;
                    }

                    if (line[i] == ')')
                    {
                        depth--;
                    }

                    if (depth == 0)
                    {
                        lastIndex = i; 
                        break;
                    }
                }

                var argString = line.Substring(firstIndex, lastIndex - firstIndex).Replace(" ", "");
                var args = argString.Split(',');

                if (args.Length < argumentCount)
                {
                    throw new ArgumentException($"Unable to parse {argumentCount} arguments from {filePath}:{lineNumber}");
                }

                result = new string[argumentCount];
                for (int i = 0; i < argumentCount; i++)
                {
                    result[i] = args[i];
                }

                _cachedArguments.Add(sourceInfo, result);
                
                return result;
            }
#pragma warning disable CS0168
            catch (Exception e)
#pragma warning restore CS0168
            {
                sourceInfo = new SourceInfo("Error", 0);
                return new[] {"Error"};
            }
        }

        string GetRemainingCodeInFile(string filePath, string memberName, int lineNumber)
        {
            // Read up to target line
            var file = new System.IO.StreamReader(filePath);
            for (int i = 0; i < lineNumber - 1; i++)
                file.ReadLine();

            // Read requested line
            var remainingText = file.ReadToEnd();
            
            file.Close();

            remainingText = remainingText.Replace("\r\n", "");
            remainingText = remainingText.Replace("\n", "");

            var indexOfMemberName = remainingText.IndexOf(memberName, StringComparison.InvariantCulture);

            remainingText = remainingText.Substring(indexOfMemberName + memberName.Length);
            
            return remainingText;
        }
    }
}
