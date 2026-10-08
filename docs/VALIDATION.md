# 验证说明 / Validation

## 中文

2026-10-08，本版本在一台 Windows 11 x64 电脑上完成：

- 使用仓库 `build.ps1` 从源码成功编译，无依赖下载。
- 16 项自动检查通过，覆盖原有守护状态逻辑、两种语言的文本、倒计时以及语言偏好保存和无效配置回退。
- 实际创建窗口，通过语言按钮分别切换中文和英文，检查按钮、标题、托盘菜单以及结束状态文本；查看两种语言的渲染截图。
- UI 测试没有执行物理关屏，也跳过全局快捷键注册以避免与正在运行的程序冲突。测试报告中 `hotkeys_skipped_for_ui_test=True` 表示没有验证真实快捷键注册。

此前中文版本的 Windows 事件曾显示“息屏—测试亮屏—再次息屏—请求恢复”。但用户观察到外接显示器并未随恢复请求实际点亮，直到鼠标输入才亮。因此不能以 API 返回或系统事件承诺物理亮屏成功。

本次增加语言和整理目录，没有重新做整夜、热插拔、锁屏、远程会话、各类缩放比例或多显示器的物理测试。这些仍需要实际设备验证。仓库图片由程序自身渲染，不含桌面信息。

## English

On 2026-10-08, this version was checked on one Windows 11 x64 computer:

- Successful source compilation using the repository's `build.ps1`, without dependency downloads.
- 16 automated checks passed, covering the existing guard state logic, bilingual messages, countdown formatting, saved language preferences and invalid-preference fallback.
- A real window was created; its language buttons switched between Chinese and English. Titles, buttons, tray menu and stopped-state text were checked. Both rendered previews were visually inspected.
- UI tests did not physically power off displays. Global shortcut registration was skipped to avoid interfering with an existing instance. `hotkeys_skipped_for_ui_test=True` means real shortcut registration was not verified by that test.

Earlier testing of the Chinese version received Windows display events for off, test-on, off again and a restore request. However, the user observed that an external monitor did not visibly turn on until mouse input occurred. API returns or system events therefore do not prove physical display recovery.

This language and packaging update did not repeat overnight, hot-plug, secure-desktop, remote-session, all-DPI or multi-monitor hardware testing. Those remain device-dependent. Repository previews are rendered from the app's own window and contain no desktop content.
