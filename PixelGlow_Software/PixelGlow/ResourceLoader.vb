Imports System.Drawing
Imports System.IO
Imports System.Reflection

Public Module ResourceLoader

    ' Load an Image anywhere in the app: Dim img = ResourceLoader.GetImage("im_PixelGlow1.png")
    Public Function GetImage(fileName As String) As Image
        Try
            Dim asm As Assembly = Assembly.GetExecutingAssembly()
            Dim resourceName As String = asm.GetManifestResourceNames().FirstOrDefault(Function(n) n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))

            If Not String.IsNullOrEmpty(resourceName) Then
                Using stream As Stream = asm.GetManifestResourceStream(resourceName)
                    If stream IsNot Nothing Then Return Image.FromStream(stream)
                End Using
            End If
        Catch
        End Try
        Return Nothing
    End Function

    ' Load an Icon anywhere in the app: Dim ico = ResourceLoader.GetIcon("ic_PixelGlow1.ico")
    Public Function GetIcon(fileName As String) As Icon
        Try
            Dim asm As Assembly = Assembly.GetExecutingAssembly()
            Dim resourceName As String = asm.GetManifestResourceNames().FirstOrDefault(Function(n) n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))

            If Not String.IsNullOrEmpty(resourceName) Then
                Using stream As Stream = asm.GetManifestResourceStream(resourceName)
                    If stream IsNot Nothing Then Return New Icon(stream)
                End Using
            End If
        Catch
        End Try
        Return Nothing
    End Function

End Module