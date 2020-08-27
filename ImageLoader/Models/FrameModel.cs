using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ImageLoader.Models
{
    /// <summary>
    /// フレームモデルを表すクラスを定義します。
    /// </summary>
    public class FrameModel
    {
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr ho);

        /// <summary>
        /// フレーム移動時の出力バッファの処理方法を列挙します。
        /// </summary>
        public enum DisposeMode : byte
        {
            /// <summary>
            /// 何もしません。
            /// </summary>
            None,
            /// <summary>
            /// 出力バッファを黒で塗りつぶします。
            /// </summary>
            Background,
            /// <summary>
            /// 出力バッファを前フレームの状態に戻します。
            /// </summary>
            Previous
        }

        /// <summary>
        /// 出力バッファへの描画方法を列挙します。
        /// </summary>
        public enum BlendMode : byte
        {
            /// <summary>
            /// 上書き
            /// </summary>
            Normal,
            /// <summary>
            /// アルファ合成
            /// </summary>
            AlphaBlending
        }

        /// <summary>
        /// フレームを描画するx座標を取得または設定します。
        /// </summary>
        public int XOffset { get; set; }
        /// <summary>
        /// フレームを描画するy座標を取得または設定します。
        /// </summary>
        public int YOffset { get; set; }
        /// <summary>
        /// フレームの幅を取得または設定します。
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// フレームの高さを取得または設定します。
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// フレームの表示時間（ミリ秒）を取得または設定します。
        /// </summary>
        public int Delay { get; set; }
        /// <summary>
        /// フレーム移動時の出力バッファの処理方法を取得または設定します。
        /// </summary>
        public DisposeMode Dispose { get; set; }
        /// <summary>
        /// 出力バッファへの描画方法を取得または設定します。
        /// </summary>
        public BlendMode Blend { get; set; }
        /// <summary>
        /// フレームの画像データを取得または設定します。
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// <see cref="Data"/>プロパティからビットマップ画像を作成します。
        /// </summary>
        /// <returns>作成したビットマップ画像を返します。</returns>
        public BitmapSource CreateBitmap()
        {
            return CreateBitmap(Data, Width, Height);
        }

        /// <summary>
        /// <see cref="byte"/>型の配列からビットマップ画像を作成します。
        /// </summary>
        /// <param name="buffer">出力バッファを設定します。</param>
        /// <returns>作成したビットマップ画像を返します。</returns>
        public static BitmapSource CreateBitmap(byte[] buffer, int width, int height)
        {
            using var bitmap = new Bitmap(width, height);
            var rectangle = new Rectangle(0, 0, width, height);
            var bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            // Bitmapの先頭アドレスを取得
            var ptr = bitmapData.Scan0;

            // Bitmapへコピー
            Marshal.Copy(buffer, 0, ptr, buffer.Length);
            bitmap.UnlockBits(bitmapData);

            // WPFで使用可能な形式に変換
            var hBitmap = bitmap.GetHbitmap();
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(hBitmap);

            return bitmapSource;
        }
    }
}
