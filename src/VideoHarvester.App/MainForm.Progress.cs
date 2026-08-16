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
using VideoHarvester.App.Core;

namespace VideoHarvester.App
{
    public partial class MainForm : Form {
        void HandleLine(DownloadJob j,string line) {
            j.Diagnostic.AppendLine(line);
            if(line.StartsWith("VH_TITLE:")) {
                currentTitle=line.Substring(9);
                j.Title=currentTitle;
                j.State="已解析 · 准备下载";
                SetStage("已找到视频 · 准备下载");
                SetFriendly(j,"已识别视频：“"+j.Title+"”");
                UpdateRow(j);
                return;
            }
            if(line.StartsWith("VH_FILE:")) {
                currentFile=line.Substring(8);
                j.File=currentFile;
                if(!completedFiles.Contains(currentFile))completedFiles.Add(currentFile);
                SetStage("正在整理文件");
                SetFriendly(j,"文件已经保存，正在完成最后处理…");
                return;
            }
            if(line.StartsWith("VH_META:")) {
                j.Resolution=line.Substring(8);
                if(audio.Checked)j.Actual="MP3 音频";
                else {
                    var hm=Regex.Match(j.Resolution,@"x(\d+)");
                    if(hm.Success)j.Actual=hm.Groups[1].Value+"p";
                }
                ShowJobDetails(j);
                return;
            }
            if(line.IndexOf("Merging formats",StringComparison.OrdinalIgnoreCase)>=0) {
                j.State="正在合并音视频";
                SetStage(j.State);
                SetFriendly(j,"视频和音频已下载，正在合并…");
            }
            else if(line.IndexOf("Downloading video",StringComparison.OrdinalIgnoreCase)>=0) {
                SetStage("正在下载视频");
                SetFriendly(j,"正在下载视频画面…");
            }
            else if(line.IndexOf("Downloading audio",StringComparison.OrdinalIgnoreCase)>=0) {
                SetStage("正在下载音频");
                SetFriendly(j,"正在下载音频…");
            }
            else if(line.IndexOf("cookies",StringComparison.OrdinalIgnoreCase)>=0&&login.Checked)SetStage("正在读取 "+browser.SelectedItem+" 登录状态");
            var m=Regex.Match(line,@"(\d+(?:\.\d+)?)%.*?at\s+([^\s]+).*?ETA\s+([^\s]+)");
            var listProgress=Regex.Match(line,@"Downloading video\s+(\d+)\s+of\s+(\d+)",RegexOptions.IgnoreCase);
            if(listProgress.Success)queueLabel.Text="列表 "+listProgress.Groups[1].Value+"/"+listProgress.Groups[2].Value;
            if(m.Success) {
                double p;
                if(Double.TryParse(m.Groups[1].Value,out p)) {
                    j.Progress=(int)p;
                    j.State="正在下载";
                    overall.Value=Math.Max(0,Math.Min(100,j.Progress));
                    metrics.Text="当前 "+p.ToString("0.0")+"% · "+m.Groups[2].Value+" · 剩余 "+m.Groups[3].Value;
                    UpdateRow(j,m.Groups[2].Value+" / "+m.Groups[3].Value);
                }
            }
            if(line.StartsWith("ERROR:")||line.StartsWith("WARNING:")) {
                string friendly=TranslateTechnical(line);
                if(line.StartsWith("ERROR:"))j.Error=friendly;
                SetFriendly(j,friendly);
            }
        }
        string TranslateTechnical(string line) {
            return UserMessageTranslator.FromDiagnosticLine(line);
        }
        void SetFriendly(DownloadJob j,string text) {
            if(j.Friendly.Count==0||j.Friendly[j.Friendly.Count-1]!=text)j.Friendly.Add(text);
            if(tasks.SelectedItems.Count==0||tasks.SelectedItems[0].Tag==j)ShowJobDetails(j);
        }
        void ShowJobDetails(DownloadJob j) {
            var b=new StringBuilder();
            b.AppendLine("任务："+j.Title);
            b.AppendLine("状态："+j.State);
            b.AppendLine("请求："+j.Requested+"    实际："+j.Actual+(j.EstimatedBytes>0?"    预计 "+FormatBytes(j.EstimatedBytes):""));
            if(j.File!="") {
                b.AppendLine("文件："+j.File);
                b.AppendLine("大小："+j.Size+"    画质："+j.Resolution);
            }
            if(j.Friendly.Count>0) {
                b.AppendLine();
                foreach(var x in j.Friendly)b.AppendLine("• "+x);
            }
            details.Text=b.ToString();
        }
        void UpdateRow(DownloadJob j,string speed="--") {
            if(j.Row==null)return;
            j.Row.SubItems[0].Text=j.Title;
            j.Row.SubItems[1].Text=j.State;
            j.Row.SubItems[2].Text=j.Progress+"%";
            j.Row.SubItems[3].Text=speed;
            SaveQueue(false);
        }
        void SetStage(string text) {
            status.Text=text;
            status.ForeColor=text.IndexOf("失败")>=0||text.IndexOf("错误")>=0?Color.FromArgb(255,105,120):text.IndexOf("完成")>=0?Color.FromArgb(80,220,150):Color.White;
        }
    }
}
