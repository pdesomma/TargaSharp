using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TargaSharp;
using TargaSharp.Drawing;

namespace TestWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Fixtures now live in .tests\Fixtures\ (see TASK M12), but the csproj's <None Include> links
        // them back under Examples\ in the build output, so this relative path is unchanged.
        string[] Files = Directory.GetFiles(@"Examples\", "*.tga", SearchOption.AllDirectories);
        TgaFile? T;


        public MainWindow()
        {
            InitializeComponent();
            foreach(var file in Files.Select(x => Path.GetFileName(x)))
            {
                listBox1.Items.Add(file);
            }
        }

        private void BmpTgaBmpButton_Click(object sender, RoutedEventArgs e)
        {
            if (T is null) return;

            using Bitmap bitmap = T.ToBitmap();
            T = TgaDrawing.FromBitmap(bitmap);
            ShowTga();
        }

        private void ListBoxSelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;

            string tgaFilePath = Files[listBox1.SelectedIndex];
            if (File.Exists(tgaFilePath))
            {
                T = new TgaFile(tgaFilePath);
                //T.UpdatePostageStampImage();
                ShowTga();
            }
        }

        private void SaveSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (T == null)
                return;

            string OutDir = @"D:\TGA\";
            if (!Directory.Exists(OutDir))
                Directory.CreateDirectory(OutDir);

            T.Save(Path.Combine(OutDir, Path.GetFileName("___T.tga")));
        }
        private void SaveAllButton_Click(object sender, RoutedEventArgs e)
        {
            string OutDir = @"D:\TGA\";
            if (!Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);

            for (int i = 0; i < Files.Length; i++) new TgaFile(Files[i]).Save(Path.Combine(OutDir, Path.GetFileName(Files[i])));
        }





        private void ShowTga()
        {
            if (T is null) return;

            using Bitmap converted = T.ToBitmap();
            // WPF's BMP decoder cannot show Format16bppGrayScale; downsample to 8bpp indexed for display.
            using Bitmap bitmap = converted.PixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppGrayScale
                ? Gray16To8bppIndexed(converted)
                : converted;

            richTextBox1.Document.Blocks.Clear();
            richTextBox1.AppendText(TgaJson.Serialize(T));

            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                var bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                pictureBox1.Source = bitmapimage;
            }
        }



        public Bitmap Gray16To8bppIndexed(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format16bppGrayScale)
                throw new BadImageFormatException();

            // Copy row by row: GDI+ pads rows to 4 bytes, so a flat Width * Height copy shears odd widths.
            int width = bitmap.Width, height = bitmap.Height;
            Rectangle Re = new Rectangle(0, 0, width, height);

            byte[] ImageData = new byte[width * height * 2];
            BitmapData BmpData = bitmap.LockBits(Re, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            for (int y = 0; y < height; y++)
                Marshal.Copy(BmpData.Scan0 + (nint)y * BmpData.Stride, ImageData, y * width * 2, width * 2);
            bitmap.UnlockBits(BmpData);

            byte[] ImageData2 = new byte[width * height];
            for (int i = 0; i < ImageData2.Length; i++)
                ImageData2[i] = ImageData[i * 2 + 1];

            Bitmap BmpOut = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            BmpData = BmpOut.LockBits(Re, ImageLockMode.WriteOnly, BmpOut.PixelFormat);
            for (int y = 0; y < height; y++)
                Marshal.Copy(ImageData2, y * width, BmpData.Scan0 + (nint)y * BmpData.Stride, width);
            BmpOut.UnlockBits(BmpData);

            ColorPalette GrayPalette = BmpOut.Palette;
            System.Drawing.Color[] GrayColors = GrayPalette.Entries;
            for (int i = 0; i < GrayColors.Length; i++)
            {
                GrayColors[i] = System.Drawing.Color.FromArgb(i, i, i);
            }   
            BmpOut.Palette = GrayPalette;

            return BmpOut;
        }
    }
}