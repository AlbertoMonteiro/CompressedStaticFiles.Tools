using System;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace DarkXaHTeP.CompressedStaticFiles.Tools
{
    public class Program
    {
        [Argument(0, "search_pattern1 search_pattern2 ...")]
        public string[] FileMasks { get; } = null;

        [Option("-w|--workdir", "Use directory relative to CWD", CommandOptionType.SingleValue)]
        public string WorkDir { get; } = null;

        [Option("-gz|--gzip", "Compress to GZip", CommandOptionType.NoValue)]
        public bool GZipRequested { get; } = false;
        
        [Option("-br|--brotli", "Compress to GZip", CommandOptionType.NoValue)]
        public bool GZipRequested { get; } = false;


        public int OnExecute()
        {
            Console.WriteLine("GZip: " + GZipRequested);
            
            var currentDirPath = Directory.GetCurrentDirectory();
            var workDirPath = WorkDir == null ? currentDirPath : Path.Combine(currentDirPath, WorkDir);
            
            var workDir = new DirectoryInfo(workDirPath);
            
            Console.WriteLine("Working Directory: " + workDirPath);

            if (!workDir.Exists)
            {
                Console.WriteLine("Directory doesn't exist");
                return -1;
            }

            var files = (FileMasks ?? new []{ "*.*" }).SelectMany(m => workDir.GetFiles(m, SearchOption.AllDirectories))
                .Select(fi => GetRelativePath(workDirPath, fi.FullName)).Distinct().ToArray();

            foreach (var file in files)
            {
                Console.WriteLine("- " + file);
            }

            return 0;
        }

        private string GetRelativePath(string workDirPath, string fullName)
        {
            return Path.GetRelativePath(workDirPath, fullName);
        }

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);
    }
}
