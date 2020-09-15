using ImageLoader.Exts;
using ImageLoader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ImageLoader
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<FrameModel> images = new List<FrameModel>();
        private CancellationTokenSource tokenSource = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ファイルをドラッグしたときの処理を定義します。
        /// </summary>
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
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
                            });
                        }
                        catch (Exception ex)
                        {
                            Message.Text = ex.Message;
                        }
                    }
                }
            }
        }
    }
}
