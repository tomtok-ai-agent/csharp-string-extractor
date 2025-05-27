# C# String Literal Extractor

A command-line tool that scans C# source code and extracts string literals using AST (Abstract Syntax Tree) analysis.

## Features

- Recursively scans directories for C# source files
- Uses Roslyn for accurate C# syntax parsing
- Extracts all string literals from each file
- Outputs results as a structured JSON file
- Minimal dependencies

## Installation

```bash
# Clone the repository
git clone https://github.com/tomtok-ai-agent/csharp-string-extractor.git

# Build the project
cd csharp-string-extractor
dotnet build
```

## Usage

```bash
# Run the tool
dotnet run --project src/CSharpStringExtractor/CSharpStringExtractor.csproj -- --directory <source-directory> --output <output-file.json>
```

### Parameters

- `--directory` or `-d`: The directory containing C# source code files to analyze (required)
- `--output` or `-o`: The output JSON file path (required)

## Output Format

The tool generates a JSON file with the following structure:

```json
{
  "path/to/file1.cs": [
    "String literal 1",
    "String literal 2"
  ],
  "path/to/file2.cs": [
    "Another string literal"
  ]
}
```

## Project Structure

- `src/CSharpStringExtractor`: Main application code
- `tests/CSharpStringExtractor.Tests`: Unit tests

## Dependencies

- Microsoft.CodeAnalysis.CSharp: For C# syntax parsing
- System.CommandLine: For command-line argument parsing

## License

MIT
