using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace DarkXaHTeP.CompressedStaticFiles.Tools
{
    public class Program
    {
        private const string BrotliExtension = ".br";
        private const string GZipExtension = ".gz";

        private string _workDirPath;

        [Argument(0, "search_pattern1 search_pattern2 ...")]
        public string[] FileMasks { get; } = null;

        [Option("-w|--workdir", "Use directory relative to CWD", CommandOptionType.SingleValue)]
        public string WorkDir { get; } = null;

        [Option("-gz|--gzip", "Compress to GZip", CommandOptionType.NoValue)]
        public bool UseGZip { get; } = false;
        
        [Option("-br|--brotli", "Compress to GZip", CommandOptionType.NoValue)]
        public bool UseBrotli { get; } = false;

        [Option("--batch", "Number of compressions to execute simultaneously", CommandOptionType.SingleValue)]
        public uint BatchSize { get; } = 1;

        public async Task<int> OnExecute()
        {
            if (!UseBrotli && !UseGZip)
            {
                Console.WriteLine("At least one compression algorithm is required");
                return -1;
            }
            
            var currentDirPath = Directory.GetCurrentDirectory();
            _workDirPath = WorkDir == null ? currentDirPath : Path.Combine(currentDirPath, WorkDir);
            
            var workDir = new DirectoryInfo(_workDirPath);
            
            Console.WriteLine("Working Directory: " + _workDirPath);

            if (!workDir.Exists)
            {
                Console.WriteLine("Directory doesn't exist");
                return -1;
            }

            var files = (FileMasks ?? new []{ "*.*" })
                .SelectMany(m => workDir.GetFiles(m, SearchOption.AllDirectories))
                .ToLookup(x => x.FullName)
                .Select(x => x.First())
                .ToList();

            var compressions = files.SelectMany(f => CreateCompressionTasks(f)).ToList();

            var batchSize = (BatchSize == 0 || BatchSize > 99) ? 1 : BatchSize;

            var batches = SplitCompressions(compressions, batchSize);

            foreach (var batch in batches)
            {
                await Task.WhenAll(batch.Select(task =>
                {
                    var (fi, compress) = task;
                    return compress(fi);
                }));
            }

            return 0;
        }

        private IEnumerable<IEnumerable<(FileInfo, Func<FileInfo, Task>)>> SplitCompressions(IReadOnlyList<(FileInfo, Func<FileInfo,Task>)> compressions, uint batchSize)
        {
            int bSize = Convert.ToInt32(batchSize);;

            for (var i = 0; i < (float)compressions.Count / batchSize; i++)
            {
                yield return compressions.Skip(i * bSize).Take(bSize);
            }
        }

        private IEnumerable<(FileInfo, Func<FileInfo, Task>)> CreateCompressionTasks(FileInfo fi)
        {
            if (UseGZip)
            {
                if (fi.Extension == GZipExtension)
                {
                    yield return (fi, Skip("GZIP"));
                }
                else
                {
                    yield return (fi, CreateGZip);
                }
            }

            if (UseBrotli)
            {
                if (fi.Extension == BrotliExtension)
                {
                    yield return (fi, Skip("BROTLI"));
                }
                else
                {

                    yield return (fi, CreateBrotli);
                }
            }
        }

        private async Task CreateBrotli(FileInfo fi)
        {
            await CreateCompressed(fi, BrotliExtension, "BROTLI", stream => new BrotliStream(stream, CompressionLevel.Optimal));
        }

        private async Task CreateGZip(FileInfo fi)
        {
            await CreateCompressed(fi, GZipExtension, "GZIP", stream => new GZipStream(stream, CompressionLevel.Optimal));
        }

        private async Task CreateCompressed(FileInfo fi, string extension, string name, Func<Stream, Stream> createCompressStream)
        {
            var destFileInfo = new FileInfo(fi.FullName + extension);

            using (var readStream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Console.WriteLine($"[{name}][Open] {GetRelativePath(_workDirPath, fi.FullName)} [{fi.Length / 1024}KB]");
                using (var writeStream = destFileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var brStream = createCompressStream(writeStream))
                    {
                        await readStream.CopyToAsync(brStream);                        
                    }
                }
            }

            Console.WriteLine($"[{name}][Done] {GetRelativePath(_workDirPath, destFileInfo.FullName)} [{destFileInfo.Length / 1024}KB]");
        }

        private Func<FileInfo, Task> Skip(string name)
        {
            return fi =>
            {
                Console
                    .WriteLine($"[{name}][Skip] {GetRelativePath(_workDirPath, fi.FullName)} [{fi.Length / 1024}KB]");
                return Task.CompletedTask;
            };
        }

        private string GetRelativePath(string workDirPath, string fullName)
        {
            return Path.GetRelativePath(workDirPath, fullName);
        }

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);
    }
}
