
Imports System
Imports Microsoft.Win32
Imports System.Drawing

Imports System.Windows.Forms
Public Class RegistryHelper
    Private Const ROOT_PATH As String = "Software\PixelGlow\"
    Public Shared Sub SetStartup(enabled As Boolean)
        Try
            Dim path As String = "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
            Dim key As RegistryKey = Registry.CurrentUser.OpenSubKey(path, True)
            If enabled Then
                ' Set the value to the path of your compiled PixelGlow.exe
                key.SetValue("PixelGlow", Application.ExecutablePath)
            Else
                ' Remove the entry if disabled
                key.DeleteValue("PixelGlow", False)
            End If
        Catch ex As Exception
            ' Fail silently or log if permissions are an issue
        End Try
    End Sub


    Public Shared Sub SaveWindowBounds(frm As Form)
        If frm.WindowState <> FormWindowState.Normal Then Return

        ' Use the Form's name to create a unique subkey
        Using key = Registry.CurrentUser.CreateSubKey(ROOT_PATH & frm.Name)
            key.SetValue("X", frm.Location.X)
            key.SetValue("Y", frm.Location.Y)
            key.SetValue("W", frm.Size.Width)
            key.SetValue("H", frm.Size.Height)
        End Using
    End Sub

    Public Shared Sub LoadWindowBounds(frm As Form)
        Using key = Registry.CurrentUser.OpenSubKey(ROOT_PATH & frm.Name)
            If key IsNot Nothing Then
                Dim x = CInt(key.GetValue("X", frm.Location.X))
                Dim y = CInt(key.GetValue("Y", frm.Location.Y))
                Dim w = CInt(key.GetValue("W", frm.Size.Width))
                Dim h = CInt(key.GetValue("H", frm.Size.Height))

                Dim savedRect = New Rectangle(x, y, w, h)
                Dim isVisible = False
                For Each scr In Screen.AllScreens
                    If scr.WorkingArea.IntersectsWith(savedRect) Then
                        isVisible = True
                        Exit For
                    End If
                Next

                If isVisible Then
                    frm.StartPosition = FormStartPosition.Manual
                    frm.DesktopBounds = savedRect
                End If
            End If
        End Using
    End Sub
End Class