using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
        static readonly string ExamplesDir = Path.Combine(AppContext.BaseDirectory, "Examples");
        static readonly string OutDir = Path.Combine(Path.GetTempPath(), "TGA");
        string[] Files = [];
        TgaFile? T;


        public MainWindow()
        {
            InitializeComponent();
            // A field initializer runs before any handler's try/catch, so a missing folder used to kill the app.
            if (Directory.Exists(ExamplesDir))
                Files = Directory.GetFiles(ExamplesDir, "*.tga", SearchOption.AllDirectories);
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

            // ToBitmap never yields Format16bppGrayScale (16-bit gray comes back as 8bpp indexed), so it is directly displayable.
            using Bitmap bitmap = T.ToBitmap();

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
    }
}
