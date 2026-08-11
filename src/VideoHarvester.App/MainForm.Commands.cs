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
        void ShowSelected() {
            if(tasks.SelectedItems.Count==0)return;
            ShowJobDetails((DownloadJob)tasks.SelectedItems[0].Tag);
        }
        async Task RetrySelected() {
            if(tasks.SelectedItems.Count==0)return;
            var j=(DownloadJob)tasks.SelectedItems[0].Tag;
            j.State="等待重试";
            j.Progress=0;
            j.Error="";
            UpdateRow(j);
            await RunJobs(new List<DownloadJob> {
                j
            }
            );
        }
        void OpenSelected() {
            if(tasks.SelectedItems.Count==0)return;
            var j=(DownloadJob)tasks.SelectedItems[0].Tag;
            if(File.Exists(j.File))Process.Start(new ProcessStartInfo(j.File) {
                UseShellExecute=true
            }
            );
        }
        void OpenFolder() {
            Directory.CreateDirectory(folder.Text);
            Process.Start(new ProcessStartInfo(folder.Text) {
                UseShellExecute=true
            }
            );
        }
        void CopySelectedError() {
            if(tasks.SelectedItems.Count==0) {
                MessageBox.Show("请先在任务列表中选择一个任务。");
                return;
            }
            var j=(DownloadJob)tasks.SelectedItems[0].Tag;
            string text=j.Diagnostic.ToString();
            if(String.IsNullOrWhiteSpace(text))text="URL: "+j.Url+Environment.NewLine+"当前还没有诊断输出。";
            Clipboard.SetText(text);
            SetStage("诊断日志已复制");
        }
        void Cancel() {
            try {
                if(current!=null&&!current.HasExited)current.Kill();
            }
            catch {
            }
            SetStage("已取消");
        }
        void OpenPath(string path) {
            try {
                if(File.Exists(path))Process.Start(new ProcessStartInfo(path) {
                    UseShellExecute=true
                }
                );
            }
            catch {
                MessageBox.Show("无法打开这个文件，请从保存文件夹中手动打开。");
            }
        }
        void AskOpenFile(string file,bool allowSuppress) {
            using(var f=new Form()) {
                f.Text="下载完成";
                f.StartPosition=FormStartPosition.CenterParent;
                f.FormBorderStyle=FormBorderStyle.FixedDialog;
                f.MaximizeBox=false;
                f.MinimizeBox=false;
                f.ClientSize=new Size(510,190);
                f.Font=Font;
                f.BackColor=Color.White;
                var t=new Label();
                t.Text="已经下载完成：\n\n"+Path.GetFileName(file);
                t.SetBounds(24,20,462,70);
                t.AutoEllipsis=true;
                f.Controls.Add(t);
                var stop=new CheckBox();
                stop.Text="本次剩余文件不再询问";
                stop.SetBounds(24,100,250,28);
                stop.Visible=allowSuppress;
                f.Controls.Add(stop);
                var yes=new Button();
                yes.Text="立即打开";
                yes.SetBounds(286,138,96,34);
                yes.DialogResult=DialogResult.Yes;
                Btn(yes,Color.FromArgb(58,135,255),Color.White);
                var no=new Button();
                no.Text="稍后再说";
                no.SetBounds(390,138,96,34);
                no.DialogResult=DialogResult.No;
                Btn(no,Color.FromArgb(235,239,245),Color.FromArgb(60,72,90));
                f.Controls.Add(yes);
                f.Controls.Add(no);
                f.AcceptButton=yes;
                f.CancelButton=no;
                var answer=f.ShowDialog(this);
                if(stop.Checked)suppressRemainingPrompts=true;
                if(answer==DialogResult.Yes)OpenPath(file);
            }
        }
        void ShowBatchSummary() {
            using(var f=new Form()) {
                f.Text="任务完成";
                f.StartPosition=FormStartPosition.CenterParent;
                f.FormBorderStyle=FormBorderStyle.FixedDialog;
                f.MaximizeBox=false;
                f.MinimizeBox=false;
                f.ClientSize=new Size(540,220);
                f.Font=Font;
                f.BackColor=Color.White;
                var title=new Label();
                title.Text="本次任务处理完成";
                title.Font=new Font(Font.FontFamily,14,FontStyle.Bold);
                title.SetBounds(26,22,480,34);
                f.Controls.Add(title);
                var info=new Label();
                info.Text="成功 "+runSuccess+" 个    跳过 "+runSkipped+" 个    失败 "+runFailed+" 个\n文件已保存到："+folder.Text;
                info.SetBounds(26,68,488,66);
                info.AutoEllipsis=true;
                f.Controls.Add(info);
                var first=new Button();
                first.Text="播放第一个";
                first.SetBounds(202,160,104,36);
                Btn(first,Color.FromArgb(235,239,245),Color.FromArgb(50,62,80));
                first.Click+=(s,e)=> {
                    if(runFiles.Count>0)OpenPath(runFiles[0]);
                }
                ;
                var dir=new Button();
                dir.Text="打开文件夹";
                dir.SetBounds(314,160,104,36);
                Btn(dir,Color.FromArgb(58,135,255),Color.White);
                dir.Click+=(s,e)=> {
                    OpenFolder();
                    f.Close();
                }
                ;
                var close=new Button();
                close.Text="关闭";
                close.SetBounds(426,160,88,36);
                Btn(close,Color.FromArgb(235,239,245),Color.FromArgb(50,62,80));
                close.Click+=(s,e)=>f.Close();
                f.Controls.Add(first);
                f.Controls.Add(dir);
                f.Controls.Add(close);
                f.ShowDialog(this);
            }
        }
        void ShowClearMenu() {
            var menu=new ContextMenuStrip();
            var done=menu.Items.Add("清除已完成和已跳过的任务");
            var all=menu.Items.Add("清除全部任务记录");
            done.Click+=(s,e)=>ClearCompleted();
            all.Click+=(s,e)=>ClearAllRecords();
            menu.Show(clearRecords,new Point(0,clearRecords.Height));
        }
        void ClearCompleted() {
            var remove=jobs.FindAll(j=>j.State=="完成"||j.State=="已跳过");
            foreach(var j in remove) {
                if(j.Row!=null)tasks.Items.Remove(j.Row);
                jobs.Remove(j);
            }
            if(remove.Count==0)MessageBox.Show("当前没有可清除的已完成记录。");
            else {
                details.Clear();
                SetStage("已清除 "+remove.Count+" 条任务记录");
                SaveQueue(true);
            }
        }
        void ClearAllRecords() {
            if(!start.Enabled) {
                MessageBox.Show("请先取消或等待当前任务结束，再清除记录。");
                return;
            }
            if(jobs.Count==0)return;
            if(MessageBox.Show("确定清除界面中的全部任务记录吗？\n\n已经下载的视频不会被删除。","清理记录",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            jobs.Clear();
            tasks.Items.Clear();
            details.Clear();
            overall.Value=0;
            metrics.Text="当前 0%";
            queueLabel.Text="总任务 0/0";
            SetStage("任务记录已清空");
            SaveQueue(true);
        }
        void Notify(string text) {
            try {
                notify.BalloonTipTitle="VideoHarvester";
                notify.BalloonTipText=text;
                notify.ShowBalloonTip(3500);
            }
            catch {
            }
        }
    }
}
