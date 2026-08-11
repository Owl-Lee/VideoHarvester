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
        TextBox urls=new TextBox(), folder=new TextBox(), details=new TextBox();
        ComboBox quality=new ComboBox(), browser=new ComboBox(), duplicate=new ComboBox(), completion=new ComboBox();
        CheckBox login=new CheckBox(), audio=new CheckBox();
        Button start=new Button(),cancel=new Button(),browse=new Button(),retry=new Button(),openFile=new Button(),openFolder=new Button(),copyError=new Button(),clearRecords=new Button(),updateEngine=new Button();
        ProgressBar overall=new ProgressBar();
        Label status=new Label(),metrics=new Label(),queueLabel=new Label(),dots=new Label();
        ListView tasks=new ListView();
        Timer animation=new Timer();
        NotifyIcon notify=new NotifyIcon();
        Process current;
        string root, engine, currentFile="", currentTitle="", detectedGroupName="";
        int dotFrame;
        DownloadJob active;
        List<DownloadJob> jobs=new List<DownloadJob>();
        List<string> completedFiles=new List<string>();
        List<string> runFiles=new List<string>();
        Dictionary<string,string> history=new Dictionary<string,string>();
        DateTime lastQueueSave=DateTime.MinValue;
        bool closingApproved, suppressRemainingPrompts, currentRunIsBatch, queueArmed;
        int runSuccess, runFailed, runSkipped;
        string SettingsPath {
            get {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VideoHarvester","settings.txt");
            }
        }
        string QueuePath {
            get {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VideoHarvester","queue.json");
            }
        }
        string HistoryPath {
            get {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VideoHarvester","history.txt");
            }
        }
        public MainForm() {
            Text="VideoHarvester";
            Width=940;
            Height=850;
            MinimumSize=new Size(860,760);
            StartPosition=FormStartPosition.CenterScreen;
            AutoScaleMode=AutoScaleMode.Dpi;
            Font=new Font("Microsoft YaHei UI",10);
            BackColor=Color.FromArgb(244,247,251);
            ForeColor=Color.FromArgb(25,34,48);
            root=AppDomain.CurrentDomain.BaseDirectory;
            engine=Path.Combine(root,"yt-dlp.exe");
            Build();
            LoadSettings();
            LoadHistory();
            Shown+=(s,e)=>RestoreQueue();
            FormClosing+=OnClosing;
        }
        Label L(Control p,string t,int x,int y,int w) {
            var l=new Label();
            l.Text=t;
            l.SetBounds(x,y,w,28);
            l.ForeColor=Color.FromArgb(49,61,78);
            l.BackColor=Color.Transparent;
            p.Controls.Add(l);
            return l;
        }
        void Field(Control c) {
            c.BackColor=Color.FromArgb(248,250,253);
            c.ForeColor=Color.FromArgb(22,31,45);
            if(c is TextBox)((TextBox)c).BorderStyle=BorderStyle.FixedSingle;
        }
        void Btn(Button b,Color bg,Color fg) {
            b.FlatStyle=FlatStyle.Flat;
            b.FlatAppearance.BorderSize=0;
            b.BackColor=bg;
            b.ForeColor=fg;
            b.Cursor=Cursors.Hand;
        }
    }
}
