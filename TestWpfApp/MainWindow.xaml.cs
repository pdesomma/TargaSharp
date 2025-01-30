using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using TargaSharp;

namespace TestWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] Files = Directory.GetFiles(@"Examples\", "*.tga", SearchOption.AllDirectories);
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
            richTextBox1.AppendText(GetTargaInfo(T));

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

        /// <summary>
        /// Get information from TGA image.
        /// </summary>
        /// <returns>MultiLine string with info fields (one per line).</returns>
        private string GetTargaInfo(Targa tga)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Header:");
            stringBuilder.AppendLine("\tID Length = " + tga.HeaderArea.IdLength);
            stringBuilder.AppendLine("\tImage Type = " + tga.HeaderArea.ImageType);
            stringBuilder.AppendLine("\tHeader -> ImageSpec:");
            stringBuilder.AppendLine("\t\tImage Width = " + tga.HeaderArea.ImageSpec.ImageWidth);
            stringBuilder.AppendLine("\t\tImage Height = " + tga.HeaderArea.ImageSpec.ImageHeight);
            stringBuilder.AppendLine("\t\tPixel Depth = " + tga.HeaderArea.ImageSpec.PixelDepth);
            stringBuilder.AppendLine("\t\tImage Descriptor (AsByte) = " + tga.HeaderArea.ImageSpec.ImageDescriptor.ToByte());
            stringBuilder.AppendLine("\t\tImage Descriptor -> AttributeBits = " + tga.HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits);
            stringBuilder.AppendLine("\t\tImage Descriptor -> ImageOrigin = " + tga.HeaderArea.ImageSpec.ImageDescriptor.ImageOrigin);
            stringBuilder.AppendLine("\t\tX_Origin = " + tga.HeaderArea.ImageSpec.XOrigin);
            stringBuilder.AppendLine("\t\tY_Origin = " + tga.HeaderArea.ImageSpec.YOrigin);
            stringBuilder.AppendLine("\tColorMap Type = " + tga.HeaderArea.ColorMapType);
            stringBuilder.AppendLine("\tHeader -> ColorMapSpec:");
            stringBuilder.AppendLine("\t\tColorMap Entry Size = " + tga.HeaderArea.ColorMapSpec.EntrySize);
            stringBuilder.AppendLine("\t\tColorMap Length = " + tga.HeaderArea.ColorMapSpec.ColorMapLength);
            stringBuilder.AppendLine("\t\tFirstEntry Index = " + tga.HeaderArea.ColorMapSpec.FirstEntryIndex);

            stringBuilder.AppendLine("\nImage / Color Map Area:");
            stringBuilder.Append("\tImage ID = ");
            stringBuilder.AppendLine(tga.HeaderArea.IdLength > 0 && tga.ImageOrColorMapArea.ImageId is not null ? "\"" + tga.ImageOrColorMapArea.ImageId.GetString() + "\"" : "null");
            stringBuilder.Append("\tImage Data ");
            stringBuilder.AppendLine(tga.ImageOrColorMapArea.ImageData is not null ? "Length = " + tga.ImageOrColorMapArea.ImageData.Length : "= null");
            stringBuilder.Append("\tColorMap Data Length = ");
            stringBuilder.AppendLine(tga.ImageOrColorMapArea.ColorMapData is not null ? "Length = " + tga.ImageOrColorMapArea.ColorMapData.Length : " = null");

            stringBuilder.AppendLine("\nDevelopers Area:");
            if (tga.DeveloperArea is not null)
                stringBuilder.AppendLine("\tCount = " + tga.DeveloperArea.Count);
            else
                stringBuilder.AppendLine("\tDevArea = null");

            stringBuilder.AppendLine("\nExtension Area:");
            if (tga.ExtensionArea is not null)
            {
                stringBuilder.AppendLine("\tExtension Size = " + tga.ExtensionArea.ExtensionSize);
                stringBuilder.AppendLine("\tAuthor Name = \"" + tga.ExtensionArea.AuthorName.GetString() + "\"");
                stringBuilder.AppendLine("\tAuthor Comments = \"" + tga.ExtensionArea.AuthorComments.GetString() + "\"");
                stringBuilder.AppendLine("\tDate / Time Stamp = " + tga.ExtensionArea.DateTimeStamp);
                stringBuilder.AppendLine("\tJob Name / ID = \"" + tga.ExtensionArea.JobNameOrID.GetString() + "\"");
                stringBuilder.AppendLine("\tJob Time = " + tga.ExtensionArea.JobTime);
                stringBuilder.AppendLine("\tSoftware ID = \"" + tga.ExtensionArea.SoftwareID.GetString() + "\"");
                stringBuilder.AppendLine("\tSoftware Version = \"" + tga.ExtensionArea.SoftwareVersion + "\"");
                stringBuilder.AppendLine("\tKey Color = " + tga.ExtensionArea.KeyColor);
                stringBuilder.AppendLine("\tPixel Aspect Ratio = " + tga.ExtensionArea.PixelAspectRatio);
                stringBuilder.AppendLine("\tGamma Value = " + tga.ExtensionArea.GammaValue);
                stringBuilder.AppendLine("\tColor Correction Table Offset = " + tga.ExtensionArea.ColorCorrectionTableOffset);
                stringBuilder.AppendLine("\tPostage Stamp Offset = " + tga.ExtensionArea.PostageStampOffset);
                stringBuilder.AppendLine("\tScan Line Offset = " + tga.ExtensionArea.ScanLineOffset);
                stringBuilder.AppendLine("\tAttributes Type = " + tga.ExtensionArea.AttributesType);

                if (tga.ExtensionArea.ScanLineTable is not null)
                    stringBuilder.AppendLine("\tScan Line Table = " + tga.ExtensionArea.ScanLineTable.Length);
                else
                    stringBuilder.AppendLine("\tScan Line Table = null");

                if (tga.ExtensionArea.PostageStampImage is not null)
                    stringBuilder.AppendLine("\tPostage Stamp Image: " + tga.ExtensionArea.PostageStampImage.ToString());
                else
                    stringBuilder.AppendLine("\tPostage Stamp Image = null");

                stringBuilder.AppendLine("\tColor Correction Table = " + (tga.ExtensionArea.ColorCorrectionTable is not null));
            }
            else
                stringBuilder.AppendLine("\tExtArea = null");

            stringBuilder.AppendLine("\nFooter:");
            if (tga.FooterArea is not null)
            {
                stringBuilder.AppendLine("\tExtension Area Offset = " + tga.FooterArea.ExtensionAreaOffset);
                stringBuilder.AppendLine("\tDeveloper Directory Offset = " + tga.FooterArea.DeveloperDirectoryOffset);
                stringBuilder.AppendLine("\tSignature (Full) = \"" + tga.FooterArea.Signature.ToString() +
                    tga.FooterArea.ReservedCharacter.ToString() + tga.FooterArea.BinaryZeroStringTerminator.ToString() + "\"");
            }
            else stringBuilder.AppendLine("\tFooter = null");

            return stringBuilder.ToString();
        }
    }
}