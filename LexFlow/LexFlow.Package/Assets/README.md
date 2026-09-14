# LexFlow Assets

This directory contains the application icons and images for Microsoft Store packaging.

## Required Assets

Please replace the placeholder files with actual icons:

### Store Icons
- **StoreLogo.png** (50x50) - Microsoft Store listing icon
- **Square150x150Logo.png** (150x150) - Start menu tile
- **Square44x44Logo.png** (44x44) - Taskbar icon
- **SmallTile.png** (71x71) - Small tile
- **LargeTile.png** (310x310) - Large tile
- **Wide310x150Logo.png** (310x150) - Wide tile
- **LockScreen.png** (620x300) - Lock screen image
- **SplashScreen.png** (620x300) - Splash screen

### Application Icon
- **LexFlow.ico** - Main application icon (256x256 with 16x16, 32x32, 48x48, 64x64, 128x128, 256x256)

## Design Guidelines

### Icon Style
- Modern, clean design
- Use the "L" letter or abbreviation for LexFlow
- Blue/indigo color scheme (#1E88E5 or similar)
- White text on colored background
- Consistent with Windows 11 design language

### Color Palette
- Primary: #1E88E5 (Blue)
- Secondary: #0D47A1 (Dark Blue)
- Accent: #FFC107 (Amber) for highlights
- Background: Transparent for store icons

### File Formats
- PNG for all Windows Store assets
- ICO for desktop application icon
- Use PNG with transparency where appropriate

## Generation Tools

### Recommended Tools
- **Adobe Illustrator** - Professional vector graphics
- **Figma** - Free online design tool
- **Paint.NET** - Free raster graphics editor
- **IcoFX** - For creating .ico files
- **ImageMagick** - Command-line image conversion

### Quick Generation
If you don't have design tools, you can:
1. Use an online icon generator
2. Use a template from Microsoft's icon gallery
3. Create a simple text-based icon using web tools

## Size Specifications

### Store Icons
- 50x50 - Store listing
- 44x44 - Taskbar
- 71x71 - Small tile
- 150x150 - Medium tile
- 310x150 - Wide tile
- 310x310 - Large tile
- 620x300 - Lock screen / splash screen

### Application Icon
- 16x16 - Small taskbar
- 32x32 - Medium taskbar
- 48x48 - Large taskbar
- 64x64 - Extra large
- 128x128 - Desktop shortcut
- 256x256 - High DPI displays

## Placeholder Files

Current files are text placeholders. Replace them with actual image files before building the Microsoft Store package.

## Verification

After adding icons:
1. Build the package: `dotnet build LexFlow.Package`
2. Check generated .appx file
3. Test in Windows App Certification Kit
4. Verify icons appear correctly in Store preview