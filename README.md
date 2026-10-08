# 夜间息屏守护 · Night Screen Guard

[中文](README.md) | [English](README.en.md)

[![Windows build and tests](https://github.com/PB-in-GH/NightScreenGuard/actions/workflows/build.yml/badge.svg)](https://github.com/PB-in-GH/NightScreenGuard/actions/workflows/build.yml)

**[下载 Windows 便携版](https://github.com/PB-in-GH/NightScreenGuard/releases/latest) · [反馈问题](https://github.com/PB-in-GH/NightScreenGuard/issues)**

让电脑继续运行，同时尽量保持屏幕关闭。适合夜间下载、计算或运行后台任务。

这是一个轻量的 Windows 桌面小工具：你主动开始守护后，它会覆盖黑色窗口、请求关闭显示器，并在屏幕意外亮起时再次请求关屏。操作键鼠即可结束守护。

> 它不能保证零闪亮，也不是锁屏或隐私隔离工具。显示器背光、驱动和 Windows 安全界面可能不受它控制。

![中文界面](docs/screenshot-zh.png)

## 功能

- **中文 / English** 即时切换：窗口、状态、提示、托盘菜单都随语言切换。
- 记住语言选择；首次运行根据 Windows 界面语言选择中文或英文。
- 手动开始，五秒倒计时；不与系统月亮键或自动息屏联动。
- 黑色遮罩、屏幕状态通知与定时补充关屏。
- 键盘、鼠标点击或适量移动结束守护；可关闭移动唤亮。
- 月亮星星托盘图标，左键单击打开，右键显示菜单。
- 守护期间临时阻止自动睡眠，不更改原有电源方案。
- 便携运行，无管理员权限、联网、遥测或第三方运行库下载需求。

## 运行

支持目标：Windows 10/11 **x64**，已启用 **.NET Framework 4.8**。本版本已在一台 Windows 11 x64 电脑上验证；并未覆盖所有系统、显示器和输入设备。非 Windows、ARM64 原生版本尚不支持。

若拿到打包好的便携版本，解压后双击 `NightScreenGuard.exe`。若从仓库下载的是源码，请先按下文构建；源码目录本身不包含可执行文件。

1. 保持显示器电源开启，确认显示模式正确。
2. 选择右上角的 **中文 / English**。
3. 点击 **开始守护 · 5 秒**，松开键鼠。
4. 恢复时按普通键、点击或移动鼠标。建议用 Shift 或轻移鼠标，因为唤亮输入不保证被吞掉。

守护开始后有约 0.7 秒输入宽限，鼠标有轻微抖动过滤。关闭窗口会收起到托盘；完全退出请点击“退出”或使用托盘菜单。程序不会自动开始，也没有定时自动结束。

| 操作 | 效果 |
| --- | --- |
| Ctrl + Alt + F12 | 开始五秒倒计时，或取消守护 |
| Ctrl + Alt + End | 停止守护并打开窗口 |
| Esc（倒计时期间） | 取消倒计时 |
| 托盘图标左键单击 | 打开窗口 |
| 托盘图标右键 | 显示开始、停止和退出菜单 |

一些键盘的月亮键不是普通 F12；快捷键不可用时直接用窗口按钮。月亮键原本的 Windows 行为不会被重新绑定。程序也不会阻止手动睡眠、合盖、休眠或关机。

## 从源码构建

在源码目录打开 Windows PowerShell：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

脚本使用 Windows 的 .NET Framework C# 编译器，不下载依赖。可执行文件和随附说明输出到 `dist/`。`ExecutionPolicy Bypass` 仅作用于本次 PowerShell 进程，不永久修改执行策略。

```powershell
# 状态逻辑、语言文本与偏好保存测试；不会息屏
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1

# 另加实际窗口双语切换与渲染检查；不会息屏，需要交互式桌面
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1 -UI
```

测试结果输出到 `test-results/`。UI 测试会暂时显示窗口并生成程序自身的截图；不会截取桌面、修改语言偏好或实际执行关屏。结果表示程序逻辑和窗口正常，不能代替真实显示器或整夜使用测试。更多见 [验证说明](docs/VALIDATION.md)。

## 原理与限制

守护期间订阅 Windows 显示状态通知，意外亮屏后留出约 150 毫秒判断输入，再请求关屏；每 1.5 秒还有补充关屏请求。黑色窗口降低桌面露出的机会，保持运行请求让后台任务继续执行。

- 亮屏必须先发生，软件才能响应；不能承诺完全不闪或没有背光。Windows 报告“已亮屏”也不代表外接显示器真的亮起。
- 这不是锁屏：它不会锁定账户、屏蔽所有弹窗或替代 Windows 勿扰。
- 安全桌面、锁屏、远程会话或虚拟输入设备可能影响输入判断。安全桌面使用当前会话输入时间作为恢复后备，优先避免无法操作。
- 自动化或虚拟输入也可能结束守护；不要把它当作鉴别真实操作者的安全机制。
- 程序退出、崩溃、系统重启后不会继续守护。没有开机自启、系统服务或计划任务。

## 隐私与本地文件

不联网，不保存输入文本、具体按键、鼠标位置或桌面截图。常规运行只在程序目录写入：

- `language.txt`：`zh` 或 `en`。目录不可写时语言仍能切换，但不能保存。
- `guard.log`、`guard.log.old`：启动、显示状态、结束原因等诊断信息，约 256 KB 轮换。可能包含你的使用时间，分享前请检查。

这些文件、测试结果、快捷方式和构建产物都已加入 `.gitignore`。删除程序前先从托盘退出，再删除解压目录即可。

## 项目结构

```text
src/                 程序、版本信息和 Windows 清单
assets/              原创月亮星星图标
scripts/             图标生成源码与重建脚本
docs/                双语截图、验证说明与上传步骤
build.ps1            构建
test.ps1             自动检查
README.md            中文说明
README.en.md         English guide
LICENSE              MIT
```

## 发布与贡献

项目仓库：[PB-in-GH/NightScreenGuard](https://github.com/PB-in-GH/NightScreenGuard)。可运行版本见 [Releases](https://github.com/PB-in-GH/NightScreenGuard/releases)，后续版本发布流程见 [Publishing](docs/PUBLISHING.md)。修改与提交约定见 [CONTRIBUTING](CONTRIBUTING.md)。

## 许可证

代码、文档及本项目原创月亮星星图标采用 [MIT License](LICENSE)。
