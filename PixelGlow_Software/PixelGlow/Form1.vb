Imports System.Drawing
Imports System.Windows.Forms

Public Class Form1
    Private _isExiting As Boolean = False
    Private _mimicColorMode As Integer = 0 ' 0=Normal, 1=Red, 2=Green, 3=Blue, 4=White
    Private _engine As AmbientEngine
    Private _mnuControlHw As ToolStripMenuItem ' Tracks the tray menu checkmark
    Private _uiTimer As Timer
    Private _trayIcon As NotifyIcon
    Private _settingsForm As SettingsForm

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        Me.Name = "MainMimicForm"

        SettingsManager.Load()
        RegistryHelper.LoadWindowBounds(Me)

        Me.DoubleBuffered = True
        Me.Text = "PixelGlow Mimic Form"
        Me.BackColor = Color.Black

        ' --- Form Context Menu ---
        Dim mimicMenu As New ContextMenuStrip()
        mimicMenu.Items.Add("Settings", Nothing, Sub() ShowSettings())
        mimicMenu.Items.Add("About", Nothing, Sub()
                                                  Dim frm As New AboutForm()
                                                  frm.ShowDialog()
                                              End Sub)
        mimicMenu.Items.Add("-")
        mimicMenu.Items.Add("Exit", Nothing, Sub()
                                                 _isExiting = True
                                                 Me.Close()
                                             End Sub)
        Me.ContextMenuStrip = mimicMenu

        InitTray()

        ' Start hidden if the setting is enabled
        If SettingsManager.Current.StartInTray Then
            Me.WindowState = FormWindowState.Minimized
            Me.ShowInTaskbar = False
            Me.Visible = False
        End If

        _engine = New AmbientEngine()
        _engine.Start()

        _uiTimer = New Timer() With {.Interval = 33}
        AddHandler _uiTimer.Tick, Sub() Me.Invalidate()
        _uiTimer.Start()
    End Sub

    Private Sub InitTray()
        Dim trayMenu As New ContextMenuStrip()

        trayMenu.Items.Add("Show/Hide Mimic", Nothing, Sub() ToggleMimicVisibility())
        trayMenu.Items.Add("Settings", Nothing, Sub() ShowSettings())
        trayMenu.Items.Add("-")

        ' --- HARDWARE TOGGLE ---
        _mnuControlHw = New ToolStripMenuItem("Control Hardware") With {
            .CheckOnClick = True,
            .Checked = SettingsManager.Current.ControlHardware
        }
        AddHandler _mnuControlHw.CheckedChanged, Sub(sender As Object, e As EventArgs)
                                                     ' Prevent double-firing if updated via the Settings menu
                                                     If SettingsManager.Current.ControlHardware <> _mnuControlHw.Checked Then
                                                         SettingsManager.Current.ControlHardware = _mnuControlHw.Checked
                                                         SettingsManager.Save()
                                                         _engine?.ReloadSettings()
                                                         UpdateTrayIcon()
                                                     End If
                                                 End Sub
        trayMenu.Items.Add(_mnuControlHw)
        trayMenu.Items.Add("-")

        ' --- SAFE ABOUT BUTTON ---
        Dim aboutItem As New ToolStripMenuItem("About")
        AddHandler aboutItem.Click, Sub(sender As Object, e As EventArgs)
                                        Dim frm As New AboutForm()
                                        frm.ShowDialog()
                                    End Sub
        trayMenu.Items.Add(aboutItem)
        ' -------------------------

        trayMenu.Items.Add("-")
        trayMenu.Items.Add("Exit", Nothing, Sub()
                                                _isExiting = True
                                                Me.Close()
                                            End Sub)

        Me.Icon = ResourceLoader.GetIcon("ic_PixelGlow1.ico") ' Primary Form Icon

        ' Initialize with the correct icon based on current settings
        Dim initialIcon = If(SettingsManager.Current.ControlHardware, "ic_PixelGlow0b.ico", "ic_PixelGlow0a.ico")

        _trayIcon = New NotifyIcon() With {
            .Icon = ResourceLoader.GetIcon(initialIcon),
            .ContextMenuStrip = trayMenu,
            .Text = "PixelGlow Ambient Light",
            .Visible = True
        }
        AddHandler _trayIcon.MouseDoubleClick, Sub() ToggleMimicVisibility()
    End Sub
    Private Sub UpdateTrayIcon()
        If _trayIcon IsNot Nothing Then
            Dim iconName = If(SettingsManager.Current.ControlHardware, "ic_PixelGlow0b.ico", "ic_PixelGlow0a.ico")
            _trayIcon.Icon = ResourceLoader.GetIcon(iconName)
        End If
    End Sub
    Private Sub ToggleMimicVisibility()
        If Me.Visible AndAlso Me.WindowState <> FormWindowState.Minimized Then
            Me.Hide()
        Else
            Me.Show()
            Me.WindowState = FormWindowState.Normal
            Me.BringToFront()
        End If
    End Sub

    Private Sub ShowSettings()
        If _settingsForm IsNot Nothing AndAlso Not _settingsForm.IsDisposed Then
            _settingsForm.BringToFront()
            _settingsForm.Activate()
            Return
        End If

        _settingsForm = New SettingsForm()

        ' Listen for the Apply/OK buttons to refresh the UI
        AddHandler _settingsForm.SettingsApplied, Sub(sender, ev)
                                                      ' Sync the tray menu checkmark to match the saved setting
                                                      If _mnuControlHw IsNot Nothing Then
                                                          _mnuControlHw.Checked = SettingsManager.Current.ControlHardware
                                                      End If

                                                      _engine.ReloadSettings()
                                                      UpdateTrayIcon() ' Sync the tray icon
                                                      Me.Invalidate()
                                                  End Sub

        _settingsForm.Show()
    End Sub

    Protected Overrides Sub OnMouseDoubleClick(e As MouseEventArgs)
        MyBase.OnMouseDoubleClick(e)

        ' Check if CTRL key is held down
        If ModifierKeys.HasFlag(Keys.Control) Then
            _mimicColorMode += 1
            If _mimicColorMode > 4 Then _mimicColorMode = 0
        Else
            ' Simple double click exits the color mode immediately
            _mimicColorMode = 0
        End If

        Me.Invalidate() ' Force immediate redraw
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        ' --- OVERRIDE: Mimic Color Cycle Mode ---
        If _mimicColorMode > 0 Then
            Dim c As Color
            Select Case _mimicColorMode
                Case 1 : c = Color.Red
                Case 2 : c = Color.Green
                Case 3 : c = Color.Blue
                Case 4 : c = Color.White
            End Select
            e.Graphics.Clear(c)
            Return
        End If

        Dim zones = _engine?.CurrentZones
        If zones Is Nothing Then Exit Sub

        Dim cols = _engine.GridCols
        Dim rows = _engine.GridRows
        Dim cellW As Single = Me.ClientSize.Width / cols
        Dim cellH As Single = Me.ClientSize.Height / rows

        For y As Integer = 0 To rows - 1
            For x As Integer = 0 To cols - 1
                Using br As New SolidBrush(zones(x, y))
                    e.Graphics.FillRectangle(br, x * cellW, y * cellH, cellW, cellH)
                End Using

                ' Draw grid lines only if enabled in settings
                If SettingsManager.Current.ShowDetectionGrid Then
                    e.Graphics.DrawRectangle(Pens.DimGray, x * cellW, y * cellH, cellW, cellH)
                End If
            Next
        Next
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        ' If the user just clicked the X, cancel the close and hide the form instead
        If Not _isExiting Then
            e.Cancel = True
            Me.Hide()
            Return
        End If

        ' Otherwise, proceed with full shutdown
        RegistryHelper.SaveWindowBounds(Me)
        _engine?.ReleaseAndStop()
        _trayIcon?.Dispose()
        If _settingsForm IsNot Nothing AndAlso Not _settingsForm.IsDisposed Then _settingsForm.Close()
        MyBase.OnFormClosing(e)
    End Sub
End Class