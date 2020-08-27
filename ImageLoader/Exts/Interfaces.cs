using ImageLoader.Models;
using System.IO;

namespace ImageLoader.Exts
{
    public static class Interfaces
    {
        /// <summary>
        /// ファイルの画像形式を解析して展開可能なデコーダを取得します。
        /// </summary>
        /// <param name="path">解析するファイルのパスを設定します。</param>
        /// <param name="data">読み込んだ画像データを入れる配列を設定します。</param>
        /// <returns>画像形式を特定できた場合はデコーダを返します。</returns>
        public static IImageDecoder GetImageDecoder(string path)
        {
            if (CheckHeader(path, PNG.Signature))
            {
                return new PNG();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// ファイルのヘッダが一致するかチェックします。
        /// </summary>
        /// <param name="path">チェックするファイルのパスを設定します。</param>
        /// <param name="header">チェックするヘッダの配列を設定します。</param>
        /// <returns>一致した場合はtrue、それ以外の場合はfalseを返します。</returns>
        public static bool CheckHeader(string path, byte[] header)
        {
            var buffer = new byte[header.Length];

            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var count = stream.Read(buffer, 0, header.Length);

                if (count < header.Length)
                {
                    return false;
                }
            }

            var result = true;

            for (var i = 0; i < header.Length; i++)
            {
                if (buffer[i] != header[i])
                {
                    result = false;
                    break;
                }
            }

            return result;
        }
    }
}
