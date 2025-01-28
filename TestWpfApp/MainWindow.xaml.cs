using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using TargaNet;

namespace TestWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] Files = Directory.GetFiles(@"..\..\..\..\TGA File Examples\", "*.tga", SearchOption.AllDirectories);
        Targa T;


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
            if (T is null || T is not Targa) return;

            T = (Targa)((Bitmap)T);
            ShowTga();
        }

        private void ListBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            string TgaFile = Files[listBox1.SelectedIndex];
            if (File.Exists(TgaFile))
            {
                T = new Targa(TgaFile);
                //T.UpdatePostageStampImage();
                ShowTga();
            }
        }

        private void SaveSelectedButton_Click(object sender, EventArgs e)
        {
            if (T == null)
                return;

            string OutDir = @"D:\TGA\";
            if (!Directory.Exists(OutDir))
                Directory.CreateDirectory(OutDir);

            T.Save(Path.Combine(OutDir, Path.GetFileName("___T.tga")));
        }
        private void SaveAllButton_Click(object sender, EventArgs e)
        {
            string OutDir = @"D:\TGA\";
            if (!Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);

            for (int i = 0; i < Files.Length; i++) new Targa(Files[i]).Save(Path.Combine(OutDir, Path.GetFileName(Files[i])));
        }





        private void ShowTga()
        {
            Bitmap bitmap = (Bitmap)T;
            Bitmap thumb = T.GetPostageStampImage();

            // Convert image if Format16bppGrayScale
            if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppGrayScale)
            {
                bitmap = Gray16To8bppIndexed(bitmap);
                if (thumb != null)
                    thumb = Gray16To8bppIndexed(thumb);
            }

            richTextBox1.Document.Blocks.Clear();
            richTextBox1.AppendText(T.GetInfo());

            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
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

            byte[] ImageData = new byte[bitmap.Width * bitmap.Height * 2];
            Rectangle Re = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            BitmapData BmpData = bitmap.LockBits(Re, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            Marshal.Copy(BmpData.Scan0, ImageData, 0, ImageData.Length);
            bitmap.UnlockBits(BmpData);

            byte[] ImageData2 = new byte[bitmap.Width * bitmap.Height];
            for (long i = 0; i < ImageData2.LongLength; i++)
                ImageData2[i] = ImageData[i * 2 + 1];
            ImageData = null;

            Bitmap BmpOut = new Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            BmpData = BmpOut.LockBits(Re, ImageLockMode.WriteOnly, BmpOut.PixelFormat);
            Marshal.Copy(ImageData2, 0, BmpData.Scan0, ImageData2.Length);
            BmpOut.UnlockBits(BmpData);
            ImageData2 = null;
            BmpData = null;

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