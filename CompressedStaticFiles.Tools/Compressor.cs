using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using static Spectre.Console.AnsiConsole;
using static System.IO.Path;

namespace CompressedStaticFiles.Tools;

internal static class Compressor
{
    private const string BrotliExtension = ".br";
    private const string GZipExtension = ".gz";

    private static bool _useBrotli;
    private static bool _useGzip;
    private static DirectoryInfo _workDir;
    private static Table _table;
    private static LiveDisplayContext _ctx;

    internal static async Task<int> Execute(string[] fileMasks, DirectoryInfo workDir, bool useGzip, bool useBrotli, uint batchSize)
    {
        _useBrotli = useBrotli;
        _useGzip = useGzip;
        _workDir = workDir;
        if (!_useBrotli && !_useGzip)
        {
            MarkupLine("[red]At least one compression algorithm is required[/]");
            return -1;
        }

        MarkupLineInterpolated($"[cyan]Working Directory: [/][yellow]{workDir.FullName}[/]");

        if (!workDir.Exists)
        {
            MarkupLine("[red]Directory doesn't exist[/]");
            return -1;
        }

        var files = (fileMasks ?? new[] { "*.*" })
                    .SelectMany(m => workDir.GetFiles(m, SearchOption.AllDirectories))
                    .ToLookup(x => x.FullName)
                    .Select(x => x.First())
                    .ToList();

        _table = new Table()
            .HeavyHeadBorder()
            .DoubleEdgeBorder()
            .Border(TableBorder.Rounded)
            .Collapse()
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Compression[/]")
            .AddColumn("[bold]File[/]")
            .AddColumn("[bold]Size[/]");

        await Live(_table)
            .StartAsync(async ctx =>
            {
                _ctx = ctx;
                await Task.Delay(100);
                var compressions = files.SelectMany(static f => CreateCompressionTasks(f)).ToList();

                var batches = SplitCompressions(compressions, Math.Clamp(batchSize, 1, 99));
                foreach (var batch in batches)
                {
                    await Task.WhenAll(batch.Select(task =>
                    {
                        var (fi, compress) = task;
                        return compress(fi);
                    }));
                }
            });

        return 0;
    }

    private static IEnumerable<IEnumerable<(FileInfo, Func<FileInfo, Task>)>> SplitCompressions(IReadOnlyList<(FileInfo, Func<FileInfo, Task>)> compressions, uint batchSize)
    {
        var bSize = Convert.ToInt32(batchSize); ;

        for (var i = 0; i < (float)compressions.Count / batchSize; i++)
        {
            yield return compressions.Skip(i * bSize).Take(bSize);
        }
    }

    private static IEnumerable<(FileInfo, Func<FileInfo, Task>)> CreateCompressionTasks(FileInfo fi)
    {
        if (_useBrotli)
        {
            yield return fi.Extension == BrotliExtension
                ? (fi, Skip("[steelblue1]BROTLI[/]"))
                : (fi, CreateBrotli);
        }

        if (_useGzip)
        {
            yield return fi.Extension == GZipExtension
                ? (fi, Skip("[chartreuse2]GZIP[/]"))
                : (fi, CreateGZip);
        }
    }

    private static Task CreateBrotli(FileInfo fi)
#if NET6_0_OR_GREATER
        => CreateCompressed(fi, BrotliExtension, "[steelblue1]BROTLI[/]", stream => new BrotliStream(stream, CompressionLevel.SmallestSize, false));
#else
        => CreateCompressed(fi, BrotliExtension, "[steelblue1]BROTLI[/]", stream => new BrotliStream(stream, CompressionLevel.Optimal, false));
#endif

    private static Task CreateGZip(FileInfo fi)
#if NET6_0_OR_GREATER
        => CreateCompressed(fi, GZipExtension, "[chartreuse2]GZIP[/]", stream => new GZipStream(stream, CompressionLevel.SmallestSize, false));
#else
        => CreateCompressed(fi, GZipExtension, "[chartreuse2]GZIP[/]", stream => new GZipStream(stream, CompressionLevel.Optimal, false));
#endif

    private static async Task CreateCompressed(FileInfo fi, string extension, string name, Func<Stream, Stream> createCompressStream)
    {
        var destFileInfo = new FileInfo(fi.FullName + extension);
        var filePath = GetRelativePath(_workDir.FullName, fi.FullName);
        var fileSize = fi.Length / 1024;
        var lastRow = _table.Rows.Count;
        using (var readStream = fi.OpenRead())
        {
            var row = _table.AddRow("[blue]open[/]", name, $"[bold]{filePath}[/]", $"[darkorange]{fileSize}KB[/]");
            _ctx.Refresh();
            using var brStream = createCompressStream(destFileInfo.OpenWrite());
            await readStream.CopyToAsync(brStream);
        }

        _ = _table.UpdateCell(lastRow, 0, "[green]done[/]");
        _ = _table.UpdateCell(lastRow, 2, $"[bold]{filePath} -> {GetRelativePath(_workDir.FullName, destFileInfo.FullName)}[/]");
        _ = _table.UpdateCell(lastRow, 3, $"[darkorange]{fileSize}KB -> {destFileInfo.Length / 1024}KB[/]");
        _ctx.Refresh();
    }

    private static Func<FileInfo, Task> Skip(string name)
        => fi =>
        {
            _ = _table.AddRow("[yellow]skip[/]", name, $"[bold]{GetRelativePath(_workDir.FullName, fi.FullName)}[/]", $"[darkorange]{fi.Length / 1024}KB[/]");
            _ctx.Refresh();
            return Task.CompletedTask;
        };
}
