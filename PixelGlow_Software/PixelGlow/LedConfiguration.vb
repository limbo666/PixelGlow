' LedConfiguration.vb
Public Class LedConfiguration
    ' Detection Grid Dimensions
    Public Property GridCols As Integer = 16
    Public Property GridRows As Integer = 9

    ' Physical counts for your specific LED strip setup
    Public Property TopCount As Integer = 16
    Public Property BottomCount As Integer = 16
    Public Property LeftCount As Integer = 9
    Public Property RightCount As Integer = 9

    Public ReadOnly Property TotalLeds As Integer
        Get
            Return TopCount + RightCount + BottomCount + LeftCount
        End Get
    End Property
End Class