# Night Screen Guard · 夜间息屏守护

[中文](README.md) | [English](README.en.md)

**[Download for Windows](https://github.com/PB-in-GH/NightScreenGuard/releases/latest) · [Report an issue](https://github.com/PB-in-GH/NightScreenGuard/issues)**

A lightweight, portable Windows utility. Turn off your screens at night while downloads, computation and other background tasks keep running.

It goes beyond a black overlay: it asks Windows to power off your displays and tries to keep them off when notifications or other events wake them without your input. Use your keyboard or mouse to stop guarding and restore the display.

Compatible monitors can enter standby with the screen fully dark, reducing prolonged screen use, burn-in risk and light that may disturb your sleep. Behavior depends on your monitor; brief wake-ups may still occur.

![English interface](docs/screenshot-en.png)

## Use

1. Download and extract the Windows build, then open `NightScreenGuard.exe`.
2. Click **Start guarding**, release the keyboard and mouse, and wait five seconds.
3. Press a key, click or move the mouse to restore the display. Movement-to-wake can be disabled in the app.

Switch between **中文 / English** in the window. Single-click the moon tray icon to open the window; use its right-click menu to quit. Your system's moon key keeps its existing behavior.

Shortcuts: `Ctrl + Alt + F12` starts or cancels guarding; `Ctrl + Alt + End` stops guarding.

## Privacy

No keyboard or mouse input content is recorded, no online services are used, and all features run locally.

Windows 10/11 x64 · .NET Framework 4.8 · [MIT License](LICENSE)
