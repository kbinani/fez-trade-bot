using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Text;

namespace com.github.kbinani.feztradebot {
    /// <summary>
    /// tesseract のラッパー
    /// </summary>
    class Tesseract {
        /// <summary>
        /// 画像から文字列を検出する
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string Detect( Bitmap image ) {
            string imageTempFile = Path.GetTempFileName();
            File.Delete( imageTempFile );
            image.Save( imageTempFile, ImageFormat.Png );

            string tesseractDirectory = Path.Combine( Path.GetDirectoryName( Application.ExecutablePath ), "tesseract" );
            string tesseractBinaryPath = Path.Combine( tesseractDirectory, "tesseract.exe" );

            Process process = new Process();
            process.StartInfo.FileName = tesseractBinaryPath;
            process.StartInfo.WorkingDirectory = tesseractDirectory;
            process.StartInfo.Arguments = " \"" + imageTempFile + "\" -l jpn -psm 6";
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();
            string result = process.StandardOutput.ReadToEnd().Trim();
            File.Delete( imageTempFile );

            if( process.ExitCode == 0 ) {
                return result;
            } else {
                return "";
            }
        }
    }
}
