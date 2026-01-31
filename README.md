# File Processor

A high-performance file sorting system in C# capable of handling files up to 100GB using external merge sort.

## Overview

File Processor consists of two command-line tools:

- **FP.Generator** - Generates test files with configurable size and duplicate content
- **FP.Sorter** - Sorts large files using external merge sort algorithm

### Input Format

Files use the format `<Number>. <String>` where each line contains:
- A number (up to 64-bit integer)
- A period and space separator (`. `)
- A text string

Example:
```
415. Apple
30432. Something something something
1. Apple
32. Cherry
2. Banana
```

### Sorting Criteria

Lines are sorted by:
1. **String part** (alphabetically, ordinal comparison)
2. **Number part** (ascending) when strings are equal

After sorting, the example above becomes:
```
1. Apple
415. Apple
2. Banana
32. Cherry
30432. Something something something
```

## Requirements

- .NET 10.0 SDK or later
- Windows, macOS, or Linux

## Building

```bash
# Clone the repository
git clone https://github.com/yourusername/file-processor.git
cd file-processor

# Build the solution
dotnet build src/FileProcessor.sln
```

## Usage

### FP.Generator - Test File Generator

Generate test files with random content for testing the sorter.

```bash
dotnet run --project src/FP.Generator -- [options]
```

**Options:**

| Option | Description | Default |
|--------|-------------|---------|
| `-s, --size <size>` | Target file size (e.g., 10MB, 1GB) | 10MB |
| `-o, --output <path>` | Output file path | output.txt |
| `-d, --duplicates <0-100>` | Duplicate percentage | 10 |
| `--seed <number>` | Random seed for reproducible generation | (random) |
| `--min-string-length <n>` | Minimum string length | 5 |
| `--max-string-length <n>` | Maximum string length | 50 |

**Examples:**

```bash
# Generate a 100MB file
dotnet run --project src/FP.Generator -- -s 100MB -o test.txt

# Generate a 1GB file with 20% duplicates
dotnet run --project src/FP.Generator -- -s 1GB -o large.txt -d 20

# Generate reproducible output with a seed
dotnet run --project src/FP.Generator -- -s 50MB -o test.txt --seed 12345
```

### FP.Sorter - External Merge Sort

Sort large files using memory-efficient external merge sort.

```bash
dotnet run --project src/FP.Sorter -- [options]
```

**Options:**

| Option | Description | Default |
|--------|-------------|---------|
| `-i, --input <path>` | Input file to sort **(required)** | - |
| `-o, --output <path>` | Output file path | `<input>_sorted.<ext>` |
| `-m, --memory <size>` | Maximum memory to use (e.g., 512MB, 1GB) | 512MB |
| `-k, --merge-way <n>` | Number of chunks to merge at once | 16 |
| `-t, --temp <path>` | Temporary directory for chunks | (auto) |
| `--keep-temp` | Don't delete temporary files after sorting | false |

**Examples:**

```bash
# Sort a file with default settings
dotnet run --project src/FP.Sorter -- -i input.txt

# Sort with custom output path
dotnet run --project src/FP.Sorter -- -i input.txt -o sorted.txt

# Sort a large file with 1GB memory limit
dotnet run --project src/FP.Sorter -- -i large.txt -m 1GB

# Sort with custom temp directory
dotnet run --project src/FP.Sorter -- -i input.txt -t /path/to/temp
```

## How It Works

### External Merge Sort Algorithm

The sorter uses a two-phase external merge sort algorithm optimized for files that don't fit in memory:

**Phase 1: Chunk Creation**
1. Read the input file in chunks that fit within the memory limit
2. Sort each chunk in memory using the comparison criteria
3. Write sorted chunks to temporary files

**Phase 2: K-Way Merge**
1. Use a priority queue for efficient k-way merging (default k=16)
2. Merge chunks into larger sorted chunks
3. Repeat until a single output file remains

**Performance Example (100GB file, 512MB memory):**
- ~200 sorted chunks created
- Pass 1: 200 → 13 merged files
- Pass 2: 13 → 1 final file
- Total: 2 merge passes

### Memory Management

- Configurable maximum memory usage
- Buffer pooling for efficient I/O operations
- Automatic cleanup of temporary files
- Controlled chunk sizes to prevent memory overflow

## Project Structure

```
file-processor/
├── src/
│   ├── FileProcessor.sln
│   ├── FP.Common/                    # Shared library
│   │   ├── Models/
│   │   │   └── FileLine.cs           # Line data structure
│   │   ├── Interfaces/
│   │   │   └── ILineParser.cs        # Parser interface
│   │   ├── Parsing/
│   │   │   └── LineParser.cs         # Line parsing logic
│   │   └── Comparison/
│   │       └── FileLineComparer.cs   # Sorting comparison logic
│   │
│   ├── FP.Generator/                 # Test file generator
│   │   ├── Program.cs                # CLI entry point
│   │   ├── Configuration/
│   │   │   └── GeneratorOptions.cs   # Generator settings
│   │   ├── Generators/
│   │   │   ├── IFileGenerator.cs
│   │   │   └── FileGenerator.cs      # File generation logic
│   │   └── Providers/
│   │       ├── IRandomContentProvider.cs
│   │       └── RandomContentProvider.cs
│   │
│   └── FP.Sorter/                    # File sorter
│       ├── Program.cs                # CLI entry point
│       ├── Configuration/
│       │   └── SorterOptions.cs      # Sorter settings
│       ├── Sorters/
│       │   ├── IExternalSorter.cs
│       │   ├── IChunkSorter.cs
│       │   ├── ExternalMergeSorter.cs # Main orchestrator
│       │   └── ChunkSorter.cs         # Chunk creation
│       └── Mergers/
│           ├── IChunkMerger.cs
│           └── KWayMerger.cs          # K-way merge logic
│
└── Tests/
    ├── FP.Common.Tests/              # Common library tests
    ├── FP.Generator.Tests/           # Generator tests
    └── FP.Sorter.Tests/              # Sorter tests (unit + integration)
```

## Running Tests

```bash
# Run all tests
dotnet test src/FileProcessor.sln

# Run specific test projects
dotnet test Tests/FP.Common.Tests
dotnet test Tests/FP.Generator.Tests
dotnet test Tests/FP.Sorter.Tests

# Run with verbose output
dotnet test src/FileProcessor.sln --logger "console;verbosity=detailed"
```

## Quick Start

```bash
# 1. Build the solution
dotnet build src/FileProcessor.sln

# 2. Generate a 100MB test file
dotnet run --project src/FP.Generator -- -s 100MB -o test.txt

# 3. Sort the file
dotnet run --project src/FP.Sorter -- -i test.txt -o sorted.txt

# 4. Verify the output (first 20 lines)
# On Windows PowerShell:
Get-Content sorted.txt -Head 20

# On Linux/macOS:
head -20 sorted.txt
```

## Performance Tips

1. **Memory allocation**: Increase `-m` for faster sorting when RAM is available
2. **Merge factor**: Higher `-k` values reduce merge passes but use more file handles
3. **SSD storage**: Place temp directory on SSD for faster I/O during merge phases
4. **Large files**: For 100GB+ files, use at least 1GB memory allocation

## Dependencies

- [System.CommandLine](https://github.com/dotnet/command-line-api) (2.0.2) - CLI argument parsing
- [NUnit](https://nunit.org/) (4.0.1) - Unit testing framework
- [Moq](https://github.com/moq/moq4) (4.20.70) - Mocking framework for tests
