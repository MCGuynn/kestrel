# Kestrel

A fast, tabbed Windows text and code editor. Clean-room implementation: Notepad++ (GPL v3) was used only as a feature reference. No Notepad++ code, name, or artwork is included.

## Requirements

- Windows 10/11
- .NET 8 SDK (`dotnet --version` shows 8.x or later)

## Build and run

```powershell
cd C:/Users/mguynn/dev/apps/kestrel
dotnet new sln -n Kestrel
dotnet sln add src/Kestrel.Core src/Kestrel.App tests/Kestrel.Tests
dotnet restore
dotnet test
dotnet run --project src/Kestrel.App
```

Open files from the command line: `dotnet run --project src/Kestrel.App -- C:/path/file.sql`

After the first restore, pin the editor package: `dotnet list src/Kestrel.App package`, then replace `Version="*"` in `Kestrel.App.csproj` with the resolved version.

## Where state lives

Window size, theme, wrap, zoom, and open files are saved to `%AppData%/Kestrel/state.json` on exit. A corrupt file is renamed to `state.json.bad` and defaults are used. Under MSIX, Windows redirects this path into the package's private AppData automatically.

## Features (v1)

- Tabs, middle-click to close, drag files onto the tab strip or title bar
- Syntax highlighting and folding: SQL (PostgreSQL keywords), JavaScript, TypeScript, Python, C#, Java, HTML, CSS, JSON, XML (incl. .pcf, .xsl), Markdown
- Find/replace with match case, whole word, regex (ECMAScript), wrap, count, replace all
- Encoding detection (UTF-8, UTF-8 BOM, UTF-16 LE/BE BOM, Windows-1252), convert or reopen as another encoding, lossy-save warning
- Line-ending detection and conversion (CR LF, LF, CR)
- Line operations: duplicate, delete, move, sort, dedupe, remove empty, trim; upper/lower case
- Multi-cursor (Ctrl+click) and column select (Alt+drag)
- Dark and light themes, word wrap, show whitespace, zoom
- Session restore, atomic saves, 256 MB file-size guard

## Shortcuts

Ctrl+N/O/S new/open/save, Ctrl+Alt+S save as, Ctrl+Shift+S save all, Ctrl+W close, Ctrl+F find, Ctrl+H replace, F3/Shift+F3 next/previous, Ctrl+G go to line, Ctrl+D duplicate line, Ctrl+Shift+L delete line, Ctrl+Shift+Up/Down move line, Ctrl+U/Ctrl+Shift+U lower/upper case, Ctrl++/-/0 zoom.

## Packaging for the Microsoft Store (MSIX)

1. In Visual Studio 2022, add a **Windows Application Packaging Project** to the solution.
2. Reference `Kestrel.App` from it and set it as the entry point.
3. Add your Store logo assets in the packaging project's `Images` folder.
4. Project > Publish > Associate App with the Store, then Create App Packages.

## Known limitations (v1)

- Menus and dialogs use the system theme; only the editor follows dark/light (.NET 8 WinForms has no app-wide dark mode).
- Unsaved untitled tabs are not restored; you are prompted to save on exit.
- Files dropped directly onto the text area are not opened (drop on the tab strip or title bar).
- UTF-32 files are not detected (a UTF-32 LE BOM reads as UTF-16 LE).
- No app icon yet; the default WinForms icon is used.

## License

Choose before publishing. Because no GPL code is included, a permissive license (MIT) or proprietary license is possible. ScintillaNET and Scintilla/Lexilla are under their own permissive licenses; include their notices in the About box or a NOTICES file.
