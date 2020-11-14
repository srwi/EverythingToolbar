EverythingToolbar
=================

[Everything](https://www.voidtools.com/) integration for the Windows taskbar

![demo](https://user-images.githubusercontent.com/17520641/94998321-63675a00-05b1-11eb-9331-ef1fd744329e.gif)

Requirements
------------

- .NET Framework &ge; 4.7
- Everything &ge; 1.4.1 must be running (Lite Version not supported)
- High DPI support requires at least Windows 10 Creators Update.

Install
-------

- Download from [releases](https://github.com/stnkl/EverythingToolbar/releases)
- Extract it somewhere safe
- Run the `install.cmd` as admin
- Enable EverythingToolbar via the context menu of the taskbar
  - *Note: You might have to open the context menu twice as it not always shows up instantly.*

Uninstall
---------

- Run the `uninstall.cmd` as admin

Build
-----

- Open solution in Visual Studio 2019 with .NET Framework 4.7 support
- Disable signing in project properties
- Build the project (Windows Explorer will restart)
- Install the toolbar by running `EverythingToolbar/bin/<Configuration>/install.cmd` as admin

Customization
-------------

- Edit an existing theme/item template or create a new one in the `Themes` / `ItemTemplates` folder
- Restart Windows Explorer via the task manager
- Select theme/item template via the context menu of the search box

Rules
-----

Create custom "open with" commands by selecting `Rules...` from the search box context menu. By checking `Automatically apply rules based on condition` matching files/folders based on the `Type` and `Regular Expression` field will execute the corresponding command when opened.

Examples:

| Name                     | Type | Regular Expression           | Command                 |
|--------------------------|------|------------------------------|-------------------------|
| Open terminal here...    | Any  |                              | `cmd /K "cd %path%"`    |
| Total Commander (Left)   | Any  |                              | `totalcmd /O /L=%path%` |
| Total Commander (Right)  | Any  |                              | `totalcmd /O /R=%path%` |
| MSPaint                  | File | `.*\\PixelArt\\.*(bmp\|BMP)` | `mspaint %file%`        |

Leaving the regular expression field empty will never match.
