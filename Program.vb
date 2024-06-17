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

    Function ConvertImage(filename As String) As Integer
        Console.WriteLine($"Converting {filename}...")
        Dim output As String = String.Empty

        Dim image As Image = GetImage(filename)
        If image Is Nothing Then
            Console.WriteLine($"Error: Could not load image {filename}")
            Return 0
        End If

        If config("grayscale") = 1 Then
            image = ConvertToGrayscale(image)
        End If

        ' Calculate new height to maintain aspect ratio
        Dim newWidth As Integer = config("imagewidth")
        Dim aspectRatio As Double = CDbl(image.Height) / CDbl(image.Width)
        Dim newHeight As Integer = CInt(newWidth * aspectRatio * 0.65) ' Adjust for character aspect ratio

        ' Debugging: Print out new dimensions
        Console.WriteLine($"Resizing image to: {newWidth}x{newHeight}")

        image = ResizeImage(image, newWidth, newHeight)

        output &= $"<HTML>{Environment.NewLine}<HEAD>{Environment.NewLine}<TITLE>{Path.GetFileName(filename)}</TITLE>{Environment.NewLine}</HEAD>{Environment.NewLine}<BODY BGCOLOR={config("bgcolor")}>{Environment.NewLine}<center><table align=""center"" cellpadding=""10"">{Environment.NewLine}<tr>{Environment.NewLine}<td><font size={config("fontsize")}><pre><br>{Environment.NewLine}"

        Dim width As Integer = image.Width
        Dim height As Integer = image.Height

        ' Debugging: Print out image dimensions
        Console.WriteLine($"Image dimensions: {width}x{height}")

        Dim oldcolours As Color = Color.FromArgb(-1)

        For y As Integer = 0 To height - 1
            Dim line As String = ""
            For x As Integer = 0 To width - 1
                Dim pixelColor As Color = CType(image, Bitmap).GetPixel(x, y)
                If Not pixelColor.Equals(oldcolours) Then
                    If x = 0 Then
                        line &= $"<font color=#{pixelColor.R:X2}{pixelColor.G:X2}{pixelColor.B:X2}>{NextCharacter()}"
                    Else
                        line &= $"</font><font color=#{pixelColor.R:X2}{pixelColor.G:X2}{pixelColor.B:X2}>{NextCharacter()}"
                    End If
                Else
                    line &= NextCharacter()
                End If
                oldcolours = pixelColor
            Next
            oldcolours = Color.FromArgb(-1)
            line &= "</font><br>"
            output &= line
        Next

        output &= $"{Environment.NewLine}</pre></font></td>{Environment.NewLine}</tr>{Environment.NewLine}</table></center>{Environment.NewLine}</BODY></HTML>{Environment.NewLine}"

        Try
            File.WriteAllText($"./HTML/{Path.GetFileNameWithoutExtension(filename)}.html", output)
            Console.WriteLine($"Successfully converted {filename}")
        Catch ex As Exception
            Console.WriteLine($"Error writing HTML file for {filename}: {ex.Message}")
            Return 0
        End Try

        Return 1
    End Function

    Function GetImage(url As String) As Image
        ' Loads an image from the specified file path
        Try
            Return Image.FromFile(url)
        Catch ex As Exception
            Console.WriteLine($"Error loading image {url}: {ex.Message}")
            Return Nothing
        End Try
    End Function

    Function ConvertToGrayscale(original As Image) As Image
        ' Converts the given image to grayscale
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

    Function ResizeImage(image As Image, width As Integer, height As Integer) As Image
        ' Resizes the given image to the specified dimensions
        Dim newBitmap As New Bitmap(width, height)
        Using g As Graphics = Graphics.FromImage(newBitmap)
            g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
            g.DrawImage(image, 0, 0, width, height)
        End Using
        Return newBitmap
    End Function

End Module
