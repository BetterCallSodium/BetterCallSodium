using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BetterCallSodium.Tests
{
    internal static class ProcessUtils
    {
        private static readonly object ConsoleEncodingLock = new();
        private static Encoding? _encodingCache;

        private static Encoding ConsoleEncoding
        {
            get
            {
                lock (ConsoleEncodingLock)
                {
                    if (_encodingCache != null)
                        return _encodingCache;

                    if (OperatingSystem.IsWindows())
                    {
                        try
                        {
                            using Process process = Process.Start(new ProcessStartInfo("chcp")
                            {
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true
                            })!;

                            using StreamReader standardOutput = process.StandardOutput;

                            int cp = int.Parse(string.Concat(standardOutput.ReadLine()!.Where(char.IsDigit)));

                            if (cp <= 0)
                                throw new InvalidOperationException();

                            _encodingCache = Encoding.GetEncoding(cp);
                            return _encodingCache;
                        }
                        catch
                        {
                            // ignore
                        }

                        try
                        {
                            int codePage = (int)GetConsoleCodePage();

                            if (codePage <= 0)
                                throw new Win32Exception();

                            _encodingCache = Encoding.GetEncoding(codePage);
                            return _encodingCache;
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    _encodingCache = Console.OutputEncoding;
                    return _encodingCache;
                }
            }
        }

        [DllImport("Kernel32.dll", EntryPoint = "GetConsoleOutputCP", SetLastError = true)]
        private static extern uint GetConsoleCodePage();

        /// <summary>
        /// Creates a process and waits for it to exit
        /// </summary>
        /// <param name="fileName">Executable name</param>
        /// <param name="arguments">Arguments</param>
        /// <param name="output">Process output</param>
        /// <param name="error">Process error</param>
        /// <param name="workingDirectory">Working directory, current if null</param>
        /// <returns>Process exit code</returns>
        public static int GetProcessOutput(string fileName, string arguments, out string output, out string error, string? workingDirectory = null)
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo(fileName, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = ConsoleEncoding,
                    StandardErrorEncoding = ConsoleEncoding,
                    WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
                }
            };

            object outputLock = new();
            StringBuilder outputBuilder = new();

            object errorLock = new();
            StringBuilder errorBuilder = new();

            process.OutputDataReceived += (_, args) =>
            {
                lock (outputLock)
                    outputBuilder.AppendLine(args.Data);
            };

            process.ErrorDataReceived += (_, args) =>
            {
                lock (errorLock)
                    errorBuilder.AppendLine(args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            lock (outputLock)
                output = outputBuilder.ToString().Trim();
            lock (errorLock)
                error = errorBuilder.ToString().Trim();

            return process.ExitCode;
        }
    }
}
