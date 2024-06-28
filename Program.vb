Imports System
Imports System.Drawing
Imports System.IO
Imports System.Text

Module TextImageConverter

    ' Configuration settings
    Dim config As New Dictionary(Of String, Object) From {
        {"texttype", "sequence"},
        {"phrase", New String() {"0", "1"}},
        {"bgcolor", "BLACK"},
        {"fontsize", "-3"},
        {"grayscale", 0},
        {"imagewidth", 150},
        {"suffix", ".jpg"}
    }

    Dim textType_count As Integer = -1
    Dim timestart As DateTime = DateTime.Now
    Dim random As New Random()
    Dim phrase As String()

    Sub Main()
        ' Entry point of the program
        Console.WriteLine(":: TEXT-IMAGE ::")
        Console.WriteLine("Starting conversion...")

        ' Check if the configuration file exists
        If Not File.Exists("configuration.ini") Then
            Console.WriteLine("Error: No configuration.ini found!")
            Return
        End If

        ' Read configuration from the file
        Dim configuration As String = File.ReadAllText("configuration.ini")
        ParseConfiguration(configuration)

        ' Print loaded configuration settings
        Console.WriteLine("Loaded configuration:")
        For Each key In config.Keys
            Console.WriteLine($"{key} = {config(key)}")
        Next

        ' Check if the images directory exists
        If Not Directory.Exists("./images") Then
            Console.WriteLine("Error: No images directory found!")
            Return
        End If

        ' Create HTML directory if it doesn't exist
        Directory.CreateDirectory("./HTML")

        Dim converted As Integer = 0
        Dim files = Directory.GetFiles("./images/")
        For Each file As String In files
            ' Process each image file
            Console.WriteLine($"Found file: {file}")
            If file.ToLower().EndsWith(config("suffix").ToString().ToLower()) Then
                converted += ConvertImage(file)
            Else
                Console.WriteLine($"Skipped file: {file}, does not match suffix: {config("suffix")}")
            End If
        Next

        ' Print summary after conversion
        Console.WriteLine($"Mission complete! Converted {converted} images in {(DateTime.Now - timestart).TotalSeconds} seconds.")
    End Sub

    Sub ParseConfiguration(configuration As String)
        ' Parse configuration settings from the provided string
        Console.WriteLine("Parsing configuration...")
        Dim lines = configuration.Split({Environment.NewLine}, StringSplitOptions.None)
        For Each line As String In lines
            ' Skip empty lines or comments
            If line.Length < 1 OrElse line.StartsWith("#") Then Continue For
            Dim parts = line.Split("="c)
            If parts.Length = 2 Then
                ' Store configuration key-value pairs
                config(parts(0).Trim()) = parts(1).Trim()
                Console.WriteLine($"Config {parts(0).Trim()} = {parts(1).Trim()}")
            End If
        Next

        ' Convert specific configuration values to the correct types
        config("grayscale") = Convert.ToInt32(config("grayscale"))
        config("imagewidth") = Convert.ToInt32(config("imagewidth"))
        phrase = config("phrase").ToString().ToCharArray().Select(Function(c) c.ToString()).ToArray()
    End Sub

    Function NextCharacter() As String
        ' Returns the next character based on configured text type
        If phrase.Length = 1 Then Return phrase(0)

        If config("texttype").ToString().ToLower() = "random" Then
            ' Return a random character from the list
            Return phrase(random.Next(phrase.Length))
        ElseIf config("texttype").ToString().ToLower() = "sequence" Then
            ' Return characters sequentially
            textType_count += 1
            If textType_count >= phrase.Length Then
                textType_count = 0
            End If
            Return phrase(textType_count)
        End If
        Return String.Empty
    End Function

    Sub LogToFile(message As String)
        ' Define your log file path
        Dim logFilePath As String = "./log.txt"

        ' Append the message to the log file
        Try
            Using writer As New StreamWriter(logFilePath, True)
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - {message}")
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error writing to log file: {ex.Message}")
        End Try
    End Sub

    Function AdjustBrightnessContrast(image As Image, brightness As Single, contrast As Single, saturation As Single) As Image
        Dim tempImage As New Bitmap(image.Width, image.Height)
        Using g As Graphics = Graphics.FromImage(tempImage)
            Dim colorMatrix As New Imaging.ColorMatrix(New Single()() {
                New Single() {contrast, 0, 0, 0, 0},
                New Single() {0, contrast, 0, 0, 0},
                New Single() {0, 0, contrast, 0, 0},
                New Single() {0, 0, 0, 1, 0},
                New Single() {0, 0, 0, 0, 1}
            })
            Using attributes As New Imaging.ImageAttributes()
                attributes.SetColorMatrix(colorMatrix)
                g.DrawImage(image, New Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes)
            End Using
        End Using

        ' Enhance color saturation
        Dim hsvImage As New Bitmap(tempImage.Width, tempImage.Height)
        For y As Integer = 0 To tempImage.Height - 1
            For x As Integer = 0 To tempImage.Width - 1
                Dim pixelColor As Color = tempImage.GetPixel(x, y)
                Dim h As Single, s As Single, v As Single
                ColorToHSV(pixelColor, h, s, v)
                s *= saturation
                If s > 1 Then s = 1
                Dim newColor As Color = ColorFromHSV(h, s, v)
                hsvImage.SetPixel(x, y, newColor)
            Next
        Next

        Return hsvImage
    End Function

    ' Converts a Color from RGB to HSV representation.
    Sub ColorToHSV(color As Color, ByRef hue As Single, ByRef saturation As Single, ByRef value As Single)
        ' Determine the maximum and minimum values of RGB components.
        Dim max As Single = Math.Max(color.R, Math.Max(color.G, color.B))
        Dim min As Single = Math.Min(color.R, Math.Min(color.G, color.B))

        ' Calculate the hue value (normalized to [0, 1]).
        hue = color.GetHue() / 360.0F

        ' Calculate the saturation value.
        If max = 0 Then
            saturation = 0
        Else
            saturation = 1.0F - (1.0F * min / max)
        End If

        ' Calculate the value (brightness) value.
        value = max / 255.0F
    End Sub

    ' Converts HSV values back to a Color in RGB representation.
    Function ColorFromHSV(hue As Single, saturation As Single, value As Single) As Color
        ' Convert hue from [0, 1] to degrees [0, 360].
        Dim h As Integer = CInt(hue * 360)
        Dim s As Single = saturation
        Dim v As Single = value

        ' Calculate chroma (color intensity).
        Dim c As Single = v * s
        ' Calculate intermediate value x.
        Dim x As Single = c * (1 - Math.Abs((h / 60) Mod 2 - 1))
        ' Calculate brightness modifier m.
        Dim m As Single = v - c

        ' Initialize RGB components.
        Dim r As Single, g As Single, b As Single

        ' Determine RGB values based on hue range.
        If h >= 0 And h < 60 Then
            r = c
            g = x
            b = 0
        ElseIf h >= 60 And h < 120 Then
            r = x
            g = c
            b = 0
        ElseIf h >= 120 And h < 180 Then
            r = 0
            g = c
            b = x
        ElseIf h >= 180 And h < 240 Then
            r = 0
            g = x
            b = c
        ElseIf h >= 240 And h < 300 Then
            r = x
            g = 0
            b = c
        Else
            r = c
            g = 0
            b = x
        End If

        ' Convert the calculated RGB values to a Color object.
        Return Color.FromArgb(CInt((r + m) * 255), CInt((g + m) * 255), CInt((b + m) * 255))
    End Function

    Function ConvertImage(filename As String) As Integer
        Console.WriteLine($"Converting {filename}...")
        Dim output As String = String.Empty

        Dim image As Image = GetImage(filename)
        If image Is Nothing Then
            Console.WriteLine($"Error: Could not load image {filename}")
            Return 0
        End If

        ' Adjust brightness, contrast, and saturation
        image = AdjustBrightnessContrast(image, 0.0F, 1.2F, 1.5F)

        If config("grayscale") = 1 Then
            image = ConvertToGrayscale(image)
        End If

        Dim height As Integer = image.Height
        Dim width As Integer = image.Width
        If width > config("imagewidth") Then
            height = CInt(image.Height * config("imagewidth") / image.Width)
            width = CInt(config("imagewidth"))
            image = ResizeImage(image, width, height)
        End If

        Dim oldColors As Color = Color.FromArgb(-1)
        output &= "<html><head><style>body{font-family:Courier;background-color:" & config("bgcolor") & ";color:white;font-size:" & config("fontsize") & ";}</style></head><body>"

        For y As Integer = 0 To height - 1
            Dim line As String = String.Empty
            For x As Integer = 0 To width - 1
                Dim pixelColor As Color = CType(image, Bitmap).GetPixel(x, y)
                If Not pixelColor.Equals(oldColors) Then
                    If x = 0 Then
                        line &= $"<font color=#{pixelColor.R:X2}{pixelColor.G:X2}{pixelColor.B:X2}>{NextCharacter()}"
                    Else
                        line &= $"</font><font color=#{pixelColor.R:X2}{pixelColor.G:X2}{pixelColor.B:X2}>{NextCharacter()}"
                    End If
                Else
                    line &= NextCharacter()
                End If
                oldColors = pixelColor
            Next
            oldColors = Color.FromArgb(-1)
            line &= "</font><br>"
            output &= line
        Next

        output &= "</body></html>"

        Dim outputFilename As String = $"./HTML/{Path.GetFileNameWithoutExtension(filename)}.html"
        File.WriteAllText(outputFilename, output, Encoding.UTF8)
        Console.WriteLine($"Saved {outputFilename}")
        Return 1
    End Function

    Function ResizeImage(image As Image, width As Integer, height As Integer) As Image
        ' Resize image to the specified width and height
        Dim newImage As New Bitmap(width, height)
        Using g As Graphics = Graphics.FromImage(newImage)
            g.DrawImage(image, 0, 0, width, height)
        End Using
        Return newImage
    End Function

    Function GetImage(filename As String) As Image
        ' Load image from file
        Try
            Return Image.FromFile(filename)
        Catch ex As Exception
            LogToFile($"Error loading image: {ex.Message}")
            Return Nothing
        End Try
    End Function

    Function ConvertToGrayscale(original As Image) As Image
        Dim newBitmap As New Bitmap(original.Width, original.Height)
        Using g As Graphics = Graphics.FromImage(newBitmap)
            Dim colorMatrix As New Imaging.ColorMatrix(New Single()() {
                New Single() {0.3, 0.3, 0.3, 0, 0},
                New Single() {0.59, 0.59, 0.59, 0, 0},
                New Single() {0.11, 0.11, 0.11, 0, 0},
                New Single() {0, 0, 0, 1, 0},
                New Single() {0, 0, 0, 0, 1}
            })
            Using attributes As New Imaging.ImageAttributes()
                attributes.SetColorMatrix(colorMatrix)
                g.DrawImage(original, New Rectangle(0, 0, original.Width, original.Height), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes)
            End Using
        End Using
        Return newBitmap
    End Function

End Module
