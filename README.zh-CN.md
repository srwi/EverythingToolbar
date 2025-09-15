**其他语言版本: [English](README.md), [中文](README.zh-CN.md).**

# <img src="EverythingToolbar/Images/AppIcon.ico" width="24"> EverythingToolbar

<a href="https://paypal.me/rumswinkel"><img src="https://img.shields.io/static/v1?label=Donate&message=%E2%9D%A4&logo=PayPal&color=%23009cde" height="25" /></a>
<a href="https://ko-fi.com/stephanrwi"><img src="https://img.shields.io/static/v1?label=Buy%20me%20a%20coffee&message=%E2%98%95&logo=Ko-fi&color=%23FF5E5B" height="25" /></a>
<a href="https://github.com/srwi/EverythingToolbar/releases/latest"><img src="https://img.shields.io/github/downloads/srwi/EverythingToolbar/total?color=critical" height="25" /></a>
<a href="https://crowdin.com/project/everythingtoolbar"><img src="https://badges.crowdin.net/everythingtoolbar/localized.svg" height="25" /></a>

为Windows任务栏提供即时文件搜索集成，由[Everything](https://www.voidtools.com/)提供支持。

<img src="https://raw.githubusercontent.com/srwi/EverythingToolbar/develop/.github/images/demo.gif" alt="EverythingToolbar演示" width="80%">

## 🌟 功能特点

### 明亮和暗黑主题
EverythingToolbar无缝融入Windows 10和11，自动适应您的系统主题。

<img src="https://raw.githubusercontent.com/srwi/EverythingToolbar/develop/.github/images/fast.png" alt="明亮和暗黑主题" width="80%">

### 自定义搜索过滤器
EverythingToolbar内置常用搜索过滤器。对于高级用户，它可以与Everything的自定义过滤器同步，让您完全控制搜索体验。通过在EverythingToolbar的**过滤器**设置中勾选**使用Everything过滤器**来启用此功能。

<img src="https://raw.githubusercontent.com/srwi/EverythingToolbar/develop/.github/images/flexible.png" alt="自定义搜索过滤器" width="80%">

### 快速搜索选项
轻松切换搜索标志（区分大小写、全字匹配、匹配路径和正则表达式）以精确查找文件。

<img src="https://raw.githubusercontent.com/srwi/EverythingToolbar/develop/.github/images/on_point.png" alt="快速切换" width="80%">

### 基于正则表达式的文件操作
通过导航到**设置** → **自定义操作**创建自定义的**打开方式**操作。启用**根据条件自动应用规则**可以基于文件类型和正则表达式自动执行命令。将正则表达式字段留空，则该操作仅在上下文菜单中显示。

<img src="https://raw.githubusercontent.com/srwi/EverythingToolbar/develop/.github/images/custom_actions.png" alt="自定义操作选项" width="80%">

### 兼容性
EverythingToolbar在Windows 10和Windows 11上无缝运行，并与[ExplorerPatcher](https://github.com/valinet/ExplorerPatcher)和[StartAllBack](https://www.startallback.com/)等工具完美集成，在Windows 11上提供完整的桌面栏支持。

<img src="https://raw.githubusercontent.com/srwi/EverythingToolbar/develop/.github/images/compatible.png" alt="Windows 10截图" width="80%">

### 其他功能

- 通过可自定义的全局快捷键即时访问EverythingToolbar
- 直接从搜索结果拖放文件
- 通过[QuickLook](https://github.com/QL-Win/QuickLook)或[Seer](http://1218.io/)集成预览文件
- 支持自定义Everything[实例名称](https://www.voidtools.com/support/everything/multiple_instances/#named_instances)

### 键盘快捷键

| 快捷键                                                           | 功能                                 |
|--------------------------------------------------------------------|------------------------------------------|
| <kbd>↑</kbd> / <kbd>↓</kbd>                                        | 导航搜索结果                  |
| <kbd>Ctrl</kbd> + <kbd>↑</kbd> / <kbd>↓</kbd>                     | 导航搜索历史（如果启用）    |
| <kbd>Enter</kbd> / <kbd>点击</kbd>                                | 打开文件或文件夹                     |
| <kbd>Ctrl</kbd> + <kbd>Enter</kbd> / <kbd>点击</kbd>             | 打开所在文件夹                   |
| <kbd>Shift</kbd> + <kbd>Enter</kbd> / <kbd>点击</kbd>            | 在Everything中打开                      |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>C</kbd>                 | 复制完整路径到剪贴板             |
| <kbd>Alt</kbd> + <kbd>Enter</kbd> / <kbd>点击</kbd>              | 显示文件属性                     |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>Enter</kbd> / <kbd>点击</kbd> | 以管理员身份运行                     |
| <kbd>Shift</kbd> + <kbd>右键点击</kbd>                         | 打开系统上下文菜单                |
| (<kbd>Shift</kbd> +) <kbd>Tab</kbd>                               | 循环切换过滤器                    |
| <kbd>Ctrl</kbd> + <kbd>0-9</kbd>                                  | 按数字选择过滤器                  |
| <kbd>Space</kbd>                                                   | 在QuickLook中预览文件                |
| <kbd>Win</kbd> + <kbd>Alt</kbd> + <kbd>S</kbd>                    | 聚焦搜索框（可自定义）         |

## 📦 安装

### 前提条件
- Windows 10或Windows 11
- [Everything](https://www.voidtools.com) ≥ 1.4.1已安装并运行
  > **注意：** 不支持Everything的Lite版本

### 安装EverythingToolbar
选择以下安装方法之一：

- **安装程序**：从[GitHub Releases](https://github.com/srwi/EverythingToolbar/releases)下载
- **winget**：`winget install srwi.everythingtoolbar.launcher`或`winget install srwi.everythingtoolbar.deskband`

> **重要提示：** 对于Everything 1.5a用户，请在EverythingToolbar的高级设置中将实例名称设置为`1.5a`（**设置** → **高级** → **Everything实例名称**）。([截图](https://raw.githubusercontent.com/srwi/EverythingToolbar/develop/.github/images/named_instance.png))

## ⚙️ 设置

### 启动器（任务栏图标）
*推荐用于未修改的Windows 11安装*

在Windows 11上安装后，设置助手将指导您完成配置过程。

> 如果设置助手没有自动启动，请在Windows开始菜单中搜索**EverythingToolbar**。

> 对于偏好搜索图标的Windows 10用户（不推荐），请搜索**EverythingToolbar.Launcher.exe**，手动启动并按照设置过程操作。

### 桌面栏
*推荐用于Windows 10或使用[ExplorerPatcher](https://github.com/valinet/ExplorerPatcher) / [StartAllBack](https://www.startallback.com/)时*

在Windows 10上安装后：
1. 右键点击任务栏，从_工具栏_上下文菜单中选择EverythingToolbar
   > 您可能需要打开上下文菜单两次才能显示EverythingToolbar
2. 解锁任务栏以调整大小和位置（[演示视频](https://raw.githubusercontent.com/srwi/EverythingToolbar/develop/.github/images/deskband_resizing_demo.gif)）

> **Windows 11用户**：安装后搜索图标设置助手会自动启动。如果要使用桌面栏（仅在使用ExplorerPatcher/StartAllBack时推荐），请关闭助手并通过系统托盘结束EverythingToolbar的后台进程。

-----------------------------

有关故障排除和其他设置帮助，请参阅[常见问题解答](FAQ.md)。

## 🛠️ 开发

### 从源代码构建
1. 在支持.NET 8.0的Visual Studio中打开解决方案
2. 在项目属性中禁用代码签名
3. 选择您的构建目标：
   - **桌面栏**：构建`EverythingToolbar.Deskband`项目，然后以管理员身份运行`/tools/install_deskband.cmd`
   - **搜索图标**：将`EverythingToolbar.Launcher`设置为启动项目并开始调试

## 🤝 贡献

欢迎各种类型的贡献！无论是报告错误、请求功能还是提交拉取请求，您的帮助都将受到赞赏。

### 如何贡献
- 创建新问题前请检查现有[问题](https://github.com/srwi/EverythingToolbar/issues)
- 帮助解决开放的问题和讨论
- [将EverythingToolbar翻译](https://crowdin.com/project/everythingtoolbar)成您的语言

## 💖 支持

如果您喜欢EverythingToolbar，我将非常感谢您的支持！
您可以通过在GitHub上给予星标或进行捐赠来表达您的感谢：

- **[给我买杯咖啡](https://ko-fi.com/stephanrwi)**
- **[通过PayPal捐赠](https://paypal.me/rumswinkel)**

## ❓ 常见问题解答

有关常见问题、故障排除指南和详细配置说明，请访问[常见问题解答](https://github.com/srwi/EverythingToolbar/blob/develop/FAQ.md)。