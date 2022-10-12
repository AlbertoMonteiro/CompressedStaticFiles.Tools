using CompressedStaticFiles.Tools;
using System.CommandLine;
using System.IO;

var rootCommand = new RootCommand("A CLI tool that allows to compress static assets using Brotli or GZip");
var fileMasks = new Argument<string[]>("search_pattern1 search_pattern2 ...");
rootCommand.AddArgument(fileMasks);
var workDir = new Option<DirectoryInfo>(new[] { "-w", "--workdir" }, () => new DirectoryInfo(Directory.GetCurrentDirectory()), "Use directory relative to CWD");
rootCommand.AddOption(workDir);
var useGzip = new Option<bool>(new[] { "-gz", "--gzip" }, "Compress to GZip");
rootCommand.AddOption(useGzip);
var useBrotli = new Option<bool>(new[] { "-br", "--brotli" }, "Compress to GZip");
rootCommand.AddOption(useBrotli);
var batchSize = new Option<uint>("--batch", () => 1, "Number of compressions to execute simultaneously");
rootCommand.AddOption(batchSize);

rootCommand.SetHandler(Compressor.Execute, fileMasks, workDir, useGzip, useBrotli, batchSize);

return await rootCommand.InvokeAsync(args);