# CompressedStaticFiles.Tools
A CLI tool that allows to compress static assets using Brotli or GZip to be served by CompressedStaticFiles middleware later

# Usage

```
Usage: CompressedStaticFiles.Tools [options] <search_pattern1 search_pattern2 ...>

Arguments:
  search_pattern1 search_pattern2 ...

Options:
  -w|--workdir                         Use directory relative to CWD
  -gz|--gzip                           Compress to GZip
  -br|--brotli                         Compress to GZip
  --batch                              Number of compressions to execute simultaneously
                                       Default value is: 1.
  -?|-h|--help                         Show help information.
```

Example: Compress all files in current directory recursively.
```
dotnet-compress *.* -br -gz
```