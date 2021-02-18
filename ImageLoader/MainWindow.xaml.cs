using ImageLoader.Exts;
using ImageLoader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ImageLoader
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<FrameModel> images = new List<FrameModel>();
        private readonly CancellationTokenSource tokenSource = null;
        private volatile bool isReading = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ファイルをドラッグしたときの処理を定義します。
        /// </summary>
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (!isReading && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        /// <summary>
        /// ファイルをドロップしたときの処理を定義します。
        /// </summary>
        private void Window_Drop(object sender, DragEventArgs e)
        {
            var entries = (string[])e.Data.GetData(DataFormats.FileDrop);
            isReading = true;

            foreach (var entry in entries)
            {
                if (File.Exists(entry))
                {
                    tokenSource?.Cancel();

                    var decoder = Interfaces.GetImageDecoder(entry);

                    if (decoder != null)
                    {
                        Title = Path.GetFileName(entry);
                        Message.Text = "";

                        try
                        {
                            Task.Factory.StartNew(() =>
                            {
                                (var width, var height, var times) = decoder.Decode(entry, images);
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    ImageArea.Width = width;
                                    ImageArea.Height = height;
                                }));

                                if (images.Count == 1)
                                {
                                    // 画像が一枚の時はそのまま表示
                                    Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        ImageArea.Source = images[0].CreateBitmap();
                                    }));
                                }
                                else
                                {
                                    // 画像が複数ある時はアニメーション表示
                                    Animation.Play(images, width, height, times, ImageArea, tokenSource);
                                }

                                isReading = false;
                            });
                        }
                        catch (Exception ex)
                        {
                            Message.Text = ex.Message;
                        }
                    }
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// コピーコマンドの処理を定義します。
        /// </summary>
        private void CommandCopy(object sender, ExecutedRoutedEventArgs e)
        {
            if (ImageArea.Source != null)
            {
                Clipboard.SetImage((System.Windows.Media.Imaging.BitmapSource)ImageArea.Source);
            }
        }
    }
}
