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
        void Build() {
            var title=L(this,"VideoHarvester",28,8,520);
            title.SetBounds(28,8,520,50);
            title.Font=new Font("Segoe UI",24,FontStyle.Bold);
            title.ForeColor=Color.FromArgb(19,29,44);
            title.TextAlign=ContentAlignment.MiddleLeft;
            var sub=L(this,"保存喜欢的视频 · 简单、清晰、只在本机运行",30,52,620);
            sub.ForeColor=Color.FromArgb(105,118,137);
            var card=new Panel();
            card.SetBounds(22,84,880,708);
            card.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
            card.BackColor=Color.White;
            card.BorderStyle=BorderStyle.FixedSingle;
            Controls.Add(card);
            var a=L(card,"视频页面链接",20,12,180);
            a.Font=new Font(Font,FontStyle.Bold);
            var hint=L(card,"每行一个链接",720,12,120);
            hint.TextAlign=ContentAlignment.TopRight;
            hint.Anchor=AnchorStyles.Top|AnchorStyles.Right;
            hint.ForeColor=Color.Gray;
            urls.Multiline=true;
            urls.ScrollBars=ScrollBars.Vertical;
            urls.SetBounds(20,40,838,82);
            urls.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            Field(urls);
            card.Controls.Add(urls);
            var f=L(card,"保存目录",20,132,100);
            f.Font=new Font(Font,FontStyle.Bold);
            folder.SetBounds(20,160,686,32);
            folder.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            Field(folder);
            card.Controls.Add(folder);
            browse.Text="选择文件夹";
            browse.SetBounds(718,157,140,38);
            browse.Anchor=AnchorStyles.Top|AnchorStyles.Right;
            Btn(browse,Color.FromArgb(232,238,247),Color.FromArgb(43,57,77));
            browse.Click+=(s,e)=>ChooseFolder();
            card.Controls.Add(browse);
            var q=L(card,"画质与内容",20,204,120);
            q.Font=new Font(Font,FontStyle.Bold);
            quality.SetBounds(20,232,160,32);
            quality.DropDownStyle=ComboBoxStyle.DropDownList;
            quality.Items.AddRange(new object[] {
                "最佳可用画质","1080p","720p","480p","360p"
            }
            );
            quality.SelectedIndex=0;
            Field(quality);
            card.Controls.Add(quality);
            audio.Text="仅 MP3";
            audio.SetBounds(200,233,88,30);
            card.Controls.Add(audio);
            var autoList=L(card,"检测到 YouTube/B站合集时自动询问",310,235,360);
            autoList.ForeColor=Color.FromArgb(105,118,137);
            login.Text="使用浏览器登录";
            login.SetBounds(20,275,150,30);
            browser.SetBounds(175,273,115,32);
            browser.DropDownStyle=ComboBoxStyle.DropDownList;
            browser.Items.AddRange(new object[] {
                "edge","chrome","firefox"
            }
            );
            browser.SelectedIndex=0;
            Field(browser);
            card.Controls.Add(login);
            card.Controls.Add(browser);
            L(card,"重复文件",320,277,78);
            duplicate.SetBounds(400,273,165,32);
            duplicate.DropDownStyle=ComboBoxStyle.DropDownList;
            duplicate.Items.AddRange(new object[] {
                "跳过已有（推荐）","覆盖已有","自动编号保存"
            }
            );
            duplicate.SelectedIndex=0;
            Field(duplicate);
            card.Controls.Add(duplicate);
            L(card,"完成后",585,277,65);
            completion.SetBounds(650,273,190,32);
            completion.DropDownStyle=ComboBoxStyle.DropDownList;
            completion.Items.AddRange(new object[] {
                "智能询问（推荐）","每个文件都询问","自动打开","从不询问"
            }
            );
            completion.SelectedIndex=0;
            Field(completion);
            card.Controls.Add(completion);
            start.Text="开始下载";
            start.SetBounds(20,316,130,40);
            Btn(start,Color.FromArgb(58,211,175),Color.FromArgb(8,39,35));
            start.Click+=async(s,e)=>await StartAll();
            card.Controls.Add(start);
            cancel.Text="取消";
            cancel.SetBounds(160,316,90,40);
            Btn(cancel,Color.FromArgb(235,239,245),Color.FromArgb(86,98,115));
            cancel.Enabled=false;
            cancel.Click+=(s,e)=>Cancel();
            card.Controls.Add(cancel);
            updateEngine.Text="更新解析器";
            updateEngine.SetBounds(260,316,120,40);
            Btn(updateEngine,Color.FromArgb(235,239,245),Color.FromArgb(60,72,90));
            updateEngine.Click+=async(s,e)=>await UpdateEngine();
            card.Controls.Add(updateEngine);
            var activity=new Panel();
            activity.SetBounds(20,370,838,315);
            activity.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
            activity.BackColor=Color.FromArgb(23,25,30);
            activity.BorderStyle=BorderStyle.FixedSingle;
            card.Controls.Add(activity);
            status=L(activity,"准备好了",14,9,340);
            status.ForeColor=Color.White;
            status.Font=new Font(Font,FontStyle.Bold);
            dots=L(activity,"",355,9,55);
            dots.ForeColor=Color.FromArgb(100,210,255);
            metrics=L(activity,"当前 0%",455,9,210);
            metrics.TextAlign=ContentAlignment.TopRight;
            metrics.ForeColor=Color.FromArgb(160,168,182);
            queueLabel=L(activity,"总任务 0/0",675,9,140);
            queueLabel.TextAlign=ContentAlignment.TopRight;
            queueLabel.ForeColor=Color.FromArgb(160,168,182);
            overall.SetBounds(14,38,808,7);
            overall.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            activity.Controls.Add(overall);
            tasks.SetBounds(14,56,808,118);
            tasks.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            tasks.View=View.Details;
            tasks.FullRowSelect=true;
            tasks.HideSelection=false;
            tasks.BorderStyle=BorderStyle.None;
            tasks.BackColor=Color.FromArgb(23,25,30);
            tasks.ForeColor=Color.FromArgb(220,225,233);
            tasks.Columns.Add("任务",360);
            tasks.Columns.Add("阶段",150);
            tasks.Columns.Add("进度",85);
            tasks.Columns.Add("速度 / 剩余",180);
            tasks.SelectedIndexChanged+=(s,e)=>ShowSelected();
            activity.Controls.Add(tasks);
            details.Multiline=true;
            details.ReadOnly=true;
            details.ScrollBars=ScrollBars.Vertical;
            details.SetBounds(14,184,808,72);
            details.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
            details.BorderStyle=BorderStyle.None;
            details.BackColor=Color.FromArgb(17,19,23);
            details.ForeColor=Color.FromArgb(174,181,194);
            details.Font=new Font("Consolas",9);
            activity.Controls.Add(details);
            retry.Text="重试";
            retry.SetBounds(14,267,72,32);
            openFile.Text="播放";
            openFile.SetBounds(94,267,72,32);
            openFolder.Text="文件夹";
            openFolder.SetBounds(174,267,82,32);
            copyError.Text="复制诊断日志";
            copyError.SetBounds(264,267,130,32);
            clearRecords.Text="清理记录";
            clearRecords.SetBounds(402,267,94,32);
            foreach(var b in new[] {
                retry,openFile,openFolder,copyError,clearRecords
            }
            ) {
                Btn(b,Color.FromArgb(45,49,57),Color.White);
                activity.Controls.Add(b);
            }
            retry.Click+=async(s,e)=>await RetrySelected();
            openFile.Click+=(s,e)=>OpenSelected();
            openFolder.Click+=(s,e)=>OpenFolder();
            copyError.Click+=(s,e)=>CopySelectedError();
            clearRecords.Click+=(s,e)=>ShowClearMenu();
            animation.Interval=330;
            animation.Tick+=(s,e)=> {
                if(!start.Enabled) {
                    dotFrame=(dotFrame+1)%3;
                    dots.Text=new string('.',dotFrame+1);
                }
                else dots.Text="";
            }
            ;
            animation.Start();
            notify.Icon=SystemIcons.Information;
            notify.Visible=true;
            notify.Text="VideoHarvester";
        }
        void ChooseFolder() {
            using(var d=new FolderBrowserDialog()) {
                d.SelectedPath=folder.Text;
                if(d.ShowDialog()==DialogResult.OK)folder.Text=d.SelectedPath;
            }
        }
    }
}
