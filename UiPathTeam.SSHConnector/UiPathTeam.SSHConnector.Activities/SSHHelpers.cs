using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UiPathTeam.SSHConnector.Activities
{
    public static class StringExt
    {
        public static string StringBeforeLastRegEx(this string str, Regex regex)
        {
            var matches = regex.Matches(str);
            return matches.Count > 0
                ? str.Substring(0, matches[matches.Count - 1].Index)
                : str;
        }

        public static bool EndsWithRegEx(this string str, Regex regex)
        {
            var matches = regex.Matches(str);

            return
                matches.Count > 0 &&
                str.Length == (matches[matches.Count - 1].Index + matches[matches.Count - 1].Length);
        }

        public static string StringAfter(this string str, string substring)
        {
            var index = str.IndexOf(substring, StringComparison.Ordinal);

            return index >= 0
                ? str.Substring(index + substring.Length)
                : "";
        }

        public static string[] GetLines(this string str) =>
            Regex.Split(str, "\r\n|\r|\n");
    }

    public static class UtilExt
    {
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> func)
        {
            foreach (var item in sequence)
            {
                func(item);
            }
        }
    }

    public static class SSHHelpers
    {
        public static string Expect(ShellStream shell, Regex expectedShellPrompt, string lastCommand, TimeSpan timeout)
        {
            var result = shell.Expect(expectedShellPrompt, timeout);

            if (result == null)
            {
                var buffer = new byte[4096];
                shell.Read(buffer, 0, buffer.Length);
                var lastReadData = System.Text.Encoding.Default.GetString(buffer).Replace("\0", string.Empty);

                throw new Exception($"Timeout {timeout.TotalSeconds}s executing {lastCommand}\nExpected Regex: {expectedShellPrompt.ToString()}\nReceived text: {lastReadData}");
            }

            result = result.Contains(lastCommand) ? result.StringAfter(lastCommand) : result;
            result = result.StringBeforeLastRegEx(expectedShellPrompt).Trim();
            return result;
        }

        public static string Execute(ShellStream shell, Regex expectedShellPrompt, string commandLine, TimeSpan timeout, bool checkExitCode)
        {
            Exception exception = null;
            string result;
            var errorMessage = "failed";
            var errorCode = "exception";

            try
            {
                shell.WriteLine(commandLine);
                result = Expect(shell, expectedShellPrompt, commandLine, timeout);

                if (checkExitCode)
                {
                    shell.WriteLine("echo $?");
                    errorCode = Expect(shell, expectedShellPrompt, "echo $?", timeout);

                    if (errorCode == "0")
                    {
                        return result;
                    }
                    else if (result.Length > 0)
                    {
                        errorMessage = result;
                    }
                }
                else
                {
                    return result;
                }

            }
            catch (Exception ex)
            {
                exception = ex;
                errorMessage = ex.Message;
            }

            throw new Exception($"Ssh error: {errorMessage}, code: {errorCode}, command: {commandLine}", exception);
        }
    }
}
