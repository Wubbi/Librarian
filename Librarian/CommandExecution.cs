using System;
using System.Diagnostics;

namespace Librarian
{
    public static class CommandExecution
    {
        /// <summary>
        /// Runs the given command in the systems shell
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static int RunWaitCommand(string command)
        {
            bool useBash = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;

            try
            {
                using (Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = useBash ? "/bin/bash" : "CMD.exe",
                        Arguments = useBash ? "-c" : "/C " + command,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                })
                {
                    process.Start();
                    process.WaitForExit();

                    return process.ExitCode;
                }
            }
#if DEBUG
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return -1;
            }
#else
            catch
            {
                return -1;
            }
#endif
        }
    }
}
