//
//  AppLovinBuildPostProcessor.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 10/30/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if UNITY_IOS || UNITY_IPHONE

using System.Diagnostics;
using System.IO;

/// <summary>
/// A helper class to run command line tools.
///
/// TODO: Currently only supports shell (Linux). Add support for Windows machines.
/// </summary>
public class AppLovinCommandLine
{
    /// <summary>
    /// Result obtained by running a command line command.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Standard output stream from command line.
        /// </summary>
        public string StandardOutput;

        /// <summary>
        /// Standard error stream from command line. 
        /// </summary>
        public string StandardError;

        /// <summary>
        /// Exit code returned from command line.
        /// </summary>
        public int ExitCode;

        /// <summary>
        /// The description of the result that can be used for error logging.
        /// </summary>
        public string Message;
    }

    /// <summary>
    /// Runs a command line tool using the provided <see cref="toolPath"/> and <see cref="arguments"/>.
    /// </summary>
    /// <param name="toolPath">The tool path to run</param>
    /// <param name="arguments">The arguments to be passed to the command line tool</param>
    /// <param name="workingDirectory">The directory from which to run this command.</param>
    /// <returns></returns>
    public static Result Run(string toolPath, string arguments, string workingDirectory)
    {
        var stdoutFileName = Path.GetTempFileName();
        var stderrFileName = Path.GetTempFileName();

        var process = new Process();
        process.StartInfo.UseShellExecute = true;
        process.StartInfo.CreateNoWindow = false;
        process.StartInfo.RedirectStandardInput = false;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;

        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.FileName = "bash";
        process.StartInfo.Arguments = string.Format("-l -c '\"{0}\" {1} 1> {2} 2> {3}'", toolPath, arguments, stdoutFileName, stderrFileName);
        process.Start();

        process.WaitForExit();

        var stdout = File.ReadAllText(stdoutFileName);
        var stderr = File.ReadAllText(stderrFileName);

        File.Delete(stdoutFileName);
        File.Delete(stderrFileName);

        var result = new Result();
        result.StandardOutput = stdout;
        result.StandardError = stderr;
        result.ExitCode = process.ExitCode;

        var messagePrefix = result.ExitCode == 0 ? "Command executed successfully" : "Failed to run command";
        result.Message = string.Format("{0}: '{1} {2}'\nstdout: {3}\nstderr: {4}\nExit code: {5}", messagePrefix, toolPath, arguments, stdout, stderr, process.ExitCode);

        return result;
    }
}

#endif
