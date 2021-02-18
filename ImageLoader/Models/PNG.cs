using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static ImageLoader.Models.Constants.PNG;

namespace ImageLoader.Models
{
    public class PNG : IImageDecoder
    {
        /// <summary>
        /// PNGファイルシグネチャを定義します。
        /// </summary>
        public readonly static byte[] Signature = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };

        /// <summary>
        /// PNGファイルを展開します。
        /// </summary>
        /// <param name="path">展開するPNGファイルのパスを設定します。</param>
        /// <param name="imageList">展開した画像データを格納するコレクションを設定します。</param>
        /// <returns>画像のサイズとアニメーションの再生回数を返します。</returns>
        public (int width, int height, int times) Decode(string path, List<FrameModel> imageList)
        {
            var chunks = new List<string>();
            var images = new List<byte[]>();
            var frames = new List<fcTL>();
            byte[] buffer;

            bool hasIHDR = false;
            bool hasPLTE = false;
            bool hasACTL = false;
            bool hasIEND = false;

            var ImageHeader = new IHDR();
            var AnimationControl = new acTL();

            bool isIdatFrame = false;
            int sequence = 0;
            bool isFCTL = false;

            imageList.Clear();

            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // PNGヘッダをスキップ
                stream.Seek(Signature.Length, SeekOrigin.Begin);
                var offset = Signature.Length;

                while (!hasIEND)
                {
                    // チャンクを読み取り
                    var length = ReadUINT(stream);
                    buffer = ReadBytes(stream, (int)(length + 4));
                    var type = Encoding.ASCII.GetString(buffer.Take(4).ToArray());
                    var crc = ReadUINT(stream);

                    // CRCチェック
                    var calcCRC = new CRC32();
                    calcCRC.SlurpBlock(buffer, 0, (int)(length + 4));

                    if (crc != (uint)calcCRC.Crc32Result)
                    {
                        throw CreateEx(type, offset, "CRCが一致していません");
                    }

                    switch (type)
                    {
                        case "IHDR":
                            // イメージヘッダ読み取り
                            if (hasIHDR)
                            {
                                throw CreateEx(type, offset, "チャンクが複数存在しています");
                            }
                            if (chunks.Count > 0)
                            {
                                throw CreateEx(type, offset, "チャンクの位置が不正です");
                            }

                            ImageHeader.Width = ReadUINT(buffer, 4);
                            ImageHeader.Height = ReadUINT(buffer, 8);
                            ImageHeader.BitDepth = buffer[12];
                            ImageHeader.ColorType = (ColorType)buffer[13];
                            ImageHeader.CompressionMethod = buffer[14];
                            ImageHeader.FilterMethod = buffer[15];
                            ImageHeader.InterlaceMethod = (InterlaceMethod)buffer[16];

                            if (ImageHeader.Width == 0 || ImageHeader.Height == 0)
                            {
                                throw CreateEx(type, offset, "画像のサイズが不正です");
                            }
                            if (!Enum.IsDefined(typeof(ColorType), ImageHeader.ColorType) ||
                                !AllowBitDepth.ContainsKey(ImageHeader.ColorType) ||
                                !AllowBitDepth[ImageHeader.ColorType].Contains(ImageHeader.BitDepth))
                            {
                                throw CreateEx(type, offset, "カラーモードとビット深度の組み合わせが不正です");
                            }
                            if (ImageHeader.CompressionMethod != 0)
                            {
                                throw CreateEx(type, offset, "圧縮手法の値が不正です");
                            }
                            if (ImageHeader.FilterMethod != 0)
                            {
                                throw CreateEx(type, offset, "フィルター手法の値が不正です");
                            }
                            if (!Enum.IsDefined(typeof(InterlaceMethod), ImageHeader.InterlaceMethod))
                            {
                                throw CreateEx(type, offset, "インターレース手法の値が不正です");
                            }

                            hasIHDR = true;
                            break;
                        case "PLTE":
                            // パレット読み取り
                            if (hasPLTE)
                            {
                                throw CreateEx(type, offset, "チャンクが複数存在しています");
                            }

                            //
                            // パレット読み込み処理
                            //

                            hasPLTE = true;
                            break;
                        case "IDAT":
                            // イメージデータ読み取り
                            var idat = buffer.Skip(4).Take(buffer.Length - 4).ToArray();
                            if (chunks[chunks.Count - 1] != type)
                            {
                                if (images.Count != 0)
                                {
                                    throw CreateEx(type, offset, "非連続なチャンクが存在しています");
                                }

                                images.Add(idat);

                                if (hasACTL && frames.Count == 1)
                                {
                                    isIdatFrame = true;
                                }
                            }
                            else
                            {
                                images[images.Count - 1] = images[images.Count - 1].Concat(idat).ToArray();
                            }

                            if (hasACTL)
                            {
                                isFCTL = false;
                            }
                            break;
                        case "acTL":
                            // アニメーションコントロール読み取り
                            if (hasACTL)
                            {
                                throw CreateEx(type, offset, "チャンクが複数存在しています");
                            }
                            if (images.Count != 0)
                            {
                                throw CreateEx(type, offset, "チャンクの位置が不正です");
                            }

                            AnimationControl.NumFrames = ReadUINT(buffer, 4);
                            AnimationControl.NumPlays = ReadUINT(buffer, 8);

                            if (AnimationControl.NumFrames == 0)
                            {
                                throw CreateEx(type, offset, "アニメーションフレーム数が0です");
                            }

                            hasACTL = true;
                            break;
                        case "fcTL":
                            // フレームコントロール読み取り
                            if (isFCTL)
                            {
                                throw CreateEx(type, offset, "フレームコントロールが連続しています");
                            }

                            var fctl = new fcTL()
                            {
                                SequenceNumber = ReadUINT(buffer, 4),
                                Width = ReadUINT(buffer, 8),
                                Height = ReadUINT(buffer, 12),
                                XOffset = ReadUINT(buffer, 16),
                                YOffset = ReadUINT(buffer, 20),
                                DelayNum = ReadWORD(buffer, 24),
                                DelayDen = ReadWORD(buffer, 26),
                                DisposeOp = (DisposeOp)buffer[28],
                                BlendOp = (BlendOp)buffer[29]
                            };

                            if (frames.Count == 0 && sequence != 0)
                            {
                                throw CreateEx(type, offset, "最初のチャンクの位置が不正です");
                            }
                            if (fctl.SequenceNumber != sequence)
                            {
                                throw CreateEx(type, offset, "シーケンス番号が不正です");
                            }
                            if (frames.Count == 0 && (fctl.Width != ImageHeader.Width || fctl.Height != ImageHeader.Height || fctl.XOffset != 0 || fctl.YOffset != 0))
                            {
                                throw CreateEx(type, offset, "1枚目のアニメーションフレームのサイズが不正です");
                            }
                            if (fctl.Width == 0 || fctl.Height == 0 || fctl.XOffset + fctl.Width > ImageHeader.Width || fctl.YOffset + fctl.Height > ImageHeader.Height)
                            {
                                throw CreateEx(type, offset, "アニメーションフレームのサイズが不正です");
                            }
                            if (!Enum.IsDefined(typeof(DisposeOp), fctl.DisposeOp))
                            {
                                throw CreateEx(type, offset, "フレーム描画後の処理方法の値が不正です");
                            }
                            if (!Enum.IsDefined(typeof(BlendOp), fctl.BlendOp))
                            {
                                throw CreateEx(type, offset, "フレーム描画方法の値が不正です");
                            }

                            if (fctl.DelayDen == 0) fctl.DelayDen = 100;

                            frames.Add(fctl);
                            sequence++;
                            isFCTL = true;
                            break;
                        case "fdAT":
                            // フレームデータ読み取り
                            if (!isFCTL)
                            {
                                throw CreateEx(type, offset, "対応するフレームコントロールが存在しません");
                            }

                            var seq = ReadUINT(buffer, 4);

                            if (seq != sequence)
                            {
                                throw CreateEx(type, offset, "シーケンス番号が不正です");
                            }

                            var fdat = buffer.Skip(8).Take(buffer.Length - 8).ToArray();
                            if (chunks[chunks.Count - 1] != type)
                            {
                                images.Add(fdat);
                            }
                            else
                            {
                                images[images.Count - 1] = images[images.Count - 1].Concat(fdat).ToArray();
                            }

                            sequence++;
                            isFCTL = false;
                            break;
                        case "IEND":
                            hasIEND = true;
                            break;
                    }

                    chunks.Add(type);
                    offset += buffer.Length + 8;
                }
            }

            // 必須チャンクチェック
            if (!hasIHDR || !hasIEND || (!hasPLTE && ImageHeader.ColorType == ColorType.Palette))
            {
                throw new InvalidOperationException("必須チャンクが存在しません");
            }

            // 不要チャンクチェック
            if (hasPLTE && (ImageHeader.ColorType == ColorType.Grayscale || ImageHeader.ColorType == ColorType.GrayscaleAlpha))
            {
                throw new InvalidOperationException("不要なチャンクが存在しています");
            }

            // フレーム数チェック
            if (hasACTL && (AnimationControl.NumFrames != frames.Count || AnimationControl.NumFrames != images.Count - (isIdatFrame ? 0 : 1)))
            {
                throw new InvalidOperationException("アニメーションフレーム数が不正です");
            }

            // イメージデータチェック
            if (images.Count == 0)
            {
                throw new InvalidOperationException("イメージデータが存在しません");
            }

            // イメージデータ展開
            if (hasACTL)
            {
                // APNG
                for (var i = 0; i < frames.Count; i++)
                {
                    imageList.Add(new FrameModel()
                    {
                        XOffset = (int)frames[i].XOffset,
                        YOffset = (int)frames[i].YOffset,
                        Width = (int)frames[i].Width,
                        Height = (int)frames[i].Height,
                        Delay = frames[i].DelayNum * 1000 / frames[i].DelayDen,
                        Dispose = frames[i].DisposeOp == DisposeOp.APNG_DISPOSE_OP_NONE ? FrameModel.DisposeMode.None : frames[i].DisposeOp == DisposeOp.APNG_DISPOSE_OP_BACKGROUND ? FrameModel.DisposeMode.Background : FrameModel.DisposeMode.Previous,
                        Blend = frames[i].BlendOp == BlendOp.APNG_BLEND_OP_SOURCE ? FrameModel.BlendMode.Normal : FrameModel.BlendMode.AlphaBlending,
                        Data = Unfilter(ZlibStream.UncompressBuffer(images[i + (isIdatFrame ? 0 : 1)]), (int)frames[i].Width, (int)frames[i].Height, ImageHeader.ColorType)
                    });
                }
            }
            else
            {
                // PNG
                imageList.Add(new FrameModel()
                {
                    Width = (int)ImageHeader.Width,
                    Height = (int)ImageHeader.Height,
                    Data = Unfilter(ZlibStream.UncompressBuffer(images[0]), (int)ImageHeader.Width, (int)ImageHeader.Height, ImageHeader.ColorType)
                });
            }

            return (width: (int)ImageHeader.Width, height: (int)ImageHeader.Height, times: (int)(hasACTL ? AnimationControl.NumPlays : 0));

            static InvalidOperationException CreateEx(string type, int offset, string message)
            {
                return new InvalidOperationException($"{type}:{offset:X8}:{message}");
            }
        }

        /// <summary>
        /// ストリームからビッグエンディアンの<see cref="uint"/>型の値を読み込みます。
        /// </summary>
        /// <param name="stream">値を読み込むストリームを設定します。</param>
        /// <returns>読み込んだ値を返します。</returns>
        private uint ReadUINT(Stream stream)
        {
            var b1 = stream.ReadByte();
            var b2 = stream.ReadByte();
            var b3 = stream.ReadByte();
            var b4 = stream.ReadByte();

            if (b1 == -1 || b2 == -1 || b3 == -1 || b4 == -1)
            {
                throw new IOException("必要な長さのデータを読み込めません");
            }

            return (uint)(b1 << 24 | b2 << 16 | b3 << 8 | b4);
        }

        /// <summary>
        /// <see cref="byte"/>型の配列からビッグエンディアンの<see cref="uint"/>型の値として取得します。
        /// </summary>
        /// <param name="buffer">値を取得する配列を設定します。</param>
        /// <param name="offset">値を取得するオフセットを設定します。</param>
        /// <returns>取得した値を返します。</returns>
        private uint ReadUINT(byte[] buffer, int offset)
        {
            if (buffer.Length - offset < 4)
            {
                throw new IOException("uint型に必要な長さのデータを読み込めません");
            }

            return (uint)(buffer[offset] << 24 | buffer[offset + 1] << 16 | buffer[offset + 2] << 8 | buffer[offset + 3]);
        }

        /// <summary>
        /// <see cref="byte"/>型の配列からビッグエンディアンの<see cref="ushort"/>型の値として取得します。
        /// </summary>
        /// <param name="buffer">値を取得する配列を設定します。</param>
        /// <param name="offset">値を取得するオフセットを設定します。</param>
        /// <returns>取得した値を返します。</returns>
        private ushort ReadWORD(byte[] buffer, int offset)
        {
            if (buffer.Length - offset < 2)
            {
                throw new IOException("ushort型に必要な長さのデータを読み込めません");
            }

            return (ushort)(buffer[offset] << 8 | buffer[offset + 1]);
        }

        /// <summary>
        /// <see cref="byte"/>型の配列から任意の長さで新たな配列を取得します。
        /// </summary>
        /// <param name="stream">値を取得するストリームを設定します。</param>
        /// <param name="length">新しい配列の長さを設定します。</param>
        /// <returns>取得した配列を返します。</returns>
        private byte[] ReadBytes(Stream stream, int length)
        {
            var buffer = new byte[length];
            var count = stream.Read(buffer, 0, length);
            if (count != length)
            {
                throw new IOException("指定の長さのデータを読み込めません");
            }

            return buffer;
        }

        /// <summary>
        /// フィルターされたイメージデータをフィルターなしに展開します。
        /// </summary>
        /// <param name="buffer">フィルターされたデータを設定します。</param>
        /// <param name="width">展開する画像の幅を設定します。</param>
        /// <param name="height">展開する画像の高さを設定します。</param>
        /// <param name="type">展開時のカラータイプを設定します。</param>
        /// <returns>展開したデータを返します。</returns>
        private byte[] Unfilter(byte[] buffer, int width, int height, ColorType type)
        {
            var colors = new byte[height * width * 4];

            var dLeft = -4;
            var dUp = -width * 4;
            var dUpLeft = dUp + dLeft;

            var foffset = 0;
            var uoffset = 0;

            byte r = 0;
            byte g = 0;
            byte b = 0;
            byte a = 0;

            for (var y = 0; y < height; y++)
            {
                var filterType = (FilterType)buffer[foffset++];

                for (var x = 0; x < width; x++)
                {
                    switch (type)
                    {
                        case ColorType.RGB:
                            r = buffer[foffset++];
                            g = buffer[foffset++];
                            b = buffer[foffset++];
                            a = 255;
                            break;
                        case ColorType.RGBAlpha:
                            r = buffer[foffset++];
                            g = buffer[foffset++];
                            b = buffer[foffset++];
                            a = buffer[foffset++];
                            break;
                    }

                    switch (filterType)
                    {
                        case FilterType.None:
                            colors[uoffset++] = b;
                            colors[uoffset++] = g;
                            colors[uoffset++] = r;
                            colors[uoffset++] = a;
                            break;
                        case FilterType.Sub:
                            if (x > 0)
                            {
                                colors[uoffset] = (byte)(b + colors[uoffset + dLeft]);
                                colors[uoffset + 1] = (byte)(g + colors[uoffset + dLeft + 1]);
                                colors[uoffset + 2] = (byte)(r + colors[uoffset + dLeft + 2]);
                                colors[uoffset + 3] = (byte)(a + (type == ColorType.RGBAlpha ? colors[uoffset + dLeft + 3] : 0));
                                uoffset += 4;
                            }
                            else
                            {
                                colors[uoffset++] = b;
                                colors[uoffset++] = g;
                                colors[uoffset++] = r;
                                colors[uoffset++] = a;
                            }
                            break;
                        case FilterType.Up:
                            if (y > 0)
                            {
                                colors[uoffset] = (byte)(b + colors[uoffset + dUp]);
                                colors[uoffset + 1] = (byte)(g + colors[uoffset + dUp + 1]);
                                colors[uoffset + 2] = (byte)(r + colors[uoffset + dUp + 2]);
                                colors[uoffset + 3] = (byte)(a + (type == ColorType.RGBAlpha ? colors[uoffset + dUp + 3] : 0));
                                uoffset += 4;
                            }
                            else
                            {
                                colors[uoffset++] = b;
                                colors[uoffset++] = g;
                                colors[uoffset++] = r;
                                colors[uoffset++] = a;
                            }
                            break;
                        case FilterType.Average:
                            if (y > 0 && x > 0)
                            {
                                colors[uoffset] = (byte)(b + (colors[uoffset + dLeft] + colors[uoffset + dUp]) / 2);
                                colors[uoffset + 1] = (byte)(g + (colors[uoffset + dLeft + 1] + colors[uoffset + dUp + 1]) / 2);
                                colors[uoffset + 2] = (byte)(r + (colors[uoffset + dLeft + 2] + colors[uoffset + dUp + 2]) / 2);
                                colors[uoffset + 3] = (byte)(a + (type == ColorType.RGBAlpha ? (colors[uoffset + dLeft + 3] + colors[uoffset + dUp + 3]) / 2 : 0));
                                uoffset += 4;
                            }
                            else if (y > 0)
                            {
                                colors[uoffset] = (byte)(b + colors[uoffset + dUp] / 2);
                                colors[uoffset + 1] = (byte)(g + colors[uoffset + dUp + 1] / 2);
                                colors[uoffset + 2] = (byte)(r + colors[uoffset + dUp + 2] / 2);
                                colors[uoffset + 3] = (byte)(a + (type == ColorType.RGBAlpha ? colors[uoffset + dUp + 3] / 2 : 0));
                                uoffset += 4;
                            }
                            else if (x > 0)
                            {
                                colors[uoffset] = (byte)(b + colors[uoffset + dLeft] / 2);
                                colors[uoffset + 1] = (byte)(g + colors[uoffset + dLeft + 1] / 2);
                                colors[uoffset + 2] = (byte)(r + colors[uoffset + dLeft + 2] / 2);
                                colors[uoffset + 3] = (byte)(a + (type == ColorType.RGBAlpha ? colors[uoffset + dLeft + 3] / 2 : 0));
                                uoffset += 4;
                            }
                            else
                            {
                                colors[uoffset++] = b;
                                colors[uoffset++] = g;
                                colors[uoffset++] = r;
                                colors[uoffset++] = a;
                            }
                            break;
                        case FilterType.Paeth:
                            if (y > 0 && x > 0)
                            {
                                colors[uoffset] = (byte)(b + Paeth(colors[uoffset + dLeft], colors[uoffset + dUp], colors[uoffset + dUpLeft]));
                                colors[uoffset + 1] = (byte)(g + Paeth(colors[uoffset + dLeft + 1], colors[uoffset + dUp + 1], colors[uoffset + dUpLeft + 1]));
                                colors[uoffset + 2] = (byte)(r + Paeth(colors[uoffset + dLeft + 2], colors[uoffset + dUp + 2], colors[uoffset + dUpLeft + 2]));
                                colors[uoffset + 3] = (byte)(a + (type == ColorType.RGBAlpha ? Paeth(colors[uoffset + dLeft + 3], colors[uoffset + dUp + 3], colors[uoffset + dUpLeft + 3]) : 0));
                                uoffset += 4;
                            }
                            else if (y > 0)
                            {
                                colors[uoffset] = (byte)(b + colors[uoffset + dUp]);
                                colors[uoffset + 1] = (byte)(g + colors[uoffset + dUp + 1]);
                                colors[uoffset + 2] = (byte)(r + colors[uoffset + dUp + 2]);
                                colors[uoffset + 3] = (byte)(a + (type == ColorType.RGBAlpha ? colors[uoffset + dUp + 3] : 0));
                                uoffset += 4;
                            }
                            else if (x > 0)
                            {
                                colors[uoffset] = (byte)(b + colors[uoffset + dLeft]);
                                colors[uoffset + 1] = (byte)(g + colors[uoffset + dLeft + 1]);
                                colors[uoffset + 2] = (byte)(r + colors[uoffset + dLeft + 2]);
                                colors[uoffset + 3] = (byte)(a + (type == ColorType.RGBAlpha ? colors[uoffset + dLeft + 3] : 0));
                                uoffset += 4;
                            }
                            else
                            {
                                colors[uoffset++] = b;
                                colors[uoffset++] = g;
                                colors[uoffset++] = r;
                                colors[uoffset++] = a;
                            }
                            break;
                    }
                }
            }

            return colors;

            static byte Paeth(byte v1, byte v2, byte v3)
            {
                var pa = (short)(v2 - v3);
                var pb = (short)(v1 - v3);
                var pc = (short)(v1 + v2 - v3 - v3);
                pa = (short)((pa ^ (pa >> 15)) - (pa >> 15));
                pb = (short)((pb ^ (pb >> 15)) - (pb >> 15));
                pc = (short)((pc ^ (pc >> 15)) - (pc >> 15));

                if (pa <= pb && pa <= pc)
                {
                    return v1;
                }
                else if (pb <= pc)
                {
                    return v2;
                }
                else
                {
                    return v3;
                }
            }
        }
    }
}
