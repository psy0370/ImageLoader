using System.Collections.Generic;

namespace ImageLoader.Models.Constants
{
    /// <summary>
    /// PNGに関する定数を定義します。
    /// </summary>
    public class PNG
    {
        /// <summary>
        /// イメージヘッダ構造体を定義します。
        /// </summary>
        public struct IHDR
        {
            /// <summary>
            /// 画像の幅を取得または設定します。
            /// </summary>
            public uint Width;
            /// <summary>
            /// 画像の高さを取得または設定します。
            /// </summary>
            public uint Height;
            /// <summary>
            /// ビット深度を取得または設定します。
            /// </summary>
            public byte BitDepth;
            /// <summary>
            /// カラータイプを取得または設定します。
            /// </summary>
            public ColorType ColorType;
            /// <summary>
            /// 圧縮手法を取得または設定します。
            /// </summary>
            public byte CompressionMethod;
            /// <summary>
            /// フィルター手法を取得または設定します。
            /// </summary>
            public byte FilterMethod;
            /// <summary>
            /// インタレース手法を取得または設定します。
            /// </summary>
            public InterlaceMethod InterlaceMethod;
        }

        /// <summary>
        /// アニメーションコントロール構造体を定義します。
        /// </summary>
        public struct acTL
        {
            /// <summary>
            /// APNGのフレーム数を取得または設定します。
            /// </summary>
            public uint NumFrames;
            /// <summary>
            /// APNGのループ回数を取得または設定します。
            /// <para>0を指定すると無限ループ。</para>
            /// </summary>
            public uint NumPlays;
        }

        /// <summary>
        /// フレームコントロール構造体を定義します。
        /// </summary>
        public struct fcTL
        {
            /// <summary>
            /// アニメーションチャンクのシーケンス番号を取得または設定します。
            /// <para>0から始まります。</para>
            /// </summary>
            public uint SequenceNumber;
            /// <summary>
            /// 後に続くフレームの幅を取得または設定します。
            /// </summary>
            public uint Width;
            /// <summary>
            /// 後に続くフレームの高さを取得または設定します。
            /// </summary>
            public uint Height;
            /// <summary>
            /// 後に続くフレームを描画するx座標を取得または設定します。
            /// </summary>
            public uint XOffset;
            /// <summary>
            /// 後に続くフレームを描画するy座標を取得または設定します。
            /// </summary>
            public uint YOffset;
            /// <summary>
            /// フレーム遅延の分子を取得または設定します。
            /// </summary>
            public ushort DelayNum;
            /// <summary>
            /// フレーム遅延の分母を取得または設定します。
            /// </summary>
            public ushort DelayDen;
            /// <summary>
            /// フレームを描画した後にフレーム領域を廃棄するか?を取得または設定します。
            /// </summary>
            public DisposeOp DisposeOp;
            /// <summary>
            /// フレーム描画方法のタイプを取得または設定します。
            /// </summary>
            public BlendOp BlendOp;
        }

        /// <summary>
        /// カラータイプを列挙します。
        /// </summary>
        public enum ColorType : byte
        {
            /// <summary>
            /// グレースケール
            /// </summary>
            Grayscale = 0,
            /// <summary>
            /// RGBカラー
            /// </summary>
            RGB = 2,
            /// <summary>
            /// パレット
            /// </summary>
            Palette = 3,
            /// <summary>
            /// アルファ付きグレースケール
            /// </summary>
            GrayscaleAlpha = 4,
            /// <summary>
            /// アルファ付きRGBカラー
            /// </summary>
            RGBAlpha = 6
        }

        /// <summary>
        /// インタレース手法を列挙します。
        /// </summary>
        public enum InterlaceMethod : byte
        {
            /// <summary>
            /// 非インターレース
            /// </summary>
            NoInterlace = 0,
            /// <summary>
            /// Adam7インターレース
            /// </summary>
            Adam7Interlace = 1
        }

        /// <summary>
        /// フレームの処理方法を列挙します。
        /// </summary>
        public enum DisposeOp : byte
        {
            /// <summary>
            /// 次のフレームを描画する前に消去しません。出力バッファをそのまま使用します。
            /// </summary>
            APNG_DISPOSE_OP_NONE = 0,
            /// <summary>
            /// 次のフレームを描画する前に、出力バッファのフレーム領域を完全に透過な黒で塗りつぶします。
            /// </summary>
            APNG_DISPOSE_OP_BACKGROUND = 1,
            /// <summary>
            /// 次のフレームを描画する前に、出力バッファのフレーム領域をこのフレームに入る前の状態に戻します。
            /// </summary>
            APNG_DISPOSE_OP_PREVIOUS = 2
        }

        /// <summary>
        /// フレームの描画方法を列挙します。
        /// </summary>
        public enum BlendOp : byte
        {
            /// <summary>
            /// アルファ値を含めた全ての要素をフレームの出力バッファ領域に上書きします。
            /// </summary>
            APNG_BLEND_OP_SOURCE = 0,
            /// <summary>
            /// 書き込むデータのアルファ値を使って出力バッファに合成します。このとき、PNG仕様への拡張Version1.2.0のアルファチャンネル処理に書いてある通り上書き処理をします。サンプルコードの2つ目の項目を参照してください。
            /// </summary>
            APNG_BLEND_OP_OVER = 1
        }

        /// <summary>
        /// フィルターを列挙します。
        /// </summary>
        public enum FilterType : byte
        {
            /// <summary>
            /// Noneフィルター
            /// </summary>
            None = 0,
            /// <summary>
            /// Subフィルター
            /// </summary>
            Sub = 1,
            /// <summary>
            /// Upフィルター
            /// </summary>
            Up = 2,
            /// <summary>
            /// Averageフィルター
            /// </summary>
            Average = 3,
            /// <summary>
            /// Paethフィルター
            /// </summary>
            Paeth = 4
        }

        /// <summary>
        /// カラータイプとビット深度の組み合わせを定義します。
        /// </summary>
        public static Dictionary<ColorType, byte[]> AllowBitDepth = new Dictionary<ColorType, byte[]>()
        {
            { ColorType.Grayscale, new byte[] { 1, 2, 4, 8, 16 } },
            { ColorType.RGB, new byte[] { 8, 16 } },
            { ColorType.Palette, new byte[] { 1, 2, 4, 8 } },
            { ColorType.GrayscaleAlpha, new byte[] { 8, 16 } },
            { ColorType.RGBAlpha, new byte[] { 8, 16 } }
        };
    }
}
