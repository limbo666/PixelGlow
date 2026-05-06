' Logger.vb
Imports System.IO

Public Class Logger
    Private Shared ReadOnly _lock As New Object()
    Private Shared ReadOnly _logPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PixelGlow.log")

    Public Shared Sub Info(message As String)
        ' Only log if the user has enabled it in Settings.json
        If Not SettingsManager.Current.LoggingEnabled Then Return

        WriteToFile("INFO", message)
    End Sub

    Public Shared Sub [Error](message As String, ex As Exception)
        ' Errors are always logged regardless of the global setting
        Dim fullMsg = If(ex IsNot Nothing, $"{message} | EX: {ex.Message}", message)
        WriteToFile("ERROR", fullMsg)
    End Sub

    Private Shared Sub WriteToFile(level As String, message As String)
        Try
            SyncLock _lock
                Dim logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}"
                Using sw As New StreamWriter(_logPath, True)
                    sw.WriteLine(logLine)
                End Using
            End SyncLock
        Catch
            ' Fail-safe: If logging fails, we don't want to crash the main engine
        End Try
    End Sub
End Class