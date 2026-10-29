# slidecraft — Project Report

## 1. Introduction

### Assignment Goal
The objective of this assignment is to design and develop a robust, desktop-based presentation application replicating core Microsoft PowerPoint functionality. The implementation focuses on rich user interface interactions, robust Open XML file serialization, document lifecycle operations, and extensible software architecture.

### Technology Stack
The application is built on modern .NET technology adhering to industry best practices:

| Component | Choice | Justification |
|---|---|---|
| **Language** | C# 12 / .NET 8 (`net8.0-windows`) | High-performance type system, pattern matching, record types, and modern language productivity. |
| **GUI Framework** | Windows Presentation Foundation (WPF) | Vector-based resolution-independent rendering, hardware acceleration, flexible data binding, templating, and advanced typography engine supporting Indic scripts (Nirmala UI). |
| **Open XML Library** | DocumentFormat.OpenXml 3.5.1 (Microsoft Open XML SDK) | Official, MIT-licensed standard library for Office Open XML files (`.pptx`); produces native PowerPoint files without requiring Office interop or installation. |
| **MVVM Toolkit** | CommunityToolkit.Mvvm 8.4.2 | Official Microsoft-backed MVVM library offering compile-time source generators for properties and commands. |
| **Unit Testing** | xUnit 2.9 + OpenXmlValidator | Industry-standard unit test runner paired with schema and semantic Open XML validation. |

---

## 2. Features Implemented

Every mandatory and optional requirement specified in the assignment brief and design specification has been implemented and verified:

| # | Requirement / Feature | Category | Status | Implementation Details |
|---|---|---|:---:|---|
| **R1** | Create new presentation | Mandatory | ✅ | `MainViewModel.NewCommand`: Prompts to save if dirty, initializes a clean deck with one blank slide. |
| **R2** | Add slides | Mandatory | ✅ | `MainViewModel.AddSlideCommand`: Adds slides dynamically, auto-numbering thumbnails. |
| **R3** | Add text boxes | Mandatory | ✅ | `MainViewModel.AddTextBoxCommand`: Adds draggable, resizable text elements with inline double-click editing. |
| **R4** | Add images | Mandatory | ✅ | `MainViewModel.AddImageCommand`: Loads PNG, JPG, JPEG, GIF; constrains to 400×300 pt while preserving aspect ratio. |
| **R5** | Save to `.pptx` | Mandatory | ✅ | `PptxExportService.Save`: Writes valid PresentationML packages with theme, slide master, layout parts, shapes, and pictures. |
| **R6** | Open existing `.pptx` | Mandatory | ✅ | `PptxImportService.Open`: Imports slides, shapes, text runs, colors, fonts, images, and transitions; safely handles corrupt/unsupported shapes. |
| **O1** | Slide layouts | Optional | ✅ | `LayoutFactory`: Title Slide (44pt title + 24pt subtitle), Title and Content (36pt title + 24pt body), and Blank slide layouts. |
| **O2** | Text formatting | Optional | ✅ | `FormatBar` + `TextElement`: Bold toggle, Italic toggle, font size combo (10–72 pt + custom), 8-color palette. |
| **O3** | Slide transitions | Optional | ✅ | `SlideModel.Transition`: None, Fade, Push, Wipe selectable per slide and serialized to Open XML `<p:transition>`. |
| **O4** | Slideshow mode | Optional | ✅ | `SlideshowWindow`: Fullscreen display with arrow keys, Space, PageUp/Down, Home/End, Esc, and transition animations. |
| **O5** | Slide deck management & Undo/Redo | Optional | ✅ | Duplicate slide, delete slide, reorder slides (Move Up/Down); Memento-based snapshot Undo (Ctrl+Z) and Redo (Ctrl+Y). |
| **O6** | English ↔ Kannada UI toggle | Optional | ✅ | `LocalizationService`: Dynamic resource dictionaries (`Strings.en.xaml` / `Strings.kn.xaml`) and `Nirmala UI` font rendering. |

---

## 3. Architecture

The application strictly follows the **Model-View-ViewModel (MVVM)** design pattern, guaranteeing clean separation of presentation logic, domain models, and file serialization services.

```
┌────────────────────────────────────────────────────────┐
│                      Views (WPF)                       │
│  MainWindow  •  FormatBar  •  SlideshowWindow          │
│  DraggableElement  •  ResizeAdorner                    │
└───────────────────────────┬────────────────────────────┘
                            │ Data Binding & Commands
┌───────────────────────────▼────────────────────────────┐
│                  ViewModels (MVVM Toolkit)             │
│  MainViewModel  •  SlideViewModel  •  ElementViewModel │
│  RelayUndoStack                                        │
└───────────────────────────┬────────────────────────────┘
                            │ Domain State
┌───────────────────────────▼────────────────────────────┐
│                    Domain Models                       │
│  PresentationModel  •  SlideModel  •  SlideElement     │
│  TextElement  •  ImageElement  •  TransitionType       │
└───────────────────────────┬────────────────────────────┘
                            │ Serialization / File I/O
┌───────────────────────────▼────────────────────────────┐
│                      Services                          │
│  PptxExportService  •  PptxImportService               │
│  LayoutFactory  •  DialogService  • LocalizationService│
└────────────────────────────────────────────────────────┘
```

### Coordinate & Scaling Model
- Coordinates are stored in **points** on a standard **960 × 540 pt** canvas (16:9 widescreen aspect ratio).
- **EMU Conversion**: 1 point = 12,700 EMU (English Metric Units). Slide dimensions equal `12,192,000 × 6,858,000 EMU`.
- The WPF canvas is hosted inside a `Viewbox` with `Stretch="Uniform"`, enabling seamless, crisp scaling at any display resolution or window size.

---

## 4. Library Justification

### DocumentFormat.OpenXml (Open XML SDK)
- **Official standard**: Maintained by Microsoft and standardizes Office Open XML (ECMA-376 / ISO/IEC 29500).
- **Standalone execution**: Operates directly on the underlying ZIP and XML package structure without needing Microsoft Office or COM interop installed on the host machine.
- **Round-trip compatibility**: Presentations generated by `slidecraft` open natively in Microsoft PowerPoint (Desktop & Web), LibreOffice Impress, Google Slides, and Apple Keynote.
- **Permissive licensing**: MIT license permits free redistribution in academic and enterprise environments.

### CommunityToolkit.Mvvm
- Modern source generators generate boilerplate property change notifications (`INotifyPropertyChanged`) and command relays (`IRelayCommand`) at compile time.
- Ensures maintainable code, decoupling View code-behind from business operations.

---

## 5. Challenges Faced & Solutions

1. **Creating a compliant Open XML package**:
   *Challenge*: An empty `.pptx` file cannot simply consist of loose slide parts; PowerPoint strictly requires a `PresentationPart`, `SlideMasterPart`, `SlideLayoutPart`, and `ThemePart` with valid relationship IDs.
   *Solution*: Implemented an automatic template generator in `PptxExportService` that synthesizes the entire relationship graph with theme colors, font schemes, and default layout hierarchies.

2. **EMU ↔ WPF DIP coordinate mapping**:
   *Challenge*: Open XML uses integer EMUs for positions and extents (`a:off`, `a:ext`), while WPF relies on floating-point Device Independent Pixels (points).
   *Solution*: Encapsulated unit conversion inside a centralized `Emu` helper class (`PerPoint = 12700`).

3. **Polymorphic Serialization for Undo/Redo**:
   *Challenge*: Storing snapshots of `PresentationModel` using `System.Text.Json` failed because `SlideElement` is an abstract base class.
   *Solution*: Configured .NET 8 `[JsonDerivedType(typeof(TextElement), "text")]` and `[JsonDerivedType(typeof(ImageElement), "image")]` annotations on `SlideElement`, enabling full round-trip serializability for the Memento undo stack.

4. **Bi-directional event suppression during UI updates**:
   *Challenge*: Selecting a slide in the thumbnail list updated the transition and layout dropdowns, which in turn fired `SelectionChanged` events that mistakenly marked pristine documents as dirty.
   *Solution*: Implemented a `_suppressEvents` reentrancy guard in `MainWindow.xaml.cs` and `FormatBar.xaml.cs`.

5. **Graceful Handling of Unsupported Open XML Features**:
   *Challenge*: External `.pptx` decks frequently contain SmartArt, audio, video, charts, or complex group shapes not supported in simple editors.
   *Solution*: Designed `PptxImportService` with open-pattern matching that reads supported elements (shapes with text, pictures, transitions) while tallying skipped elements and notifying the user without throwing exceptions.

6. **Kannada and Complex Script Typography**:
   *Challenge*: Indic scripts like Kannada require proper glyph shaping, conjunct ligatures, and taller ascender/descender margins than standard Latin text.
   *Solution*: Standardized application fonts to `Nirmala UI, Segoe UI, Noto Sans Kannada`, avoided hardcoded button heights, and enabled `TextOptions.TextFormattingMode="Ideal"`.

---

## 6. Optional Enhancements

- **Slide Layouts**: Factory supporting Title, Title+Content, and Blank slides with localized placeholders.
- **Text Formatting Bar**: Bold and italic toggles, point-size selection, and 8-color visual palette.
- **Per-Slide Transitions**: Fade, Push, and Wipe animations rendered during slideshows and exported into Open XML markup.
- **Fullscreen Slideshow Mode**: Dedicated borderless presentation window with keyboard navigation (F5, arrows, Space, PageUp/Down, Home/End, Esc) and custom animation effects.
- **Memento Undo/Redo Engine**: Non-destructive stack enabling unlimited undo and redo across additions, deletions, repositioning, and formatting.
- **Title-Bar Dirty Indicator & Confirmation Prompts**: Asterisk `*` indicator on unsaved changes with safety prompts on New, Open, and Exit.

---

## 7. Automated Testing

The automated test suite (`RoundTripTests.cs`) verifies:
1. **Three-Slide Full Round-Trip**: Validates that text formatting (bold, italic, color, size), embedded PNG image bytes, element dimensions, and slide transitions survive save and reload within a 1.0 pt tolerance.
2. **Unsupported Shape Tolerance**: Verifies that foreign shapes (such as connectors or diagram frames) are skipped without crashing.
3. **Open XML Schema Validation**: Uses `OpenXmlValidator` to ensure exported `.pptx` documents contain 0 validation errors against the Microsoft Open XML specification.
4. **Empty Deck Edge Case**: Verifies safe round-trip handling of presentations containing empty slides.
5. **Multi-line Text Handling**: Validates paragraph splitting and re-assembly across newline characters.
6. **Undo/Redo State Invariance**: Confirms that state mutations can be undone and redone across slide and element collections.

---

## 8. Future Work

- **Vector Shapes**: Support for rectangles, ellipses, arrows, callouts, and connectors.
- **Tables & Charts**: Native tabular grid editing and chart rendering.
- **Presenter View & Notes**: Dual-monitor presentation mode with elapsed timer and per-slide speaker notes.
- **Media Playback**: Embedded video and audio element playback.
- **Export Options**: Exporting slides to PDF, high-resolution PNG images, or animated HTML.
