Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Diagnostics

Public Class AboutForm
    Inherits Form

    ' Animation variables for the sidebar
    Private WithEvents animTimer As New Timer()
    Private colorStep As Double = 0
    Private sidebar As BufferedPanel

    Public Sub New()
        ' --- Form Setup ---
        Me.Text = "About PixelGlow"
        Me.Size = New Size(800, 560)
        Me.Icon = ResourceLoader.GetIcon("ic_PixelGlow1.ico")
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.5F)

        ' 1. FORCE CENTER SCREEN
        Me.StartPosition = FormStartPosition.CenterScreen

        ' --- Layout ---
        ' --- Layout ---
        sidebar = New BufferedPanel() With {.Dock = DockStyle.Left, .Width = 200}
        ' Add paint handler for custom animated background
        AddHandler sidebar.Paint, AddressOf Sidebar_Paint

        Dim content As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(40, 30, 40, 30)}
        Dim footer As New Panel() With {.Dock = DockStyle.Bottom, .Height = 60, .BackColor = Color.FromArgb(245, 245, 245)}

        ' --- Sidebar Image (Using Global Resource Loader) ---
        Dim picLogo As New PictureBox() With {
            .SizeMode = PictureBoxSizeMode.Zoom,
            .Size = New Size(140, 140),
            .Location = New Point(30, 40),
            .BackColor = Color.Transparent
        }

        picLogo.Image = ResourceLoader.GetImage("Base_Led.png")
        sidebar.Controls.Add(picLogo)

        ' --- Footer Close Button ---
        Dim btnClose As New Button() With {.Text = "Close", .Width = 100, .Height = 35, .Location = New Point(670, 12), .Anchor = AnchorStyles.Right, .BackColor = Color.White, .FlatStyle = FlatStyle.Flat}
        AddHandler btnClose.Click, Sub(sender As Object, e As EventArgs) Me.Close()
        footer.Controls.Add(btnClose)

        ' --- Content Area ---
        Dim lblTitle As New Label() With {.Text = "PixelGlow", .AutoSize = True, .Font = New Font("Segoe UI Semibold", 28), .ForeColor = Color.FromArgb(0, 120, 215)}
        Dim lblVersion As New Label() With {.Text = $"Version {Application.ProductVersion}", .AutoSize = True, .Location = New Point(0, 60), .ForeColor = Color.Gray}

        Dim descText As String = "PixelGlow is a lightweight Windows ambient lighting application that runs silently from the system tray, turning your monitor setup into an immersive RGB backlight experience — no gaming ecosystem required, no subscriptions, no bloat." & vbCrLf & vbCrLf &
                                 "At its core, PixelGlow features a high-performance screen color detection engine that continuously samples the edges of any selected monitor and translates the dominant colors into real-time LED lighting commands."

        Dim lblDesc As New Label() With {
            .Text = descText, .Size = New Size(520, 135), .Location = New Point(0, 95), .Font = New Font("Segoe UI Semilight", 10.0F)
        }

        Dim lblCredits As New Label() With {.Text = "Creator: Nikos Georgousis" & vbCrLf & "Company: Hand Water Pump", .AutoSize = True, .Location = New Point(0, 240), .Font = New Font("Segoe UI Semibold", 10)}

        Dim lnkWeb = CreateLinkLabel("http://www.georgousis.info", "http://www.georgousis.info", 290)
        Dim lnkGit = CreateLinkLabel("GitHub Project Page", "https://github.com/limbo666/PixelGlow", 315)

        Dim txtLicense As New TextBox() With {
            .Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Vertical, .Size = New Size(520, 65), .Location = New Point(0, 350),
            .Text = "LICENSE: Apache 2.0 License + Commons Clause." & vbCrLf & "You may freely use, modify, and distribute this software for non-commercial purposes. Selling or commercializing this software or its derivatives is strictly prohibited. All distributions must credit the original creator.",
            .BackColor = Color.FromArgb(250, 250, 250), .Font = New Font("Consolas", 8.5F)
        }

        Dim btnDonate As New Button() With {.Text = "Support Development (PayPal)", .Size = New Size(220, 45), .Location = New Point(0, 430), .BackColor = Color.FromArgb(0, 112, 186), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .Cursor = Cursors.Hand}
        AddHandler btnDonate.Click, Sub(sender As Object, e As EventArgs) Process.Start(New ProcessStartInfo("https://paypal.me/NikosG") With {.UseShellExecute = True})

        content.Controls.AddRange({lblTitle, lblVersion, lblDesc, lblCredits, lnkWeb, lnkGit, txtLicense, btnDonate})
        Me.Controls.AddRange({content, sidebar, footer})

        ' 2. START AMBIENT ANIMATION (30 FPS)
        animTimer.Interval = 33
        animTimer.Start()
    End Sub

    ' 3. THE ANIMATION LOOP
    Private Sub animTimer_Tick(sender As Object, e As EventArgs) Handles animTimer.Tick
        colorStep += 0.05
        If sidebar IsNot Nothing Then sidebar.Invalidate() ' Request sidebar redraw
    End Sub

    ' 4. THE GRADIENT PAINTER
    Private Sub Sidebar_Paint(sender As Object, e As PaintEventArgs)
        ' Create smooth shifting RGB values using Sine waves
        Dim r1 As Integer = CInt(230 + 15 * Math.Sin(colorStep))
        Dim g1 As Integer = CInt(235 + 15 * Math.Sin(colorStep * 0.8))
        Dim b1 As Integer = CInt(245 + 10 * Math.Sin(colorStep * 0.5))

        Dim r2 As Integer = CInt(210 + 20 * Math.Sin(colorStep * 0.7))
        Dim g2 As Integer = CInt(225 + 15 * Math.Sin(colorStep * 0.9))
        Dim b2 As Integer = CInt(240 + 15 * Math.Sin(colorStep * 1.1))

        Dim color1 As Color = Color.FromArgb(Math.Min(255, Math.Max(0, r1)), Math.Min(255, Math.Max(0, g1)), Math.Min(255, Math.Max(0, b1)))
        Dim color2 As Color = Color.FromArgb(Math.Min(255, Math.Max(0, r2)), Math.Min(255, Math.Max(0, g2)), Math.Min(255, Math.Max(0, b2)))

        ' Draw a smooth 45-degree angle gradient that changes color dynamically
        Using brush As New LinearGradientBrush(sidebar.ClientRectangle, color1, color2, 45.0F)
            e.Graphics.FillRectangle(brush, sidebar.ClientRectangle)
        End Using
    End Sub

    Private Function CreateLinkLabel(text As String, url As String, y As Integer) As LinkLabel
        Dim lnk As New LinkLabel() With {.Text = text, .AutoSize = True, .Location = New Point(0, y), .LinkColor = Color.FromArgb(0, 120, 215)}
        AddHandler lnk.LinkClicked, Sub(sender As Object, e As EventArgs) Process.Start(New ProcessStartInfo(url) With {.UseShellExecute = True})
        Return lnk
    End Function

    ' Stop the timer to free resources when the form closes
    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        animTimer.Stop()
        animTimer.Dispose()
        MyBase.OnFormClosed(e)
    End Sub
End Class

' --- High-Performance Double Buffered Panel ---
Public Class BufferedPanel
    Inherits Panel
    Public Sub New()
        Me.DoubleBuffered = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
        Me.UpdateStyles()
    End Sub
End Class