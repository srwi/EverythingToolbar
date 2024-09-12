EverythingToolbar
=================

<a href="https://paypal.me/rumswinkel"><img src="https://img.shields.io/static/v1?label=Donate&message=%E2%9D%A4&logo=PayPal&color=%23009cde" height="25" /></a>
<a href="https://github.com/srwi/EverythingToolbar/releases/latest"><img src="https://img.shields.io/github/downloads/srwi/EverythingToolbar/total?color=critical" height="25" /></a>
<a href="https://crowdin.com/project/everythingtoolbar"><img src="https://badges.crowdin.net/everythingtoolbar/localized.svg" height="25" /></a>

Instant file search integration for the Windows taskbar powered by [Everything](https://www.voidtools.com/).

<img src="https://user-images.githubusercontent.com/17520641/213898038-c8f76cc4-572e-481c-82bf-e420900e1aff.gif">

## Features

### Light & Dark
EverythingToolbar seemlessly blends into Windows 10 and 11 and adjusts according to your theme settings.

<img src="https://user-images.githubusercontent.com/17520641/213913562-076e00f3-f54b-40b4-b6a5-ec705302fe39.png">

### Custom search filters
EverythingToolbar reads custom filters previously defined in Everything. To enable this feature, check the `Use Everything filters` option in the EverythingToolbar settings. From now on, all filters will be available from the filter dropdown:

![Custom search filters](https://user-images.githubusercontent.com/17520641/213913613-3621a0c1-0386-4d7a-ac0f-e7ab0239b222.png)

### Quick toggles for search options
Quick access to search flags (match case, whole word, match path and reg-ex) allows you to find your files precisely.

![Quick toggles](https://user-images.githubusercontent.com/17520641/213913757-da27d69d-59eb-445b-9d44-5b2e34c6faf4.png)

### RegEx-powered file associations
Create custom *Open with* commands by selecting <kbd>Rules...</kbd> in the preferences. By checking the <kbd>Automatically apply rules based on condition</kbd> checkbox, matching files/folders will execute the appropriate command based on the type and regular expression field. Leaving the regular expression field empty will never match, but the entry will only be displayed in the *Open with* context menu of the search results.

![Rules window](https://user-images.githubusercontent.com/17520641/213928743-a7f6a932-0b60-4dc3-8d2b-72ee09cf6e53.png)

### Compatibility
EverythingToolbar is compatible with both Windows 10 and Windows 11 and works well with tools like [ExplorerPatcher](https://github.com/valinet/ExplorerPatcher) and [StartAllBack](https://www.startallback.com/) to give you the full deskband integration even on Windows 11.

![Windows 10 screenshot](https://user-images.githubusercontent.com/17520641/213918399-a566c476-9b7e-460b-97c5-479964ddfa78.png)

### Other features:

- Open EverythingToolbar at any time via a customizable shortcut
- Redirect Start menu search to EverythingToolbar (experimental)
- Drag and drop files to where you need them most
- Quickly preview files thanks to [QuickLook](https://github.com/QL-Win/QuickLook)/[Seer](http://1218.io/) integration
- Use custom Everything [instance names](https://www.voidtools.com/support/everything/multiple_instances/#named_instances)

### Keyboard shortcuts

| Shortcut                                              | Function                             |
|-------------------------------------------------------|--------------------------------------|
| <kbd>&#8593;</kbd>/<kbd>&#8595;</kbd>                 | Navigate search results              |
| <kbd>Ctrl</kbd>+<kbd>&#8593;</kbd>/<kbd>&#8595;</kbd> | Navigate search history (if enabled) |
| <kbd>Enter</kbd>                                      | Open                                 |
| <kbd>Ctrl</kbd>+<kbd>Enter</kbd>/<kbd>Click</kbd>     | Open path                            |
| <kbd>Shift</kbd>+<kbd>Enter</kbd>/<kbd>Click</kbd>    | Open in Everything                   |
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd>         | Copy full path to clipboard          |
| <kbd>Alt</kbd>+<kbd>Enter</kbd>/<kbd>Click</kbd>      | File properties                      |
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Enter</kbd>     | Run as admin                         |
| (<kbd>Shift</kbd>+)<kbd>Tab</kbd>                     | Cycle through filters                |
| <kbd>Ctrl</kbd>+<kbd>0-9</kbd>                        | Select filter                        |
| <kbd>Space</kbd>                                      | Preview file in [QuickLook](https://github.com/QL-Win/QuickLook) |
| <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>S</kbd>            | Focus search box (customizable)      |

## Installation

- Make sure you are running Windows 10 or 11 and [Everything](https://www.voidtools.com) &ge; 1.4.1 is installed and running (the Lite version is not supported)
- Install EverythingToolbar using one of the following methods
  - Download the installer for [EverythingToolbar](https://github.com/srwi/EverythingToolbar/releases)
  - [Chocolatey](https://chocolatey.org/): `choco install everythingtoolbar`
  - [winget](https://github.com/microsoft/winget-cli/): `winget install stnkl.everythingtoolbar`
  - [Manual installation](https://github.com/srwi/EverythingToolbar/wiki/Installation-per-user-(experimental)) without admin privileges (not recommended)
- **Note:** For Everything 1.5a the instance name `1.5a` must be set in the EverythingToolbar settings ([screenshot](https://github.com/srwi/EverythingToolbar/assets/17520641/c8e6f9ad-9f33-4ad9-92c1-788b5f7f438a)).

## Setup

### Search icon

*Recommended for **unmodified Windows 11** installations*

- After installation on Windows 11 the setup assistant will guide you through the setup process
  > If the setup assistant did not start automatically, search for `EverythingToolbar` in the Windows Start menu.
  
  > If you want to use the search icon on Windows 10 (not recommended), search for `EverythingToolbar.Launcher.exe`, start it manually and follow the setup process.

### Deskband

*Recommended for **Windows 10** or in combination with [ExplorerPatcher](https://github.com/valinet/ExplorerPatcher)/[StartAllBack](https://www.startallback.com/)*

- After installation on Windows 10, activate EverythingToolbar from the taskbar context menu
  > You will need to open the context menu twice, as EverythingToolbar will not appear the first time.
  
  > **Windows 11 only**: After installation, the search icon setup assistant will start automatically. If you want to use the deskband instead (only recommended in combination with ExplorerPatcher/StartAllback), close the assistant and end EverythingToolbar's background process via the taskbar tray icon.
- Adjust size and position after unlocking the taskbar ([Demonstration video](https://user-images.githubusercontent.com/17520641/107118574-19a1bf80-6882-11eb-843a-7e854e5d0684.gif))

## Build

- Open the solution in Visual Studio with .NET Framework 4.7 support
- Disable signing in project properties
- Deskband:
  - Build project `EverythingToolbar.Deskband` (Windows Explorer will be restarted) 
  - Install the toolbar deskband by running `/tools/install_deskband.cmd` as admin
- Search icon:
  - Set `EverythingToolbar.Launcher` as startup project and start debugging

## Contribute

All kinds of contributions (questions, bug reports, pull requests) are welcome! Helping with open issues is greatly appreciated. As a basic rule, before filing issues, feature requests or anything else, take a look at the issues and check if they have already been reported by another user. If so, engage in the already existing discussion.

You can also help by [translating EverythingToolbar](https://crowdin.com/project/everythingtoolbar).
