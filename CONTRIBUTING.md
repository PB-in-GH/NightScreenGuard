# 贡献指南 / Contributing

## 中文

第一作者 / 原开发者为 [PB-in-GH](https://github.com/PB-in-GH)。贡献须同意以本项目的 PolyForm Noncommercial 1.0.0 许可发布，并保留现有作者及许可声明；贡献者保留自己贡献的著作权。

1. 在 Windows x64 本地桌面开发，运行 `build.ps1` 和 `test.ps1 -UI`。
2. 翻译集中在 `src/NightScreenGuard.cs` 的 `L10n` 类中。新增面向用户的文字时同时补上中文和英文，并查看两种语言的界面截图。
3. 保持手动启动，不接管系统月亮键、自动息屏或电源方案；保留托盘左键单击打开。
4. UI 测试是模拟关屏。修改输入或电源行为后，需要有人在真实显示器上测试，记录实际看到的结果，不把 Windows 状态当成物理显示证明。
5. 提交前检查变更，不提交 `language.txt`、日志、个人路径、截图中的个人信息或 `dist/`。

项目使用系统自带的 .NET Framework 编译器；请保持与其 C# 版本兼容。运行 `scripts/build-icon.ps1` 可从原创绘图源码重建图标。

`--lang=zh` / `--lang=en` 可覆盖本次启动语言；通过窗口语言按钮选择才会保存。开发用 `--self-test <输出文件>` 与 `--ui-test <输出目录>` 不会关屏。另有 `--smoke-test <输出目录>`，**会真实息屏**并尝试自动恢复；仅在本地有人可以移动鼠标、按键时手动使用，不用于无人值守测试。

## English

The original author and developer is [PB-in-GH](https://github.com/PB-in-GH). Contributions must be offered under this project's PolyForm Noncommercial 1.0.0 license, preserving existing attribution and license notices. Contributors retain copyright in their own contributions.

1. Develop on a local Windows x64 desktop. Run `build.ps1` and `test.ps1 -UI`.
2. Translations live in the `L10n` class in `src/NightScreenGuard.cs`. Add both languages for user-visible text and inspect both UI previews.
3. Preserve manual start, independent Windows moon-key behavior, unmodified power plans and single-left-click tray opening.
4. UI tests simulate power-off. Changes to power or input behavior require a human to check a real monitor. Report physical observations separately from Windows display-state events.
5. Review changes before committing. Exclude preferences, logs, personal paths or screenshot data, and `dist/`.

Use syntax compatible with the bundled .NET Framework compiler. Run `scripts/build-icon.ps1` to regenerate the icon from its original drawing source.

`--lang=zh` / `--lang=en` override language for one launch; choosing from the UI language buttons saves it. Developer commands `--self-test <output-file>` and `--ui-test <output-directory>` do not turn displays off. `--smoke-test <output-directory>` **does turn displays off** and attempts recovery; use it manually with someone present who can move the mouse or press a key. It is not an unattended test.
