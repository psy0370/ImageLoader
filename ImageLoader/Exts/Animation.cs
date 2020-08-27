using ImageLoader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ImageLoader.Exts
{
    /// <summary>
    /// 
    /// </summary>
    public class Animation
    {
        /// <summary>
        /// アニメーション処理を定義します。
        /// </summary>
        /// <param name="images">表示する画像のコレクションを設定します。</param>
        /// <param name="width">表示する画像の幅を設定します。</param>
        /// <param name="height">表示する画像の高さを設定します。</param>
        /// <param name="times">アニメーションを繰り返す回数を設定します。</param>
        /// <param name="control">画像を表示するコントロールを設定します。</param>
        /// <param name="source">タスクのキャンセルを管理するオブジェクトを設定します。</param>
        public static void Play(List<FrameModel> images, int width, int height, int times, Image control, CancellationTokenSource source)
        {
            source = new CancellationTokenSource();
            var token = source.Token;

            var pBuffer = new byte[height * width * 4];
            var oBuffer = new byte[height * width * 4];
            var count = 0;

            Task animationTask = Task.Factory.StartNew(() =>
            {
                var frame = 0;

                while (true)
                {
                    // キャンセルされたか、再生回数に達した場合は終了
                    if (token.IsCancellationRequested || (times != 0 && times == count))
                    {
                        source.Dispose();
                        source = null;
                        break;
                    }

                    // 出力バッファの初期化（出力バッファを前フレームとして退避）
                    switch (images[frame].Dispose)
                    {
                        case FrameModel.DisposeMode.None:
                            // 何もしない
                            Array.Copy(oBuffer, pBuffer, height * width * 4);
                            break;
                        case FrameModel.DisposeMode.Background:
                            // 黒で塗りつぶし
                            Array.Copy(oBuffer, pBuffer, height * width * 4);
                            for (var i = 0; i < oBuffer.Length; i++)
                            {
                                oBuffer[i] = 0;
                            }
                            break;
                        case FrameModel.DisposeMode.Previous:
                            // 前フレームの状態に戻す
                            var temp = oBuffer;
                            oBuffer = pBuffer;
                            pBuffer = temp;
                            break;
                    }

                    // 出力バッファに描画
                    var sOffset = 0;
                    for (var y = 0; y < images[frame].Height; y++)
                    {
                        var offset = ((y + images[frame].YOffset) * width + images[frame].XOffset) * 4;

                        for (var x = 0; x < images[frame].Width; x++)
                        {
                            if (images[frame].Blend == FrameModel.BlendMode.AlphaBlending)
                            {
                                // アルファ合成
                                var alpha = (float)images[frame].Data[sOffset + 3] / 255;
                                var r = (byte)(images[frame].Data[sOffset] * alpha + oBuffer[offset] * (1 - alpha));
                                var g = (byte)(images[frame].Data[sOffset + 1] * alpha + oBuffer[offset + 1] * (1 - alpha));
                                var b = (byte)(images[frame].Data[sOffset + 2] * alpha + oBuffer[offset + 2] * (1 - alpha));

                                oBuffer[offset++] = r;
                                oBuffer[offset++] = g;
                                oBuffer[offset++] = b;
                                oBuffer[offset++] = 255;
                                sOffset += 4;
                            }
                            else
                            {
                                // 上書き
                                oBuffer[offset++] = images[frame].Data[sOffset++];
                                oBuffer[offset++] = images[frame].Data[sOffset++];
                                oBuffer[offset++] = images[frame].Data[sOffset++];
                                oBuffer[offset++] = images[frame].Data[sOffset++];
                            }
                        }
                    }

                    // コントロールに画像をセット
                    control.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        control.Source = FrameModel.CreateBitmap(oBuffer, width, height);
                    }));

                    if(images[frame].Delay>0)
                    {
                        Thread.Sleep(images[frame].Delay);
                    }

                    if (++frame == images.Count)
                    {
                        frame = 0;
                        count++;
                    }
                }
            }, token);
        }
    }
}
