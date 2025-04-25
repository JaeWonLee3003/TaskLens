using System.Diagnostics;

namespace TaskLens.Core
{
    public static class OllamaHelper
    {
        public static bool IsOllamaInstalled()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ollama",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
            }
        }
    }
}
