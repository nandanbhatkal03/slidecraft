# Mini PowerPoint (C# / WPF)

A lightweight PowerPoint-style desktop app built with .NET 8, WPF and the Open XML SDK.

## Features

### Mandatory
- **R1: Create a new presentation** (`MainViewModel.NewCommand`, Ctrl+N) — Starts with one clean blank slide.
- **R2: Add slides** (`MainViewModel.AddSlideCommand`, Ctrl+M) — Appends slides to the deck.
- **R3: Add text boxes** (`MainViewModel.AddTextBoxCommand`, 'T' toolbar button) — Editable, draggable, resizable text blocks.
- **R4: Add images** (`MainViewModel.AddImageCommand`, Image button) — PNG, JPG, JPEG, GIF support; preserves aspect ratio and fits within 400×300 pt.
- **R5: Save the presentation (.pptx)** (`PptxExportService`, Ctrl+S / Ctrl+Shift+S) — Generates compliant Open XML `.pptx` files that open natively in Microsoft PowerPoint and LibreOffice Impress.
- **R6: Open existing .pptx** (`PptxImportService`, Ctrl+O) — Reads slides, text shapes, font styles, colors, positions, pictures, transitions; gracefully skips unsupported elements without throwing.

### Optional (All Implemented)
- **O1: Slide layouts** (`LayoutFactory`) — Title Slide, Title and Content, and Blank layouts selectable from dropdown.
- **O2: Text formatting** (`FormatBar` + `TextElement`) — Bold (Ctrl+B), Italic (Ctrl+I), font size dropdown (10–72 pt + custom editable values), and 8-color palette.
- **O3: Slide transitions** (`SlideModel.Transition`) — None, Fade, Push, and Wipe transitions selectable per slide and exported as Open XML `<p:transition>`.
- **O4: Slideshow mode** (`SlideshowWindow`, F5) — Fullscreen presentation with arrow keys (←/→/↑/↓), Space, PageUp/PageDown, Home/End, mouse click, and Esc to exit; animated transition effects.
- **O5: Deck manipulation & Undo/Redo** (`MainViewModel`) — Delete slide, duplicate slide, move slide up/down; full Memento-based Undo (Ctrl+Z) and Redo (Ctrl+Y).
- **O6: Bilingual UI Architecture** (`LocalizationService`) — All UI strings wired via `{DynamicResource ...}` keys in English (`Strings.en.xaml`) and Kannada (`Strings.kn.xaml`) ready for instant live toggle.

## Run

1. Install [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows 10/11)
2. Clone repository and navigate to project folder:
   ```bash
   git clone <repo>
   cd slidecraft
   ```
3. Run the application:
   ```bash
   dotnet run --project src/slidecraft
   ```
   Or publish a standalone single-file executable:
   ```bash
   dotnet publish src/slidecraft -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
   ```

## Tests

Execute the automated test suite covering full Open XML round-trip tests, validator checks, and edge cases:
```bash
dotnet test
```

## Libraries

| Library | Purpose | Why |
|---|---|---|
| **DocumentFormat.OpenXml (3.5.1)** | Read and write `.pptx` presentations | Official Microsoft Open XML SDK, MIT licence, assignment-recommended for C#, produces valid Office PresentationML without requiring Microsoft Office installation. |
| **CommunityToolkit.Mvvm (8.4.2)** | MVVM architecture helpers | Eliminates boilerplate through compile-time source generators (`[ObservableProperty]`, `[RelayCommand]`), ensuring clean and robust code quality. |

## Usage / Shortcuts

| Shortcut / Action | Command / Operation | Description |
|---|---|---|
| `Ctrl+N` | New Presentation | Creates a new presentation with one blank slide (prompts if dirty) |
| `Ctrl+O` | Open Presentation | Opens an existing `.pptx` file (prompts if dirty) |
| `Ctrl+S` | Save Presentation | Saves presentation to existing path or prompts for path |
| `Ctrl+Shift+S` | Save As | Prompts for a path and saves `.pptx` |
| `Ctrl+M` | New Slide | Adds a new slide to the presentation |
| `Ctrl+Z` | Undo | Reverts the last edit / slide operation |
| `Ctrl+Y` | Redo | Reapplies the last undone operation |
| `Ctrl+B` | Toggle Bold | Toggles bold weight on selected text element |
| `Ctrl+I` | Toggle Italic | Toggles italic style on selected text element |
| `F5` | Start Slideshow | Opens fullscreen presentation starting at active slide |
| `Delete` | Delete Element | Deletes the currently selected text box or image |
| `Double-click` | Edit Text | Opens inline text editing for the clicked text box |
| `Drag element` | Move Element | Repositions the element on the 960×540 canvas |
| `Corner handles` | Resize Element | Resizes width and height from any corner |
| `Right-click slide` | Context Menu | Duplicate, delete, or move slides up/down |
| `Arrow Keys / Space` | Slideshow Next / Prev | Navigates slides during slideshow mode |
| `Esc` | Exit Slideshow | Returns from fullscreen slideshow to the editor |

## Language

Menu → Language → English / ಕನ್ನಡ  
The application architecture is fully prepared for bilingual operation using dynamic resource dictionaries and `Nirmala UI` font rendering.

## Screenshots

![Main UI](docs/screenshots/main.png)
![Kannada UI](docs/screenshots/kannada.png)

## Limitations

- Charts, SmartArt, tables, and complex animation timelines from external decks are not imported (they are gracefully skipped and reported via skipped element counter).
- Slide elements are focused on text boxes and raster images (PNG, JPEG, GIF).
