# CompressedStaticFiles.Tools
A CLI tool that allows to compress static assets using Brotli or GZip

# Usage

```
Description:
  A CLI tool that allows to compress static assets using Brotli or GZip

Usage:
  CompressedStaticFiles.Tools [<search_pattern1 search_pattern2 ...>...] [options]

Arguments:
  <search_pattern1 search_pattern2 ...>

Options:
  -w, --workdir <workdir>  Use directory relative to CWD [default:
                           C:\Dev\CompressedStaticFiles.Tools\CompressedStaticFiles.Tools\bin\Debug\net5.0]
  -gz, --gzip              Compress to GZip
  -br, --brotli            Compress to GZip
  --batch <batch>          Number of compressions to execute simultaneously [default: 1]
  --version                Show version information
  -?, -h, --help           Show help and usage information
```

Example: Compress all files in current directory recursively.
```
dotnet-compress *.* -br -gz
```