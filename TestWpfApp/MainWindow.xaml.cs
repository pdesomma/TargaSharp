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
        // them back under Examples\ in the build output; resolved against the exe so the CWD does not matter.
        string[] Files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Examples"), "*.tga", SearchOption.AllDirectories);
        TgaFile? T;
        const string OutDir = @"D:\TGA\";


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

            Guarded(() =>
            {
                using Bitmap bitmap = T.ToBitmap();
                T = TgaDrawing.FromBitmap(bitmap);
                ShowTga();
            });
        }

        private void ListBoxSelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;

            string tgaFilePath = Files[listBox1.SelectedIndex];
            if (File.Exists(tgaFilePath))
            {
                Guarded(() =>
                {
                    T = new TgaFile(tgaFilePath);
                    //T.UpdatePostageStampImage();
                    ShowTga();
                });
            }
        }

        private void SaveSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (T == null)
                return;

            Guarded(() =>
            {
                Directory.CreateDirectory(OutDir);
                T.Save(Path.Combine(OutDir, "___T.tga"));
            });
        }

        private void SaveAllButton_Click(object sender, RoutedEventArgs e)
        {
            Guarded(() =>
            {
                Directory.CreateDirectory(OutDir);
                for (int i = 0; i < Files.Length; i++) new TgaFile(Files[i]).Save(Path.Combine(OutDir, Path.GetFileName(Files[i])));
            });
        }

        /// <summary>
        /// Runs a handler body and shows any failure in a message box; an unhandled exception in a WPF event handler kills the process.
        /// </summary>
        /// <param name="action">Handler body.</param>
        private static void Guarded(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowTga()
        {
            if (T is null) return;

            using Bitmap converted = T.ToBitmap();
            // WPF's BMP decoder cannot show Format16bppGrayScale; downsample to 8bpp indexed for display.
            bool isGray16 = converted.PixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppGrayScale;
            using Bitmap? gray8 = isGray16 ? Gray16To8bppIndexed(converted) : null;
            Bitmap bitmap = gray8 ?? converted;

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
            try
            {
                for (int y = 0; y < height; y++)
                    Marshal.Copy(BmpData.Scan0 + (nint)y * BmpData.Stride, ImageData, y * width * 2, width * 2);
            }
            finally
            {
                bitmap.UnlockBits(BmpData);
            }

            byte[] ImageData2 = new byte[width * height];
            for (int i = 0; i < ImageData2.Length; i++)
                ImageData2[i] = ImageData[i * 2 + 1];

            Bitmap BmpOut = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            BmpData = BmpOut.LockBits(Re, ImageLockMode.WriteOnly, BmpOut.PixelFormat);
            try
            {
                for (int y = 0; y < height; y++)
                    Marshal.Copy(ImageData2, y * width, BmpData.Scan0 + (nint)y * BmpData.Stride, width);
            }
            finally
            {
                BmpOut.UnlockBits(BmpData);
            }

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