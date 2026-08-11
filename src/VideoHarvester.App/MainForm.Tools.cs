using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace VideoHarvester.App
{
    public partial class MainForm : Form {
        async Task EnsureTools() {
            await EnsureFile("yt-dlp.exe","https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe",false);
            await EnsureFile("deno.exe","https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip",true);
            await EnsureFile("ffmpeg.exe","https://github.com/yt-dlp/FFmpeg-Builds/releases/download/autobuild-2026-03-31-15-13/ffmpeg-N-123778-g3b55818764-win64-gpl.zip",true);
        }
        async Task EnsureFile(string name,string url,bool zip) {
            string target=Path.Combine(root,name);
            if(File.Exists(target)&&new FileInfo(target).Length>1000000)return;
            SetStage("首次运行 · 正在准备 "+name);
            ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
            using(var wc=new WebClient()) {
                if(!zip) {
                    await wc.DownloadFileTaskAsync(new Uri(url),target);
                    return;
                }
                string z=Path.GetTempFileName(),t=Path.Combine(Path.GetTempPath(),"VH-"+Guid.NewGuid().ToString("N"));
                await wc.DownloadFileTaskAsync(new Uri(url),z);
                Directory.CreateDirectory(t);
                ZipFile.ExtractToDirectory(z,t);
                foreach(var f in Directory.GetFiles(t,"*.exe",SearchOption.AllDirectories)) {
                    string n=Path.GetFileName(f);
                    if(n==name||name=="ffmpeg.exe"&&(n=="ffmpeg.exe"||n=="ffprobe.exe"))File.Copy(f,Path.Combine(root,n),true);
                }
                try {
                    File.Delete(z);
                    Directory.Delete(t,true);
                }
                catch {
                }
            }
            if(!File.Exists(target))throw new Exception(name+" 安装失败");
        }
        async Task UpdateEngine() {
            start.Enabled=false;
            try {
                SetStage("正在更新解析器");
                await EnsureTools();
                var p=Process.Start(new ProcessStartInfo(engine,"-U") {
                    UseShellExecute=false,CreateNoWindow=true
                }
                );
                await Task.Run(()=>p.WaitForExit());
                SetStage("解析器已是最新版本");
            }
            catch(Exception) {
                details.Text="解析器更新失败，请检查网络后重试。";
                SetStage("更新失败");
            }
            finally {
                start.Enabled=true;
            }
        }
    }
}
