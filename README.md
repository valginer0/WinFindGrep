# WinFindGrep

A Windows application for searching text in files across multiple directories, inspired by the functionality of the Unix "grep" command.

**Website:** [https://valginer0.github.io/WinFindGrepWebsite/](https://valginer0.github.io/WinFindGrepWebsite/)

## Features

- Search for text across multiple directories simultaneously
- Support for file filters (default: *.txt)
- Advanced search options:
  - Case sensitivity
  - Whole word matching
  - Regular expressions
  - Extended search with escape characters (\n, \r, \t)
- Replace functionality to update text in multiple files
- Results displayed with file path, line number, and content

- Double-click on results to open the file
- **Search History**: Remembers previously searched directories
- **Cancellation**: Stop long-running searches instantly

## Getting Started

### Prerequisites

- Windows operating system
- No additional dependencies required (self-contained application)

### Installation

1. Download the latest release executable from the [Releases](https://github.com/valginer0/WinFindGrep/releases) page
2. No installation needed - just run the .exe file

### Building from Source (Optional)

If you prefer to build from source:
1. Clone this repository or download the source code
2. Build the project using Visual Studio or JetBrains Rider
3. For a standalone executable, use the "Publish" feature in your IDE with "Self-contained" and "Single file" options

## Usage

1. Enter the text to search for in the "Find what:" field
2. Optionally, enter replacement text in the "Replace with:" field
3. Specify file filters (e.g., *.txt, *.cs, *.xml) separated by commas
4. Enter one or more directories to search in (separated by commas)
5. Select your search options:
   - Match whole word only
   - Match case
   - In all sub-folders
   - In hidden folders
6. Choose a search mode:
   - Normal: Standard text search
   - Extended: Support for escape sequences
   - Regular expression: Use regex patterns
7. Click "Find All" to search or "Replace in Files" to replace

## Development

The application is built with C# and .NET 9.0 using Windows Forms.

### Project Structure

- `Forms/`: Contains the UI components
- `Models/`: Contains data models
- `Services/`: Contains business logic for file operations

## Support

If you find this tool useful, consider supporting its development:

[![GitHub Sponsors](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/valginer0)
[![Donate with PayPal](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://www.paypal.me/ValeryGiner)
[![Ko-fi](https://img.shields.io/badge/Support-Ko--fi-FF5E5B.svg)](https://ko-fi.com/valginer)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy_Me_A_Coffee-FFDD00.svg?logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/valginer)
[![Patreon](https://img.shields.io/badge/Support-Patreon-F96854.svg?logo=patreon)](https://patreon.com/ValeryGiner)

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Inspired by grep command-line utility
- Built with .NET and Windows Forms
