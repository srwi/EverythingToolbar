EverythingToolbar
=================
[![build](https://github.com/stnkl/EverythingToolbar/workflows/build/badge.svg)](https://github.com/stnkl/EverythingToolbar/actions)
[![Crowdin](https://badges.crowdin.net/everythingtoolbar/localized.svg)](https://crowdin.com/project/everythingtoolbar)
[![MIT license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/stnkl/EverythingToolbar/blob/master/LICENSE)
[![Downloads](https://img.shields.io/github/downloads/stnkl/EverythingToolbar/total?color=blue)](https://github.com/stnkl/EverythingToolbar/releases/latest)

Instant file search integration for the Windows taskbar powered by [Everything](https://www.voidtools.com/).

<img src="https://user-images.githubusercontent.com/17520641/213898038-c8f76cc4-572e-481c-82bf-e420900e1aff.gif">

## Features

### Light & Dark
EverythingToolbar seemlessly blends into Windows 10 and 11 and adjusts according to your theme settings.

<img src="https://user-images.githubusercontent.com/17520641/213913562-076e00f3-f54b-40b4-b6a5-ec705302fe39.png">

### Custom search filters
EverythingToolbar reads custom filters that have been previously defined in Everything. To enable the feature, check `Use Everything filters` via the settings in EverythingToolbar. From now on all filters will be available from the filter dropdown: 

![Custom search filters](https://user-images.githubusercontent.com/17520641/213913613-3621a0c1-0386-4d7a-ac0f-e7ab0239b222.png)

### Quick toggles for search options
Quick access to search flags (match case, whole word, match path and reg-ex) allows you to find your files precisely.

![Quick toggles](https://user-images.githubusercontent.com/17520641/213913757-da27d69d-59eb-445b-9d44-5b2e34c6faf4.png)

### RegEx-powered file associations
Create custom *open with* commands by selecting <kbd>Rules...</kbd> from settings. By checking <kbd>Automatically apply rules based on condition</kbd> matching files/folders based on the type and regular expression field will execute the corresponding command when opened. Leaving the regular expression field empty will never match but will only show the entry in the *open with* context menu of search results. 

![Rules window](https://user-images.githubusercontent.com/17520641/213928743-a7f6a932-0b60-4dc3-8d2b-72ee09cf6e53.png)

### Compatibility
EverythingToolbar is compatible with both Windows 10 and 11 and works well with tools like [ExplorerPatcher](https://github.com/valinet/ExplorerPatcher) and [StartAllBack](https://www.startallback.com/) to give you the full deskband integration even on Windows 11.

![Windows 10 screenshot](https://user-images.githubusercontent.com/17520641/213918399-a566c476-9b7e-460b-97c5-479964ddfa78.png)

### Other features:

- Open EverythingToolbar at any time via a customizable shortcut
- Drag & drop files to wherever you need them most
- Quickly preview files thanks to [QuickLook](https://github.com/QL-Win/QuickLook) integration
- Use custom Everything [instance names](https://www.voidtools.com/support/everything/multiple_instances/#named_instances)

### Keyboard shortcuts

| Shortcut                                              | Function                             |
|-------------------------------------------------------|--------------------------------------|
| <kbd>&#8593;</kbd>/<kbd>&#8595;</kbd>                 | Navigate search results              |
| <kbd>Ctrl</kbd>+<kbd>&#8593;</kbd>/<kbd>&#8595;</kbd> | Navigate search history (if enabled) |
| <kbd>Enter</kbd>                                      | Open                                 |
| <kbd>Ctrl</kbd>+<kbd>Enter</kbd>/<kbd>Click</kbd>     | Open path                            |
| <kbd>Shift</kbd>+<kbd>Enter</kbd>/<kbd>Click</kbd>    | Open in Everything                   |
| <kbd>Alt</kbd>+<kbd>Enter</kbd>/<kbd>Click</kbd>      | File properties                      |
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Enter</kbd>     | Run as admin                         |
| (<kbd>Shift</kbd>+)<kbd>Tab</kbd>                     | Cycle through filters                |
| <kbd>Ctrl</kbd>+<kbd>0-9</kbd>                        | Select filter                        |
| <kbd>Ctrl</kbd>+<kbd>Space</kbd>                      | Preview file in [QuickLook](https://github.com/QL-Win/QuickLook) |
| <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>S</kbd>            | Focus search box (customizable)      |

## Installation

- Make sure [Everything](https://www.voidtools.com) &ge; 1.4.1 is installed and running (lite version is not supported)
- Install EverythingToolbar using one of the following methods
  - Download the [EverythingToolbar](https://github.com/stnkl/EverythingToolbar/releases) installer
  - [Chocolatey](https://chocolatey.org/): `choco install everythingtoolbar`
  - [winget](https://github.com/microsoft/winget-cli/): `winget install everythingtoolbar`
  - [Manual installation](https://github.com/stnkl/EverythingToolbar/wiki/Installation-per-user-(experimental)) without admin rights (not recommended)
- **Note:** For Everything 1.5a the instance name `1.5a` has to be set in EverythingToolbar settings.

## Setup

### Search icon

*Recommended for **unmodified Windows 11** installations*

- After installation on Windows 11 the setup assistant will guide you through the setup process
  - Note: If you want to use the search icon on Windows 10 (not recommended) or the setup assistant didn't start automatically, just search for `EverythingToolbar` in the Windowws Start menu.

### Deskband

*Recommended for **Windows 10** or in combination with [ExplorerPatcher](https://github.com/valinet/ExplorerPatcher)/[StartAllBack](https://www.startallback.com/)*

- After installation on Windows 10 enable EverythingToolbar via the context menu of the taskbar
  - **Note A:** You will have to open the context menu twice as it doesn't show up the first time you open it.
  - **Note B (*Windows 11 only*):** After installation the search icon setup assistant will start automatically. If you want to use the deskband instead (only recommended in combination with ExplorerPatcher/StartAllback) just close the assistant and quit the background process of EverythingToolbar via the taskbar tray icon.
- Adjust size and position after unlocking the taskbar ([demonstration video](https://user-images.githubusercontent.com/17520641/107118574-19a1bf80-6882-11eb-843a-7e854e5d0684.gif))

## Build

- Open solution in Visual Studio with .NET Framework 4.7 support
- Disable signing in project properties
- Deskband:
  - Build project `EverythingToolbar.Deskband` (Windows explorer will restart) 
  - Install the toolbar deskband by running `EverythingToolbar.Deskband/tools/install.cmd` as admin
- Search icon:
  - Set `EverythingToolbar.Launcher` as startup project and start debugging

## Contribute

All kinds of contributions (questions, bug reports, pull requests) are welcome! Helping out with open issues is very much appreciated. As a basic rule, before filing issues, feature requests or anything else, take a look at the issues and check if it has already been reported by another user. If so, engage in the already existing discussion.

You can also help by [translating EverythingToolbar](https://crowdin.com/project/everythingtoolbar).
