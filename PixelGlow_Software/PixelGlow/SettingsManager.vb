' SettingsManager.vb
Imports System.IO
Imports System.Text.Json

Public Class SettingsManager
    Private Shared _filePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.json")
    Public Shared Property Current As New AppSettings()

    Public Shared Sub Load()
        Try
            If File.Exists(_filePath) Then
                Dim json = File.ReadAllText(_filePath)
                Current = JsonSerializer.Deserialize(Of AppSettings)(json)
            Else
                Save() ' Create default file if missing
            End If
        Catch ex As Exception
            ' Fail-safe: load defaults on error
            Current = New AppSettings()
        End Try
    End Sub

    Public Shared Sub Save()
        Try
            Dim options As New JsonSerializerOptions With {.WriteIndented = True}
            Dim json = JsonSerializer.Serialize(Current, options)
            File.WriteAllText(_filePath, json)
        Catch
            ' Production-ready apps should handle write-permission issues here
        End Try
    End Sub
End Class