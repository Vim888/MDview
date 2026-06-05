using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Markdig;
using Microsoft.Win32;

namespace NativeMDView
{
    public partial class MainWindow : Window
    {
        private string _currentFilePath = string.Empty;
        private bool _isModified = false;
        private enum ViewMode { PreviewOnly, EditorOnly, Split }
        private ViewMode _currentView = ViewMode.PreviewOnly;
        private string _startupFilePath;
        private bool _isLoaded = false;
        private bool _suppressUpdate = false;

        private CancellationTokenSource _previewCts;
        private CancellationTokenSource _settingsSaveCts;

        public MainWindow(string[] args)
        {
            InitializeComponent();
            
            if (args != null && args.Length > 0 && File.Exists(args[0]))
            {
                _startupFilePath = Path.GetFullPath(args[0]);
            }
            
            Settings.Load();
            
            ApplyTheme(Settings.Theme);
            
            ToolbarPanel.Visibility = Settings.ShowToolbar ? Visibility.Visible : Visibility.Collapsed;
            Editor.FontSize = 14 * Settings.Zoom;
            _zoomLevel = Settings.Zoom;
            
            SetViewMode(ParseViewMode(Settings.ViewMode));
            
            UpdateStatusBar();
            
            KeyDown += MainWindow_KeyDown;
            SizeChanged += MainWindow_SizeChanged;
            LocationChanged += MainWindow_LocationChanged;
            StateChanged += MainWindow_StateChanged;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Width = Settings.WindowWidth;
            Height = Settings.WindowHeight;
            
            if (Settings.WindowMaximized)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                var left = Settings.WindowLeft;
                var top = Settings.WindowTop;
                
                var workArea = SystemParameters.WorkArea;
                
                if (left < workArea.Left || left > workArea.Right - 100 || top < workArea.Top || top > workArea.Bottom - 50)
                {
                    left = workArea.Left + (workArea.Width - Width) / 2;
                    top = workArea.Top + (workArea.Height - Height) / 2;
                }
                
                Left = left;
                Top = top;
            }
            
            _isLoaded = true;
            
            if (!string.IsNullOrEmpty(_startupFilePath))
            {
                OpenFileByPath(_startupFilePath);
            }
            UpdatePreview();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Editor.Focus();
                Activate();
            }));
        }

        private ViewMode ParseViewMode(string mode)
        {
            return mode switch
            {
                "editor" => ViewMode.EditorOnly,
                "split" => ViewMode.Split,
                _ => ViewMode.PreviewOnly
            };
        }

        private string ViewModeToString(ViewMode mode)
        {
            return mode switch
            {
                ViewMode.EditorOnly => "editor",
                ViewMode.Split => "split",
                _ => "preview"
            };
        }

        private void SetViewMode(ViewMode mode)
        {
            _currentView = mode;
            Settings.ViewMode = ViewModeToString(mode);
            ScheduleSettingsSave();
            UpdateStatusBar();
            
            switch (mode)
            {
                case ViewMode.PreviewOnly:
                    EditorColumn.Width = new GridLength(0);
                    PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                    Editor.Visibility = Visibility.Collapsed;
                    PreviewScroll.Visibility = Visibility.Visible;
                    break;
                case ViewMode.EditorOnly:
                    EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                    PreviewColumn.Width = new GridLength(0);
                    Editor.Visibility = Visibility.Visible;
                    PreviewScroll.Visibility = Visibility.Collapsed;
                    break;
                case ViewMode.Split:
                    EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                    PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                    Editor.Visibility = Visibility.Visible;
                    PreviewScroll.Visibility = Visibility.Visible;
                    break;
            }
            
            UpdatePreview();
        }

        private void ShowPreviewOnly_Click(object sender, RoutedEventArgs e) => SetViewMode(ViewMode.PreviewOnly);
        private void ShowEditorOnly_Click(object sender, RoutedEventArgs e) => SetViewMode(ViewMode.EditorOnly);
        private void ShowSplitView_Click(object sender, RoutedEventArgs e) => SetViewMode(ViewMode.Split);

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isLoaded) return;
            
            if (WindowState == WindowState.Normal)
            {
                Settings.WindowWidth = Width;
                Settings.WindowHeight = Height;
                Settings.WindowLeft = Left;
                Settings.WindowTop = Top;
            }
            else
            {
                Settings.WindowWidth = RestoreBounds.Width;
                Settings.WindowHeight = RestoreBounds.Height;
            }
            ScheduleSettingsSave();
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            if (!_isLoaded) return;
            
            if (WindowState == WindowState.Normal)
            {
                Settings.WindowLeft = Left;
                Settings.WindowTop = Top;
            }
            ScheduleSettingsSave();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            Settings.WindowMaximized = WindowState == WindowState.Maximized;
            ScheduleSettingsSave();
        }

        private void ScheduleSettingsSave()
        {
            _settingsSaveCts?.Cancel();
            _settingsSaveCts?.Dispose();
            _settingsSaveCts = new CancellationTokenSource();
            var token = _settingsSaveCts.Token;
            Task.Delay(500, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                {
                    Dispatcher.BeginInvoke(new Action(() => Settings.Save()));
                }
            }, token);
        }

        private void ApplyTheme(string theme)
        {
            bool isDark = theme == "dark";
            
            Resources["WindowBackground"] = isDark ? (Brush)FindResource("DarkWindowBackground") : (Brush)FindResource("LightWindowBackground");
            Resources["WindowForeground"] = isDark ? (Brush)FindResource("DarkWindowForeground") : (Brush)FindResource("LightWindowForeground");
            Resources["ToolbarBackground"] = isDark ? (Brush)FindResource("DarkToolbarBackground") : (Brush)FindResource("LightToolbarBackground");
            Resources["EditorBackground"] = isDark ? (Brush)FindResource("DarkEditorBackground") : (Brush)FindResource("LightEditorBackground");
            Resources["EditorForeground"] = isDark ? (Brush)FindResource("DarkEditorForeground") : (Brush)FindResource("LightEditorForeground");
            Resources["StatusBarBackground"] = isDark ? (Brush)FindResource("DarkStatusBarBackground") : (Brush)FindResource("LightStatusBarBackground");
            Resources["StatusBarForeground"] = isDark ? (Brush)FindResource("DarkStatusBarForeground") : (Brush)FindResource("LightStatusBarForeground");
            Resources["SeparatorBrush"] = isDark ? (Brush)FindResource("DarkSeparator") : (Brush)FindResource("LightSeparator");
            Resources["ButtonHover"] = isDark ? (Brush)FindResource("DarkButtonHover") : (Brush)FindResource("LightButtonHover");
            Resources["ButtonPressed"] = isDark ? (Brush)FindResource("DarkButtonPressed") : (Brush)FindResource("LightButtonPressed");
            Resources["MenuPopupBackground"] = isDark ? (Brush)FindResource("DarkMenuPopupBackground") : (Brush)FindResource("LightMenuPopupBackground");
            Resources["MenuPopupBorder"] = isDark ? (Brush)FindResource("DarkMenuPopupBorder") : (Brush)FindResource("LightMenuPopupBorder");
            Resources["MenuHover"] = isDark ? (Brush)FindResource("DarkMenuHover") : (Brush)FindResource("LightMenuHover");
            
            Settings.Theme = theme;
            UpdatePreview();
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            ApplyTheme(Settings.Theme == "dark" ? "light" : "dark");
            Settings.Save();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.N: NewFile_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                    case Key.O: OpenFile_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                    case Key.S:
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) SaveAsFile_Click(this, new RoutedEventArgs());
                        else SaveFile_Click(this, new RoutedEventArgs());
                        e.Handled = true; break;
                    case Key.P: TogglePreview_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                    case Key.B: Bold_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                    case Key.I: Italic_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                    case Key.K: Link_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                    case Key.OemPlus: case Key.Add: ZoomIn_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                    case Key.OemMinus: case Key.Subtract: ZoomOut_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                    case Key.D0: case Key.NumPad0: ResetZoom_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                    case Key.T: ToggleTheme_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                    case Key.D1: SetViewMode(ViewMode.PreviewOnly); e.Handled = true; break;
                    case Key.D2: SetViewMode(ViewMode.EditorOnly); e.Handled = true; break;
                    case Key.D3: SetViewMode(ViewMode.Split); e.Handled = true; break;
                    case Key.E: SetViewMode(ViewMode.EditorOnly); e.Handled = true; break;
                    case Key.W: SetViewMode(ViewMode.PreviewOnly); e.Handled = true; break;
                }
            }
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            _isModified = true;
            UpdateTitle();
            if (!_suppressUpdate)
            {
                SchedulePreviewUpdate();
                UpdateStatusBar();
            }
        }

        private void Editor_SelectionChanged(object sender, RoutedEventArgs e) => UpdateCursorStatus();

        private void UpdateTitle()
        {
            var fileName = string.IsNullOrEmpty(_currentFilePath) ? "New File" : Path.GetFileName(_currentFilePath);
            Title = $"{fileName}{(_isModified ? " *" : "")} - MDView";
        }

        private void UpdateStatusBar()
        {
            var text = Editor.Text ?? "";
            var wordCount = CountWords(text);
            StatusFile.Text = string.IsNullOrEmpty(_currentFilePath) ? "New File" : Path.GetFileName(_currentFilePath);
            StatusWords.Text = $"{wordCount} words, {text.Length} chars";
            StatusZoom.Text = $"{(int)(_zoomLevel * 100)}%";
            
            StatusMode.Text = _currentView switch
            {
                ViewMode.PreviewOnly => "Ctrl+E Edit",
                ViewMode.EditorOnly => "Ctrl+W Preview",
                ViewMode.Split => "Ctrl+1 Preview | Ctrl+2 Editor",
                _ => ""
            };
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            int count = 0;
            bool inWord = false;
            foreach (var c in text)
            {
                if (c == ' ' || c == '\n' || c == '\r' || c == '\t')
                {
                    if (inWord) { count++; inWord = false; }
                }
                else
                {
                    inWord = true;
                }
            }
            if (inWord) count++;
            return count;
        }

        private void UpdateCursorStatus()
        {
            var text = Editor.Text;
            if (string.IsNullOrEmpty(text))
            {
                StatusCursor.Text = "Ln 1, Col 1";
                return;
            }
            var caretIndex = Math.Min(Editor.CaretIndex, text.Length);
            int line = 1;
            int col = 1;
            for (int i = 0; i < caretIndex; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    col = 1;
                }
                else
                {
                    col++;
                }
            }
            StatusCursor.Text = $"Ln {line}, Col {col}";
        }

        private ThemeColors GetThemeColors(string theme)
        {
            return ThemeColors.Get(theme);
        }

        private void SchedulePreviewUpdate()
        {
            _previewCts?.Cancel();
            _previewCts?.Dispose();
            _previewCts = new CancellationTokenSource();
            var token = _previewCts.Token;
            Task.Delay(300, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                {
                    Dispatcher.BeginInvoke(new Action(() => _ = UpdatePreviewAsync()));
                }
            }, token);
        }

        private async Task UpdatePreviewAsync()
        {
            var savedOffset = PreviewScroll.VerticalOffset;
            var savedText = Editor.Text;
            var colors = GetThemeColors(Settings.Theme);
            var zoom = _zoomLevel;

            Markdig.Syntax.MarkdownDocument? document = null;
            try
            {
                document = await MarkdownRenderer.ParseAsync(savedText);
            }
            catch (Exception ex)
            {
                ShowRenderError(ex.Message);
                return;
            }

            if (Editor.Text != savedText) return;

            try
            {
                var elements = MarkdownRenderer.RenderDocument(document, colors, zoom);

                if (Editor.Text != savedText) return;

                PreviewPanel.Children.Clear();
                foreach (var element in elements)
                {
                    PreviewPanel.Children.Add(element);
                }
                PreviewScroll.ScrollToVerticalOffset(savedOffset);
            }
            catch (Exception ex)
            {
                ShowRenderError(ex.Message);
            }
        }

        private void ShowRenderError(string message)
        {
            PreviewPanel.Children.Clear();
            var errorTb = new TextBlock
            {
                Text = $"Render error: {message}",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100)),
                FontSize = 14 * _zoomLevel,
                Margin = new Thickness(0, 24, 0, 0)
            };
            PreviewPanel.Children.Add(new Border { Child = errorTb });
        }

        private void UpdatePreview()
        {
            var savedOffset = PreviewScroll.VerticalOffset;
            PreviewPanel.Children.Clear();

            try
            {
                var colors = GetThemeColors(Settings.Theme);
                var elements = MarkdownRenderer.Render(Editor.Text, colors, _zoomLevel);

                foreach (var element in elements)
                {
                    PreviewPanel.Children.Add(element);
                }
            }
            catch (Exception ex)
            {
                var errorTb = new TextBlock
                {
                    Text = $"Render error: {ex.Message}",
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100)),
                    FontSize = 14 * _zoomLevel,
                    Margin = new Thickness(0, 24, 0, 0)
                };
                PreviewPanel.Children.Add(new Border { Child = errorTb });
            }

            PreviewScroll.ScrollToVerticalOffset(savedOffset);
        }

        private double _zoomLevel = 1.0;

        private void InsertText(string before, string after = "")
        {
            if (_currentView == ViewMode.PreviewOnly)
            {
                SetViewMode(ViewMode.EditorOnly);
            }
            Editor.Focus();

            try
            {
                _suppressUpdate = true;

                var start = Editor.SelectionStart;
                var length = Editor.SelectionLength;
                var selectedText = Editor.SelectedText;

                if (length > 0)
                {
                    Editor.SelectedText = $"{before}{selectedText}{after}";
                    Editor.SelectionStart = start;
                    Editor.SelectionLength = before.Length + length + after.Length;
                }
                else
                {
                    Editor.SelectedText = $"{before}{after}";
                    Editor.SelectionStart = start + before.Length;
                }
            }
            finally
            {
                _suppressUpdate = false;
                UpdatePreview();
                UpdateStatusBar();
            }
            Editor.Focus();
        }

        private void InsertLinePrefix(string prefix)
        {
            if (_currentView == ViewMode.PreviewOnly)
            {
                SetViewMode(ViewMode.EditorOnly);
            }
            Editor.Focus();

            var text = Editor.Text;
            var start = Editor.SelectionStart;
            var length = Editor.SelectionLength;

            try
            {
                _suppressUpdate = true;

                if (length > 0)
                {
                    var selStart = start;
                    var selEnd = start + length;
                    var lineStart = text.LastIndexOf('\n', Math.Max(0, selStart - 1)) + 1;
                    var lineEnd = selEnd;
                    if (lineEnd < text.Length && text[lineEnd] != '\n')
                    {
                        var nextNewline = text.IndexOf('\n', lineEnd);
                        lineEnd = nextNewline >= 0 ? nextNewline : text.Length;
                    }

                    var beforeText = text.Substring(0, lineStart);
                    var selectedBlock = text.Substring(lineStart, lineEnd - lineStart);
                    var afterText = lineEnd < text.Length ? text.Substring(lineEnd) : "";

                    var prefixedLines = new List<string>();
                    foreach (var line in selectedBlock.Split('\n'))
                    {
                        if (!string.IsNullOrEmpty(line) && !line.StartsWith(prefix))
                            prefixedLines.Add(prefix + line);
                        else
                            prefixedLines.Add(line);
                    }

                    var joined = string.Join("\n", prefixedLines);
                    Editor.Text = beforeText + joined + afterText;
                    Editor.SelectionStart = lineStart;
                    Editor.SelectionLength = joined.Length;
                }
                else
                {
                    var lineStart = text.LastIndexOf('\n', Math.Max(0, start - 1)) + 1;
                    Editor.SelectionStart = lineStart;
                    Editor.SelectedText = prefix;
                    Editor.SelectionStart = start + prefix.Length;
                }
            }
            finally
            {
                _suppressUpdate = false;
                UpdatePreview();
                UpdateStatusBar();
            }
            Editor.Focus();
        }

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            if (_isModified)
            {
                var result = MessageBox.Show("Save changes?", "MDView", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) SaveFile_Click(this, new RoutedEventArgs());
                else if (result == MessageBoxResult.Cancel) return;
            }
            _currentFilePath = string.Empty;
            Editor.Text = string.Empty;
            _isModified = false;
            UpdateTitle();
            UpdateStatusBar();
            UpdatePreview();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Markdown Files (*.md)|*.md|Text Files (*.txt)|*.txt|All Files (*.*)|*.*", DefaultExt = ".md" };
            if (dialog.ShowDialog() == true)
            {
                OpenFileByPath(dialog.FileName);
            }
        }

        private async void OpenFileByPath(string path)
        {
            try
            {
                Editor.Text = await File.ReadAllTextAsync(path, Encoding.UTF8);
                _currentFilePath = path;
                _isModified = false;
                UpdateTitle();
                UpdatePreview();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath)) { SaveAsFile_Click(this, new RoutedEventArgs()); return; }
            try
            {
                await File.WriteAllTextAsync(_currentFilePath, Editor.Text, Encoding.UTF8);
                _isModified = false;
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Filter = "Markdown Files (*.md)|*.md|Text Files (*.txt)|*.txt|All Files (*.*)|*.*", DefaultExt = ".md", FileName = string.IsNullOrEmpty(_currentFilePath) ? "untitled.md" : Path.GetFileName(_currentFilePath) };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await File.WriteAllTextAsync(dialog.FileName, Editor.Text, Encoding.UTF8);
                    _currentFilePath = dialog.FileName;
                    _isModified = false;
                    UpdateTitle();
                    UpdateStatusBar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
        private void Undo_Click(object sender, RoutedEventArgs e) => Editor.Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => Editor.Redo();
        private void Cut_Click(object sender, RoutedEventArgs e) => Editor.Cut();
        private void Copy_Click(object sender, RoutedEventArgs e) => Editor.Copy();
        private void Paste_Click(object sender, RoutedEventArgs e) => Editor.Paste();
        private void SelectAll_Click(object sender, RoutedEventArgs e) => Editor.SelectAll();

        private void TogglePreview_Click(object sender, RoutedEventArgs e)
        {
            if (_currentView == ViewMode.PreviewOnly) SetViewMode(ViewMode.EditorOnly);
            else if (_currentView == ViewMode.EditorOnly) SetViewMode(ViewMode.Split);
            else SetViewMode(ViewMode.PreviewOnly);
        }

        private void ToggleToolbar_Click(object sender, RoutedEventArgs e)
        {
            ToolbarPanel.Visibility = ToolbarPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            Settings.ShowToolbar = ToolbarPanel.Visibility == Visibility.Visible;
            Settings.Save();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (_zoomLevel < 3.0) { _zoomLevel += 0.1; Editor.FontSize = 14 * _zoomLevel; Settings.Zoom = _zoomLevel; UpdatePreview(); UpdateStatusBar(); ScheduleSettingsSave(); }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (_zoomLevel > 0.5) { _zoomLevel -= 0.1; Editor.FontSize = 14 * _zoomLevel; Settings.Zoom = _zoomLevel; UpdatePreview(); UpdateStatusBar(); ScheduleSettingsSave(); }
        }

        private void ResetZoom_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel = 1.0; Editor.FontSize = 14; Settings.Zoom = _zoomLevel; UpdatePreview(); UpdateStatusBar(); ScheduleSettingsSave();
        }

        private void Bold_Click(object sender, RoutedEventArgs e) => InsertText("**", "**");
        private void Italic_Click(object sender, RoutedEventArgs e) => InsertText("*", "*");
        private void StrikeThrough_Click(object sender, RoutedEventArgs e) => InsertText("~~", "~~");
        private void InlineCode_Click(object sender, RoutedEventArgs e) => InsertText("`", "`");
        private void H1_Click(object sender, RoutedEventArgs e) => InsertLinePrefix("# ");
        private void H2_Click(object sender, RoutedEventArgs e) => InsertLinePrefix("## ");
        private void H3_Click(object sender, RoutedEventArgs e) => InsertLinePrefix("### ");
        private void BulletList_Click(object sender, RoutedEventArgs e) => InsertLinePrefix("- ");
        private void NumberedList_Click(object sender, RoutedEventArgs e) => InsertLinePrefix("1. ");
        private void Quote_Click(object sender, RoutedEventArgs e) => InsertLinePrefix("> ");
        private void CodeBlock_Click(object sender, RoutedEventArgs e) => InsertText("```\n", "\n```");
        
        private void HorizontalRule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _suppressUpdate = true;
                var start = Editor.SelectionStart;
                Editor.SelectedText = "\n---\n";
                Editor.SelectionStart = start + 5;
            }
            finally
            {
                _suppressUpdate = false;
                UpdatePreview();
                UpdateStatusBar();
            }
            Editor.Focus();
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new LinkDialog();
            if (dialog.ShowDialog() == true) InsertText($"[{dialog.LinkText}]({dialog.LinkUrl})");
        }

        private void Image_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ImageDialog();
            if (dialog.ShowDialog() == true) InsertText($"![{dialog.AltText}]({dialog.ImageUrl})");
        }

        private void Table_Click(object sender, RoutedEventArgs e)
        {
            InsertText("\n| Column 1 | Column 2 | Column 3 |\n|----------|----------|----------|\n| Cell 1   | Cell 2   | Cell 3   |\n| Cell 4   | Cell 5   | Cell 6   |\n");
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var version = "1.3";
            var buildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            MessageBox.Show(
                $"MDView v{version}\n\nBuild: {buildTime}\n\nMarkdown Editor (Fully Native, No WebView)\n\nView Modes:\n  Ctrl+1 - Preview Only\n  Ctrl+2 - Editor Only\n  Ctrl+3 - Split View\n\nCtrl+T - Toggle Theme\nCtrl+P - Cycle Views",
                "About MDView", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _previewCts?.Cancel();
            _previewCts?.Dispose();
            _settingsSaveCts?.Cancel();
            _settingsSaveCts?.Dispose();

            Settings.Save();
            if (_isModified)
            {
                var result = MessageBox.Show("Save changes before closing?", "MDView", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) SaveFile_Click(this, new RoutedEventArgs());
                else if (result == MessageBoxResult.Cancel) { e.Cancel = true; }
            }
            base.OnClosing(e);
        }
    }
}
