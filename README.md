EverythingToolbar
=================

[![build](https://github.com/stnkl/EverythingToolbar/workflows/build/badge.svg)](https://github.com/stnkl/EverythingToolbar/actions)
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

- .NET Framework &ge; 4.7
- Everything &ge; 1.4.1 must be running (lite version not supported)
- High DPI support requires at least Windows 10 Creators Update.

Install
-------
- Download from [releases](https://github.com/stnkl/EverythingToolbar/releases)
- Extract to a safe place
- Run `install.cmd` as admin
- Enable EverythingToolbar via the context menu of the taskbar
  - *Note: You might have to open the context menu twice as it not always shows up instantly.*
- Adjust size and position after unlocking the taskbar

<details>

<summary>Installation video</summary>

![video](https://user-images.githubusercontent.com/17520641/102831521-4ee67100-43ec-11eb-8804-42dda8affba1.gif)

</details>

Uninstall
---------

- Run `uninstall.cmd` as admin

Build
-----

- Open solution in Visual Studio with .NET Framework 4.7 support
- Disable signing in project properties
- Build the project (Windows Explorer will restart)
- Install the toolbar by running `EverythingToolbar/bin/<Configuration>/install.cmd` as admin

Keyboard shortcuts
------------------

| Shortcut                                       | Function                         |
|------------------------------------------------|----------------------------------|
| <kbd>&#8593;</kbd> <kbd>&#8595;</kbd>          | Navigate search results          |
| <kbd>Return</kbd>                              | Open                             |
| <kbd>Shift</kbd>+<kbd>Return</kbd>             | Open in Everything               |
| <kbd>Tab</kbd>/<kbd>Shift</kbd>+<kbd>Tab</kbd> | Select filter                    |
| <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>S</kbd>     | Focus search box (customizable)  |

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

Helping out with open issues, especially those marked as "help wanted", is appreciated!

You can also help by translating EverythingToolbar. To do so download and translate the [resources](https://github.com/stnkl/EverythingToolbar/blob/master/EverythingToolbar/Properties/Resources.resx) file and either attach it to [this issue](https://github.com/stnkl/EverythingToolbar/issues/64) or open a pull request.
