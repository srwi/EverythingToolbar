EverythingToolbar
=================

[![build](https://github.com/stnkl/EverythingToolbar/workflows/build/badge.svg)](https://github.com/stnkl/EverythingToolbar/actions)
[![Crowdin](https://badges.crowdin.net/everythingtoolbar/localized.svg)](https://crowdin.com/project/everythingtoolbar)
[![MIT license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/stnkl/EverythingToolbar/blob/master/LICENSE)
[![Downloads](https://img.shields.io/github/downloads/stnkl/EverythingToolbar/total?color=blue)](https://github.com/stnkl/EverythingToolbar/releases/latest)

[Everything](https://www.voidtools.com/) integration for the Windows taskbar.

Features
--------

- Instant search results using [Everything](https://www.voidtools.com/)
- Quick navigation via keyboard shortcuts
- Light and dark theme (or custom themes)
- Customizable *open with* commands
- Rules for opening files
- Uses filters defined within Everything

Demo
----

![demo](https://user-images.githubusercontent.com/17520641/102723553-04d88f00-4309-11eb-834f-d20c1ce14a67.gif)

Requirements
------------

- [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework) &ge; 4.7 ([how to check](https://user-images.githubusercontent.com/14129585/104021832-ff36e080-5206-11eb-9f5f-10e4381992f9.jpg))
- [Everything](https://www.voidtools.com/) &ge; 1.4.1 must be running (lite version not supported)
- High DPI support requires at least Windows 10 Creators Update.

Install
-------
- Make sure [Everything](https://www.voidtools.com) is installed and running
- Install EverythingToolbar using one of the following methods
  - Download the [EverythingToolbar](https://github.com/stnkl/EverythingToolbar/releases) installer
  - [Chocolatey](https://chocolatey.org/): `choco install everythingtoolbar`
  - [winget](https://github.com/microsoft/winget-cli/): `winget install everythingtoolbar`
- Enable EverythingToolbar via the context menu of the taskbar
  - *Note: You will have to open the context menu twice as it doesn't show up the first time you open it.*
- Adjust size and position after unlocking the taskbar ([demonstration video](https://user-images.githubusercontent.com/17520641/107118574-19a1bf80-6882-11eb-843a-7e854e5d0684.gif))

Build
-----

- Open solution in Visual Studio with .NET Framework 4.7 support
- Disable signing in project properties
- Build the project (Windows Explorer will restart)
- Install the toolbar by running `EverythingToolbar/tools/install.cmd` as admin

Keyboard shortcuts
------------------

| Shortcut                                              | Function                             |
|-------------------------------------------------------|--------------------------------------|
| <kbd>&#8593;</kbd>/<kbd>&#8595;</kbd>                 | Navigate search results              |
| <kbd>Ctrl</kbd>+<kbd>&#8593;</kbd>/<kbd>&#8595;</kbd> | Navigate search history (if enabled) |
| <kbd>Return</kbd>                                     | Open                                 |
| <kbd>Ctrl</kbd>+<kbd>Return</kbd>/<kbd>Click</kbd>    | Open path                            |
| <kbd>Shift</kbd>+<kbd>Return</kbd>/<kbd>Click</kbd>   | Open in Everything                   |
| <kbd>Alt</kbd>+<kbd>Return</kbd>/<kbd>Click</kbd>     | File properties                      |
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Enter</kbd>     | Run as admin                         |
| (<kbd>Shift</kbd>+)<kbd>Tab</kbd>                     | Cycle through filters                |
| <kbd>Ctrl</kbd>+<kbd>0-9</kbd>                        | Select filter                        |
| <kbd>Ctrl</kbd>+<kbd>Space</kbd>                      | Preview file in [QuickLook](https://github.com/QL-Win/QuickLook) |
| <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>S</kbd>            | Focus search box (customizable)      |

Rules
-----

Create custom *open with* commands by selecting <kbd>Rules...</kbd> from settings. By checking <kbd>Automatically apply rules based on condition</kbd> matching files/folders based on the type and regular expression field will execute the corresponding command when opened.

Examples:

| Name                     | Type | Regular Expression           | Command                 |
|--------------------------|------|------------------------------|-------------------------|
| Open terminal here...    | Any  |                              | `cmd /K "cd %path%"`    |
| Total Commander (Left)   | Any  |                              | `totalcmd /O /L=%path%` |
| Total Commander (Right)  | Any  |                              | `totalcmd /O /R=%path%` |
| MSPaint                  | File | `.*\\PixelArt\\.*(bmp\|BMP)` | `mspaint %file%`        |

Leaving the regular expression field empty will never match.

Customization
-------------

- Edit an existing theme/item template or create a new one in the `Themes` / `ItemTemplates` folder
- Restart Windows Explorer via the task manager
- Select theme/item template from settings

Contribute
----------

All kinds of contributions (questions, bug reports, pull requests) are welcome! Helping out with open issues is very much appreciated. As a basic rule, before filing issues, feature requests or anything else, take a look at the issues and check if it has already been reported by another user. If so, engage in the already existing discussion.

You can also help by [translating EverythingToolbar](https://crowdin.com/project/everythingtoolbar).
