# Contributing to EverythingToolbar

Thanks for your interest in improving EverythingToolbar. Contributions are welcome, and a little discussion up front helps make sure ideas fit the project and can be reviewed smoothly. If you are unsure whether a change fits the project, opening an issue first is the best place to start. That conversation can save time for both contributors and maintainers.

## Before you start

- For new features or larger behavior changes, please open or comment on an issue first so we can discuss the idea before implementation starts.
- For bug fixes, please check whether an issue already exists. If not, consider opening one with clear reproduction steps before submitting a fix, unless the fix is very small and obvious.
- Search existing open and closed issues and pull requests to avoid duplicate work.
- Keep pull requests focused on one change where possible. Separate unrelated fixes or refactors are easier to review as their own PRs.

## Pull request expectations

When opening a pull request, please try to:

- Link to the related issue, or explain briefly why there is no issue.
- Describe the problem and the chosen solution clearly.
- Include screenshots or recordings for visible UI changes.
- Note any manual testing you performed.
- Avoid unrelated formatting, cleanup, or refactoring where possible.
- Update documentation or translations when the change affects user-facing text or behavior.

AI tools are welcome as part of your workflow, but please review their output carefully and make sure you understand, test, and can explain the changes you submit.

## Building from Source

1. Open the solution in Visual Studio with .NET 8.0 support
2. Disable code signing in project properties
3. Choose your build target:
   - **Deskband**: Build `EverythingToolbar.Deskband` project, then run `/tools/install_deskband.cmd` as administrator
   - **Search icon**: Set `EverythingToolbar.Launcher` as startup project and start debugging
