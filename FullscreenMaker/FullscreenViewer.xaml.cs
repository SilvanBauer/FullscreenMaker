using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Screen = System.Windows.Forms.Screen;

namespace FullscreenMaker {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FullscreenViewer : Window {
        private readonly string _executingPath;

        private readonly FullscreenMakerSettings _settings;

        private Process _process;

        private DispatcherTimer _dispatcherTimerProcess;

        private bool _processEnded;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hwnd, out Rect lpRect);

        public FullscreenViewer() {
            InitializeComponent();

            DataContext = this;
            
            _executingPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            if (File.Exists($"{_executingPath}/fullscreenMakerSettings.json")) {
                var settingsJson = File.ReadAllText($"{_executingPath}/fullscreenMakerSettings.json");
                _settings = JsonConvert.DeserializeObject<FullscreenMakerSettings>(settingsJson);
            } else {
                MessageBox.Show("fullscreenMakerSettings.json could not be found", "Settings not found", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            if (_settings != null && File.Exists($"{_executingPath}/{_settings.ExecutablePath}")) {
                var startInfo = new ProcessStartInfo();
                startInfo.WorkingDirectory = _executingPath;
                startInfo.FileName = $"{_executingPath}/{_settings.ExecutablePath}";
                if (_settings.ProcessName == null) {
                    _process = Process.Start(startInfo);

                    ForceBorderlessFullscreen();
                } else {
                    Process.Start(startInfo);
                }

                _dispatcherTimerProcess = new DispatcherTimer();
                _dispatcherTimerProcess.Interval = TimeSpan.FromMilliseconds(3000);
                _dispatcherTimerProcess.Tick += OnCheckProcessExcecution;
                _dispatcherTimerProcess.Start();
            } else {
                Close();
            }
        }

        public void ForceBorderlessFullscreen() {
            var style = GetWindowLong(_process.MainWindowHandle, -16);
            var result = SetWindowLong(_process.MainWindowHandle, -16, (uint)(style & ~(0x00800000 | 0x00400000 | 0x00040000)));
            if (result == 0) {
                MessageBox.Show("Could not force borderless fullscreen", "Borderless Fullscreen failed", MessageBoxButton.OK, MessageBoxImage.Error);
            } else if (_settings.FullscreenWidth.HasValue && _settings.FullscreenHeight.HasValue) {
                var screenBonds = Screen.PrimaryScreen.Bounds;
                var x = screenBonds.Width >= _settings.FullscreenWidth.Value ? (screenBonds.Width - _settings.FullscreenWidth.Value) / 2 : 0;
                var y = screenBonds.Height >= _settings.FullscreenHeight.Value ? (screenBonds.Height - _settings.FullscreenHeight.Value) / 2 : 0;

                SetWindowPos(_process.MainWindowHandle, 0, x, y, _settings.FullscreenWidth.Value, _settings.FullscreenHeight.Value, 0x0040);
                SetParent(_process.MainWindowHandle, new WindowInteropHelper(this).Handle);
            } else {
                GetWindowRect(_process.MainWindowHandle, out var rect);
                var screenBonds = Screen.PrimaryScreen.Bounds;
                var x = screenBonds.Width >= rect.Width ? (screenBonds.Width - rect.Width) / 2 : 0;
                var y = screenBonds.Height >= rect.Height ? (screenBonds.Height - rect.Height) / 2 : 0;

                SetWindowPos(_process.MainWindowHandle, 0, x, y, rect.Width, rect.Height, 0x0040);
                SetParent(_process.MainWindowHandle, new WindowInteropHelper(this).Handle);
            }
        }

        private void OnCheckProcessExcecution(object sender, EventArgs e) {
            FindProcessByName();

            _processEnded = _process != null ? _process.HasExited : false;
            if (_processEnded) {
                Close();
            }
        }

        private void FindProcessByName() {
            if (_process == null) {
                foreach (var process in Process.GetProcesses()) {
                    if (process.MainWindowTitle.Contains(_settings.ProcessName) && !process.MainWindowTitle.EndsWith(" - Steam")) {
                        _process = process;
                    }
                }

                if (_process != null) {
                    ForceBorderlessFullscreen();
                }
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e) {
            if (!_processEnded && _process != null) {
                e.Cancel = true;
            }
        }

        private void OnWindowClosed(object sender, EventArgs e) {
            if (_dispatcherTimerProcess != null) {
                _dispatcherTimerProcess.Stop();
            }
        }
    }
}
