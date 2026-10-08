using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace NightScreenGuard {
    static class L10n {
        public static string Language = "en";
        static readonly string PreferencePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.txt");
        static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]> {
            { "title", new string[] { "夜间息屏守护", "Night Screen Guard" } },
            { "tagline", new string[] { "电脑继续工作，屏幕保持安静。", "Keep your computer working and your screens quiet." } },
            { "ready", new string[] { "准备就绪", "Ready" } },
            { "intro", new string[] { "开始后等待 5 秒，再遮黑并关闭所有屏幕。", "A 5-second countdown, then black covers and display power-off." } },
            { "mouse", new string[] { "允许移动鼠标结束守护（过滤轻微抖动）", "Allow mouse movement to end guarding (filters slight jitter)" } },
            { "start", new string[] { "开始守护 · 5 秒", "Start guarding · 5 sec" } },
            { "minimize", new string[] { "收起到托盘", "Hide to tray" } },
            { "exit", new string[] { "退出", "Exit" } },
            { "help", new string[] { "唤亮：按任意键 / 点击鼠标 / 移动鼠标\n快捷键：Ctrl + Alt + F12 开始或取消；Ctrl + Alt + End 停止守护\n可能短暂闪亮；不接管关机、合盖睡眠或 Windows 安全界面。", "Wake: press a key, click, or move your mouse.\nCtrl + Alt + F12: start / cancel · Ctrl + Alt + End: stop guarding\nBrief flashes are possible. System sleep and secure screens stay under Windows control." } },
            { "open", new string[] { "打开控制面板", "Open control panel" } },
            { "menuStart", new string[] { "开始守护（5 秒）", "Start guarding (5 sec)" } },
            { "stop", new string[] { "停止守护", "Stop guarding" } },
            { "trayIdle", new string[] { "夜间息屏守护 · 未开始", "Night Screen Guard · Idle" } },
            { "trayActive", new string[] { "夜间息屏守护 · 守护中", "Night Screen Guard · Guarding" } },
            { "inputError", new string[] { "输入监听不可用", "Input monitoring unavailable" } },
            { "inputErrorDetail", new string[] { "为避免无法唤亮，已禁止开始守护。请退出后在本地桌面重新打开。", "Guarding is disabled so you can always wake the display. Reopen on your local desktop." } },
            { "hotkeyError", new string[] { "部分全局快捷键被占用；仍可使用按钮开始、键鼠结束。", "Some shortcuts are in use. Use the buttons to start and your keyboard or mouse to stop." } },
            { "powerError", new string[] { "显示事件不可用；仍可手动开始，并每 1.5 秒补充关屏。", "Display events are unavailable. Guarding will still request power-off every 1.5 seconds." } },
            { "cancel", new string[] { "取消守护", "Cancel guarding" } },
            { "stopped", new string[] { "已结束守护", "Guarding ended" } },
            { "stoppedDetail", new string[] { "屏幕恢复正常。需要时可再次开始。", "Guarding has stopped. You can start again whenever you need." } },
            { "release", new string[] { "请松开键鼠。按 Esc 或再次点击按钮可以取消。", "Release your keyboard and mouse. Press Esc or click Cancel to cancel." } },
            { "alreadyRunning", new string[] { "息屏守护已经运行，请在系统托盘中打开。", "Night Screen Guard is already running. Click its moon icon in the system tray." } },
            { "fatal", new string[] { "守护已停止。请重新打开程序。\n", "Guarding has stopped. Please reopen the application.\n" } },
            { "countdown", new string[] { "即将开始 · {0} 秒", "Starting in {0} sec" } },
            { "active", new string[] { "守护中", "Guarding" } },
            { "activeDetail", new string[] { "正在保持息屏。操作键鼠即可结束守护。", "Keeping the displays off. Use your keyboard or mouse to end guarding." } },
            { "saveError", new string[] { "本次语言已切换，但无法保存偏好；下次将使用系统默认语言。", "Language changed, but the preference could not be saved. The system language will be used next time." } }
        };
        public static void Initialize(string[] args) {
            string fallback = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh" : "en";
            Language = ReadPreference(PreferencePath, fallback);
            foreach (string arg in args) { if (arg == "--lang=zh") Language = "zh"; if (arg == "--lang=en") Language = "en"; }
        }
        public static string ReadPreference(string path, string fallback) {
            try { string code = File.ReadAllText(path).Trim(); if (code == "zh" || code == "en") return code; } catch (IOException) { } catch (UnauthorizedAccessException) { }
            return fallback;
        }
        public static void WritePreference(string path, string code) { File.WriteAllText(path, code, new UTF8Encoding(false)); }
        public static bool Select(string code, bool save) {
            if (code != "zh" && code != "en") throw new ArgumentException("Unsupported language");
            Language = code;
            if (save) { try { WritePreference(PreferencePath, code); } catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; } }
            return true;
        }
        public static string T(string key) { return Texts[key][Language == "zh" ? 0 : 1]; }
        public static string F(string key, object value) { return string.Format(T(key), value); }
        public static bool Validate() {
            foreach (string[] pair in Texts.Values) if (pair.Length != 2 || string.IsNullOrEmpty(pair[0]) || string.IsNullOrEmpty(pair[1])) return false;
            return true;
        }
    }
    enum GuardState { Idle, Countdown, Active }
    sealed class GuardLogic {
        public GuardState State = GuardState.Idle;
        public long Deadline, IgnoreInputUntil, LastOff = -10000, RecheckAt = long.MaxValue;
        public int Distance;
        public long MotionStart;
        public void Arm(long now, int delay) { State = GuardState.Countdown; Deadline = now + delay; }
        public bool Tick(long now) {
            if (State != GuardState.Countdown || now < Deadline) return false;
            State = GuardState.Active; IgnoreInputUntil = now + 700; LastOff = -10000;
            RecheckAt = long.MaxValue; Distance = 0; MotionStart = now; return true;
        }
        public bool Input(long now, bool decisive, int distance, bool mouseAllowed) {
            if (State != GuardState.Active || now < IgnoreInputUntil) return false;
            if (decisive) return true;
            if (!mouseAllowed) return false;
            if (now - MotionStart > 650) { MotionStart = now; Distance = 0; }
            Distance = Math.Min(100000, Distance + Math.Min(10000, distance));
            return Distance >= 24;
        }
        public void DisplayOn(long now) { if (State == GuardState.Active) RecheckAt = Math.Min(RecheckAt, now + 150); }
        public bool NeedOff(long now) {
            return State == GuardState.Active && now - LastOff >= 250 && (now >= RecheckAt || now - LastOff >= 1500);
        }
        public void DidOff(long now) { LastOff = now; RecheckAt = long.MaxValue; }
        public void Stop(long now) { State = GuardState.Idle; RecheckAt = long.MaxValue; }
    }
    static class Native {
        [StructLayout(LayoutKind.Sequential)] public struct RID { public ushort Page, Usage; public uint Flags; public IntPtr Target; }
        [StructLayout(LayoutKind.Sequential)] public struct LASTINPUT { public uint Size, Time; }
        [DllImport("user32.dll", SetLastError=true)] public static extern bool RegisterRawInputDevices(RID[] d, uint n, uint size);
        [DllImport("user32.dll", SetLastError=true)] public static extern uint GetRawInputData(IntPtr raw, uint command, IntPtr data, ref uint size, uint header);
        [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr RegisterPowerSettingNotification(IntPtr h, ref Guid g, uint flags);
        [DllImport("user32.dll")] public static extern bool UnregisterPowerSettingNotification(IntPtr h);
        [DllImport("user32.dll", SetLastError=true)] public static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr DefWindowProc(IntPtr h, uint msg, IntPtr w, IntPtr l);
        [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint state);
        [DllImport("user32.dll")] public static extern bool GetLastInputInfo(ref LASTINPUT info);
        [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern bool GetUserObjectInformation(IntPtr h, int index, StringBuilder text, int size, out int needed);
        [DllImport("user32.dll")] public static extern bool CloseDesktop(IntPtr h);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
        public static uint InputTime() { LASTINPUT i = new LASTINPUT(); i.Size = 8; return GetLastInputInfo(ref i) ? i.Time : 0; }
        public static bool DefaultDesktop() {
            IntPtr h = OpenInputDesktop(0, false, 1);
            if (h == IntPtr.Zero) return false;
            try { StringBuilder b = new StringBuilder(256); int n; return GetUserObjectInformation(h, 2, b, 512, out n) && b.ToString() == "Default"; }
            finally { CloseDesktop(h); }
        }
    }
    static class Log {
        public static string FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "guard.log");
        public static void Write(string message) {
            try {
                if (File.Exists(FileName) && new FileInfo(FileName).Length > 262144) {
                    string old = FileName + ".old"; if (File.Exists(old)) File.Delete(old); File.Move(FileName, old);
                }
                File.AppendAllText(FileName, DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " " + message + Environment.NewLine, Encoding.UTF8);
            } catch { }
        }
    }
    sealed class BlackCover : Form {
        public BlackCover(Rectangle bounds) {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; BackColor = Color.Black;
            StartPosition = FormStartPosition.Manual; Bounds = bounds; TopMost = true; Cursor = Cursors.Default;
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) { return true; }
        protected override void WndProc(ref Message m) {
            if (m.Msg == 0x20) { Cursor.Current = null; m.Result = (IntPtr)1; return; }
            base.WndProc(ref m);
        }
    }
    sealed class GuardForm : Form {
        readonly GuardLogic logic = new GuardLogic();
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        readonly List<BlackCover> covers = new List<BlackCover>();
        readonly Dictionary<IntPtr, Point> absolute = new Dictionary<IntPtr, Point>();
        readonly Guid displayGuid = new Guid("2B84C20E-AD23-4DDF-93DB-05FFBD7EFCA5");
        Label state, details, title, subtitle, help;
        RadioButton chinese, english;
        Button minimize, exit;
        ContextMenuStrip menu;
        bool ended, emergencyReady;
        CheckBox mouse;
        Button start;
        NotifyIcon tray;
        IntPtr powerCookie;
        bool rawReady, hotkeyReady, quitting, initialized, inPowerCall, checkingDesktop;
        bool dryRun, smoke;
        long lastDesktopCheck, smokeStart, lastStop = -10000;
        uint lastInput;
        int smokePhase, offCount, displayEvents, onEvents, offEvents, secureStops;
        string testDir;
        public GuardForm(string[] args) {
            dryRun = Array.IndexOf(args, "--ui-test") >= 0;
            smoke = Array.IndexOf(args, "--smoke-test") >= 0;
            if (dryRun || smoke) testDir = args[args.Length - 1];
            Text = L10n.T("title"); Icon = LoadNightIcon(32);
            Font = new Font("Microsoft YaHei UI", 10F); AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(20, 25, 36); ForeColor = Color.FromArgb(225, 232, 242);
            ClientSize = new Size(780, 450); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            title = new Label { Text = L10n.T("title"), Font = new Font(Font.FontFamily, 23, FontStyle.Bold), AutoSize = true, Location = new Point(28, 25) };
            Controls.Add(title);
            subtitle = new Label { Text = L10n.T("tagline"), AutoSize = true, Location = new Point(31, 77), ForeColor = Color.FromArgb(159, 174, 193) }; Controls.Add(subtitle);
            state = new Label { Text = L10n.T("ready"), AutoSize = true, Location = new Point(31, 124), Font = new Font(Font.FontFamily, 14, FontStyle.Bold), ForeColor = Color.FromArgb(112, 214, 182) }; Controls.Add(state);
            details = new Label { Text = L10n.T("intro"), Location = new Point(31, 161), Size = new Size(718, 48) }; Controls.Add(details);
            mouse = new CheckBox { Text = L10n.T("mouse"), Checked = true, AutoSize = true, Location = new Point(31, 216) }; Controls.Add(mouse);
            start = MakeButton(L10n.T("start"), 31, 260, 227, Color.FromArgb(44, 108, 92));
            start.Click += delegate { if (logic.State == GuardState.Idle) Arm(5000); else Stop("manual", true); };
            minimize = MakeButton(L10n.T("minimize"), 273, 260, 167, Color.FromArgb(47, 58, 78)); minimize.Click += delegate { Hide(); };
            exit = MakeButton(L10n.T("exit"), 454, 260, 167, Color.FromArgb(47, 58, 78)); exit.Click += delegate { Quit(); };
            help = new Label { Text = L10n.T("help"), Location = new Point(31, 325), Size = new Size(718, 106), ForeColor = Color.FromArgb(159, 174, 193), Font = new Font(Font.FontFamily, 9F) }; Controls.Add(help);
            menu = new ContextMenuStrip();
            menu.Items.Add(L10n.T("open"), null, delegate { ShowPanel(); });
            menu.Items.Add(L10n.T("menuStart"), null, delegate { Arm(5000); });
            menu.Items.Add(L10n.T("stop"), null, delegate { Stop("tray", true); });
            menu.Items.Add(L10n.T("exit"), null, delegate { Quit(); });
            tray = new NotifyIcon { Icon = LoadNightIcon(SystemInformation.SmallIconSize.Width), Text = L10n.T("trayIdle"), ContextMenuStrip = menu, Visible = true };
            tray.MouseClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) ShowPanel(); };
            Controls.Add(new Label { Text = "语言 / Language", AutoSize = true, Location = new Point(580, 13), ForeColor = Color.FromArgb(159, 174, 193), Font = new Font(Font.FontFamily, 9F) });
            chinese = MakeLanguageButton("中文", 580, L10n.Language == "zh");
            english = MakeLanguageButton("English", 666, L10n.Language == "en");
            chinese.CheckedChanged += delegate { if (chinese.Checked) SelectLanguage("zh"); };
            english.CheckedChanged += delegate { if (english.Checked) SelectLanguage("en"); };
            StyleLanguages();
            timer.Interval = 100; timer.Tick += Tick;
        }
        RadioButton MakeLanguageButton(string text, int x, bool selected) {
            RadioButton button = new RadioButton { Text = text, Appearance = Appearance.Button, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(x, 37), Size = new Size(82, 32), Checked = selected };
            button.FlatAppearance.BorderSize = 0; Controls.Add(button); return button;
        }
        void StyleLanguages() {
            foreach (RadioButton button in new RadioButton[] { chinese, english }) {
                button.BackColor = button.Checked ? Color.FromArgb(44, 108, 92) : Color.FromArgb(47, 58, 78);
                button.FlatAppearance.CheckedBackColor = Color.FromArgb(44, 108, 92);
            }
        }
        void SelectLanguage(string code) {
            bool saved = L10n.Select(code, !dryRun && !smoke);
            StyleLanguages(); ApplyLanguage();
            if (!saved) details.Text = L10n.T("saveError");
        }
        void ApplyLanguage() {
            Text = title.Text = L10n.T("title");
            subtitle.Text = L10n.T("tagline"); mouse.Text = L10n.T("mouse"); help.Text = L10n.T("help");
            minimize.Text = L10n.T("minimize"); exit.Text = L10n.T("exit");
            menu.Items[0].Text = L10n.T("open"); menu.Items[1].Text = L10n.T("menuStart");
            menu.Items[2].Text = L10n.T("stop"); menu.Items[3].Text = L10n.T("exit");
            start.Text = L10n.T(logic.State == GuardState.Idle ? "start" : "cancel");
            tray.Text = L10n.T(logic.State == GuardState.Active ? "trayActive" : "trayIdle");
            if (logic.State == GuardState.Countdown) {
                state.Text = L10n.F("countdown", Math.Max(1, (logic.Deadline - clock.ElapsedMilliseconds + 999) / 1000));
                details.Text = L10n.T("release");
            } else if (logic.State == GuardState.Active) {
                state.Text = L10n.T("active"); details.Text = L10n.T("activeDetail");
            } else if (initialized && !rawReady) {
                state.Text = L10n.T("inputError"); details.Text = L10n.T("inputErrorDetail");
            } else {
                state.Text = L10n.T(ended ? "stopped" : "ready");
                details.Text = L10n.T(ended ? "stoppedDetail" : "intro");
                if (initialized && !ended && (!hotkeyReady || !emergencyReady)) details.Text = L10n.T("hotkeyError");
                if (initialized && !ended && powerCookie == IntPtr.Zero) details.Text = L10n.T("powerError");
            }
        }
        static Icon LoadNightIcon(int size) {
            using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("NightScreenGuard.ico"))
            using (Icon icon = new Icon(stream, size, size)) return (Icon)icon.Clone();
        }
        Button MakeButton(string text, int x, int y, int width, Color color) {
            Button b = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 44), BackColor = color, ForeColor = ForeColor, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; Controls.Add(b); return b;
        }
        protected override void OnShown(EventArgs e) {
            base.OnShown(e); if (initialized) return; initialized = true;
            Native.RID[] d = { new Native.RID { Page=1, Usage=2, Flags=0x100, Target=Handle }, new Native.RID { Page=1, Usage=6, Flags=0x100, Target=Handle } };
            rawReady = Native.RegisterRawInputDevices(d, 2, (uint)Marshal.SizeOf(typeof(Native.RID)));
            Guid g = displayGuid; powerCookie = Native.RegisterPowerSettingNotification(Handle, ref g, 0);
            hotkeyReady = dryRun || Native.RegisterHotKey(Handle, 1, 0x4003, 0x7B);
            bool emergency = dryRun || Native.RegisterHotKey(Handle, 2, 0x4003, 0x23); emergencyReady = emergency;
            lastInput = Native.InputTime();
            Log.Write("started raw=" + rawReady + " display_notification=" + (powerCookie != IntPtr.Zero) + " hotkey=" + hotkeyReady + " emergency=" + emergency + " dry_run=" + dryRun);
            if (!rawReady) { start.Enabled = false; state.Text = L10n.T("inputError"); details.Text = L10n.T("inputErrorDetail"); }
            else if (!hotkeyReady || !emergency) details.Text = L10n.T("hotkeyError");
            if (powerCookie == IntPtr.Zero) { details.Text = L10n.T("powerError"); }
            ApplyLanguage(); timer.Start();
            if (dryRun || smoke) { Directory.CreateDirectory(testDir); smokeStart = clock.ElapsedMilliseconds; }
        }
        void ShowPanel() { Show(); WindowState = FormWindowState.Normal; Activate(); }
        void Arm(int delay) {
            if (!rawReady || logic.State != GuardState.Idle) return;
            ended = false; logic.Arm(clock.ElapsedMilliseconds, delay); start.Text = L10n.T("cancel");
            Log.Write("arming delay_ms=" + delay);
        }
        void EnterGuard() {
            if (!dryRun && Native.SetThreadExecutionState(0x80000001) == 0) { Stop("keep_awake_failed", true); return; }
            Hide(); RebuildCovers(); lastInput = Native.InputTime(); absolute.Clear();
            ApplyLanguage(); Log.Write("guard_active"); TurnOff();
        }
        void RebuildCovers() {
            foreach (BlackCover c in covers) c.Close(); covers.Clear();
            if (dryRun) return;
            foreach (Screen s in Screen.AllScreens) { BlackCover c = new BlackCover(s.Bounds); covers.Add(c); c.Show(); }
            if (covers.Count > 0) covers[0].Activate();
        }
        void TurnOff() {
            if (logic.State != GuardState.Active || inPowerCall) return;
            logic.DidOff(clock.ElapsedMilliseconds); offCount++;
            if (dryRun) return;
            inPowerCall = true;
            try { Native.DefWindowProc(Handle, 0x112, (IntPtr)0xF170, (IntPtr)2); }
            finally { inPowerCall = false; }
        }
        void Stop(string why, bool show) {
            bool wasActive = logic.State == GuardState.Active;
            if (logic.State == GuardState.Idle) { if (show) ShowPanel(); return; }
            ended = true; logic.Stop(clock.ElapsedMilliseconds); lastStop = clock.ElapsedMilliseconds;
            foreach (BlackCover c in covers) c.Close(); covers.Clear();
            if (!dryRun) {
                Native.SetThreadExecutionState(0x80000000);
                if (wasActive) Native.DefWindowProc(Handle, 0x112, (IntPtr)0xF170, (IntPtr)(-1));
            }
            state.Text = L10n.T("stopped"); details.Text = L10n.T("stoppedDetail"); start.Text = L10n.T("start");
            tray.Text = L10n.T("trayIdle"); Log.Write("guard_stopped reason=" + why + " off_requests=" + offCount);
            if (show) ShowPanel();
        }
        void Tick(object sender, EventArgs e) {
            long now = clock.ElapsedMilliseconds;
            if (logic.Tick(now)) EnterGuard();
            if (logic.State == GuardState.Countdown) { state.Text = L10n.F("countdown", Math.Max(1, (logic.Deadline - now + 999) / 1000)); details.Text = L10n.T("release"); }
            if (logic.State == GuardState.Active) {
                // On a secure/locked desktop Raw Input may not reach us. Fail open on any
                // session input there; never trap the user behind repeated power-off calls.
                if (now - lastDesktopCheck >= 300) {
                    lastDesktopCheck = now; checkingDesktop = !Native.DefaultDesktop();
                }
                uint input = Native.InputTime();
                if (checkingDesktop && input != 0 && input != lastInput && now >= logic.IgnoreInputUntil) { secureStops++; Stop("secure_desktop_input", false); }
                lastInput = input;
                if (logic.NeedOff(now)) TurnOff();
            }
            if (dryRun || smoke) TestTick(now);
        }
        protected override void WndProc(ref Message m) {
            if (m.Msg == 0xFF && initialized) ReadInput(m.LParam);
            else if (m.Msg == 0x312 && initialized) {
                if (m.WParam.ToInt32() == 2) { Stop("emergency", true); }
                else if (clock.ElapsedMilliseconds - lastStop > 800) {
                    if (logic.State == GuardState.Idle) Arm(5000); else Stop("hotkey", true);
                }
            }
            else if (m.Msg == 0x218 && m.WParam.ToInt32() == 0x8013 && m.LParam != IntPtr.Zero && initialized) {
                Guid g = (Guid)Marshal.PtrToStructure(m.LParam, typeof(Guid));
                if (g == displayGuid && Marshal.ReadInt32(m.LParam, 16) >= 4) {
                    int value = Marshal.ReadInt32(m.LParam, 20); displayEvents++;
                    if (value == 0) offEvents++; else if (value == 1) onEvents++;
                    Log.Write("display_state=" + value);
                    long now = clock.ElapsedMilliseconds;
                    if (value != 0) logic.DisplayOn(now);
                }
            }
            else if (m.Msg == 0x7E && initialized && logic.State == GuardState.Active) {
                BeginInvoke((Action)delegate { if (logic.State == GuardState.Active) { RebuildCovers(); logic.DisplayOn(clock.ElapsedMilliseconds); } });
            }
            base.WndProc(ref m);
        }
        void ReadInput(IntPtr handle) {
            if (logic.State == GuardState.Idle) return;
            uint header = (uint)(8 + IntPtr.Size * 2), size = 0;
            if (Native.GetRawInputData(handle, 0x10000003, IntPtr.Zero, ref size, header) != 0 || size < header || size > 65536) return;
            IntPtr p = Marshal.AllocHGlobal((int)size);
            try {
                if (Native.GetRawInputData(handle, 0x10000003, p, ref size, header) != size) return;
                int type = Marshal.ReadInt32(p), offset = (int)header;
                IntPtr device = Marshal.ReadIntPtr(p, 8);
                long now = clock.ElapsedMilliseconds;
                if (type == 1 && size >= header + 16) {
                    int flags = (ushort)Marshal.ReadInt16(p, offset + 2), key = (ushort)Marshal.ReadInt16(p, offset + 6);
                    if ((flags & 1) != 0 || key == 0x5F || key == 255) return;
                    if (logic.State == GuardState.Countdown && key == 27) Stop("countdown_escape", true);
                    else if (logic.Input(now, true, 0, mouse.Checked)) Stop("keyboard", false);
                } else if (type == 0 && size >= header + 24) {
                    int flags = (ushort)Marshal.ReadInt16(p, offset), buttons = (ushort)Marshal.ReadInt16(p, offset + 4);
                    int x = Marshal.ReadInt32(p, offset + 12), y = Marshal.ReadInt32(p, offset + 16);
                    if ((flags & 1) != 0) {
                        Point old; Point current = new Point(x, y);
                        if (absolute.TryGetValue(device, out old)) { x -= old.X; y -= old.Y; } else { x = 0; y = 0; }
                        absolute[device] = current;
                    }
                    int distance = (int)Math.Min(10000L, Math.Abs((long)x) + Math.Abs((long)y));
                    bool clicked = (buttons & 0xD55) != 0;
                    if (logic.Input(now, clicked, distance, mouse.Checked)) Stop(clicked ? "mouse_button" : "mouse_move", false);
                }
            } finally { Marshal.FreeHGlobal(p); }
        }
        protected override bool ProcessCmdKey(ref Message m, Keys key) {
            if (key == Keys.Escape && logic.State == GuardState.Countdown) { Stop("countdown_escape", true); return true; }
            return base.ProcessCmdKey(ref m, key);
        }
        protected override void OnFormClosing(FormClosingEventArgs e) {
            if (!quitting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); return; }
            timer.Stop(); Stop("exit", false);
            if (powerCookie != IntPtr.Zero) Native.UnregisterPowerSettingNotification(powerCookie);
            Native.UnregisterHotKey(Handle, 1); Native.UnregisterHotKey(Handle, 2);
            Native.RID[] d = { new Native.RID { Page=1, Usage=2, Flags=1 }, new Native.RID { Page=1, Usage=6, Flags=1 } };
            if (rawReady) Native.RegisterRawInputDevices(d, 2, (uint)Marshal.SizeOf(typeof(Native.RID)));
            tray.Visible = false; tray.Dispose(); timer.Dispose(); Log.Write("exited"); base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing) {
            if (disposing) {
                timer.Stop();
                foreach (BlackCover c in covers) c.Dispose();
                covers.Clear();
                if (!dryRun) Native.SetThreadExecutionState(0x80000000);
                if (tray != null) { tray.Visible = false; tray.Dispose(); }
                timer.Dispose();
            }
            base.Dispose(disposing);
        }
        void Quit() { quitting = true; Close(); }
        void TestTick(long now) {
            long elapsed = now - smokeStart;
            if (dryRun) {
                if (smokePhase == 0 && elapsed > 600) {
                    foreach (string code in new string[] { "zh", "en" }) {
                        if (code == "zh") chinese.Checked = true; else english.Checked = true;
                        ApplyLanguage();
                        if (title.Text != L10n.T("title") || start.Text != L10n.T("start") || menu.Items[0].Text != L10n.T("open") || L10n.Language != code) throw new Exception("UI language mismatch");
                        using (Bitmap b = new Bitmap(Width, Height)) { DrawToBitmap(b, new Rectangle(0, 0, Width, Height)); b.Save(Path.Combine(testDir, "panel-" + code + ".png")); }
                    }
                    Arm(200); smokePhase++;
                } else if (smokePhase == 1 && elapsed > 1200) { logic.DisplayOn(now); smokePhase++; }
                else if (smokePhase == 2 && elapsed > 1900) {
                    Stop("ui_test", true);
                    foreach (string code in new string[] { "zh", "en" }) {
                        if (code == "zh") chinese.Checked = true; else english.Checked = true; ApplyLanguage();
                        if (state.Text != L10n.T("stopped") || tray.Text != L10n.T("trayIdle")) throw new Exception("UI stopped language mismatch");
                    }
                    WriteTest(); Quit();
                }
            } else {
                if (smokePhase == 0 && elapsed > 1200) { Arm(1500); smokePhase++; }
                else if (smokePhase == 1 && elapsed > 5500) {
                    if (logic.State == GuardState.Active) { Log.Write("smoke_inject_display_on"); Native.DefWindowProc(Handle, 0x112, (IntPtr)0xF170, (IntPtr)(-1)); }
                    smokePhase++;
                } else if (smokePhase == 2 && elapsed > 8000) {
                    Log.Write("smoke_before_recovery active=" + (logic.State == GuardState.Active));
                    Stop("smoke_auto_recovery", true); smokePhase++;
                } else if (elapsed > 9200) { WriteTest(); Quit(); }
            }
        }
        void WriteTest() {
            File.WriteAllText(Path.Combine(testDir, dryRun ? "ui-test.txt" : "smoke-test.txt"),
                "raw_input=" + rawReady + "\r\npower_events_registered=" + (powerCookie != IntPtr.Zero) + "\r\nhotkey=" + hotkeyReady + "\r\nhotkeys_skipped_for_ui_test=" + dryRun +
                "\r\noff_requests=" + offCount + "\r\ndisplay_events=" + displayEvents + "\r\non_events=" + onEvents + "\r\noff_events=" + offEvents +
                "\r\nsecure_input_stops=" + secureStops + "\r\nfinal_state=" + logic.State + "\r\n", Encoding.UTF8);
        }
    }
    static class Program {
        [STAThread] static int Main(string[] args) {
            L10n.Initialize(args);
            if (Array.IndexOf(args, "--self-test") >= 0) return SelfTest(args[args.Length-1]);
            bool owned;
            string mutexName = "Local\\NightScreenGuard_20261007" + (Array.IndexOf(args, "--ui-test") >= 0 ? "_UITest" : "");
            using (Mutex mutex = new Mutex(true, mutexName, out owned)) {
                if (!owned) { MessageBox.Show(L10n.T("alreadyRunning"), L10n.T("title")); return 0; }
                Native.SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                try { using (GuardForm form = new GuardForm(args)) Application.Run(form); }
                catch (Exception ex) {
                    Native.SetThreadExecutionState(0x80000000);
                    Log.Write("fatal=" + ex.GetType().Name + " " + ex.Message);
                    using (Form recovery = new Form()) Native.DefWindowProc(recovery.Handle, 0x112, (IntPtr)0xF170, (IntPtr)(-1));
                    MessageBox.Show(L10n.T("fatal") + ex.Message, L10n.T("title")); return 1;
                }
                finally { mutex.ReleaseMutex(); }
            }
            return 0;
        }
        static int SelfTest(string path) {
            List<string> passed = new List<string>();
            try {
                Check(L10n.Validate(), "bilingual_strings_complete", passed);
                L10n.Select("en", false); Check(L10n.F("countdown", 5) == "Starting in 5 sec", "english_countdown", passed);
                L10n.Select("zh", false); Check(L10n.F("countdown", 5) == "即将开始 · 5 秒", "chinese_countdown", passed);
                string preference = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), "test-language.txt");
                try {
                    L10n.WritePreference(preference, "en"); Check(L10n.ReadPreference(preference, "zh") == "en", "language_preference_roundtrip", passed);
                    File.WriteAllText(preference, "invalid"); Check(L10n.ReadPreference(preference, "zh") == "zh", "invalid_preference_fallback", passed);
                } finally { if (File.Exists(preference)) File.Delete(preference); }
                GuardLogic g = new GuardLogic();
                Check(g.State == GuardState.Idle && !g.NeedOff(100), "idle_does_not_blank", passed);
                g.Arm(100, 5000); Check(!g.Tick(5099) && g.Tick(5100), "countdown_boundary", passed);
                Check(!g.Input(5200, true, 0, true), "release_grace", passed);
                Check(g.NeedOff(5100), "initial_power_off", passed); g.DidOff(5100);
                g.DisplayOn(5300); Check(!g.NeedOff(5400) && g.NeedOff(5450), "unexpected_wake_reblank", passed); g.DidOff(5450);
                Check(!g.Input(5900, false, 2, true) && g.Input(5910, false, 23, true), "mouse_jitter_threshold", passed);
                g.Distance = 0; g.MotionStart = 5900; Check(!g.Input(7000, false, 2, true), "old_jitter_expires", passed);
                Check(!g.Input(7010, false, 100, false) && g.Input(7010, true, 0, false), "keyboard_only_mode", passed);
                Check(g.NeedOff(7000), "periodic_fallback", passed);
                g.Stop(7100); Check(!g.NeedOff(10000) && g.State == GuardState.Idle, "stop_cancels_pending_off", passed);
                g.Arm(7200, 5000); g.Stop(7300); Check(!g.Tick(13000), "cancel_countdown", passed);
                File.WriteAllLines(path, passed.ToArray(), Encoding.UTF8); return 0;
            } catch (Exception e) { File.WriteAllText(path, e.ToString(), Encoding.UTF8); return 1; }
        }
        static void Check(bool ok, string name, List<string> passed) { if (!ok) throw new Exception("FAIL " + name); passed.Add("PASS " + name); }
    }
}
