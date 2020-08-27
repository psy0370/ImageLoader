using System.Collections.Generic;

namespace ImageLoader.Models
{
    /// <summary>
    /// 画像デコーダのインターフェースを定義します。
    /// </summary>
    public interface IImageDecoder
    {
        /// <summary>
        /// 画像ファイルを展開します。
        /// </summary>
        /// <param name="path">展開する画像ファイルのパスを設定します。</param>
        /// <param name="imageList">展開した画像データを格納するコレクションを設定します。</param>
        /// <returns>画像のサイズとアニメーションの再生回数を返します。</returns>
        (int width, int height, int times) Decode(string path, List<FrameModel> images);
    }
}
