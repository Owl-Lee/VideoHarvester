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
        void LoadSettings() {
            folder.Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),"VideoHarvester");
            try {
                if(!File.Exists(SettingsPath))return;
                foreach(var line in File.ReadAllLines(SettingsPath)) {
                    var p=line.Split(new[] {
                        '='
                    }
                    ,2);
                    if(p.Length<2)continue;
                    if(p[0]=="folder")folder.Text=p[1];
                    if(p[0]=="quality")quality.SelectedIndex=Math.Max(0,Math.Min(quality.Items.Count-1,Int32.Parse(p[1])));
                    if(p[0]=="browser")browser.SelectedIndex=Math.Max(0,Math.Min(browser.Items.Count-1,Int32.Parse(p[1])));
                    if(p[0]=="duplicate")duplicate.SelectedIndex=Math.Max(0,Math.Min(duplicate.Items.Count-1,Int32.Parse(p[1])));
                    if(p[0]=="completion")completion.SelectedIndex=Math.Max(0,Math.Min(completion.Items.Count-1,Int32.Parse(p[1])));
                    if(p[0]=="login")login.Checked=p[1]=="1";
                    if(p[0]=="audio")audio.Checked=p[1]=="1";
                }
            }
            catch {
            }
        }
        void SaveSettings() {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                File.WriteAllLines(SettingsPath,new[] {
                    "folder="+folder.Text,"quality="+quality.SelectedIndex,"browser="+browser.SelectedIndex,"duplicate="+duplicate.SelectedIndex,"completion="+completion.SelectedIndex,"login="+(login.Checked?"1":"0"),"audio="+(audio.Checked?"1":"0")
                }
                );
            }
            catch {
            }
        }
        void LoadHistory() {
            history.Clear();
            try {
                if(!File.Exists(HistoryPath))return;
                foreach(var line in File.ReadAllLines(HistoryPath)) {
                    int p=line.IndexOf('\t');
                    if(p<=0)continue;
                    string key=line.Substring(0,p),path=line.Substring(p+1);
                    if(File.Exists(path))history[key]=path;
                }
            }
            catch {
            }
        }
        void SaveHistory() {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath));
                var lines=new List<string>();
                foreach(var x in history)if(File.Exists(x.Value))lines.Add(x.Key+"\t"+x.Value);
                File.WriteAllLines(HistoryPath,lines.ToArray(),Encoding.UTF8);
            }
            catch {
            }
        }
        void SaveQueue(bool force) {
            try {
                if(!queueArmed)return;
                if(!force&&(DateTime.Now-lastQueueSave).TotalSeconds<2)return;
                lastQueueSave=DateTime.Now;
                Directory.CreateDirectory(Path.GetDirectoryName(QueuePath));
                var list=new List<Dictionary<string,object>>();
                foreach(var j in jobs) {
                    var d=new Dictionary<string,object>();
                    d["url"]=j.Url;
                    d["title"]=j.Title;
                    d["state"]=j.State;
                    d["file"]=j.File;
                    d["error"]=j.Error;
                    d["resolution"]=j.Resolution;
                    d["size"]=j.Size;
                    d["progress"]=j.Progress;
                    d["whole"]=j.WholeList;
                    d["group"]=j.Group;
                    d["key"]=j.Key;
                    d["requested"]=j.Requested;
                    d["actual"]=j.Actual;
                    d["estimated"]=j.EstimatedBytes;
                    d["groupIndex"]=j.GroupIndex;
                    d["groupCount"]=j.GroupCount;
                    list.Add(d);
                }
                var js=new JavaScriptSerializer();
                js.MaxJsonLength=Int32.MaxValue;
                File.WriteAllText(QueuePath,js.Serialize(list),Encoding.UTF8);
            }
            catch {
            }
        }
        async void RestoreQueue() {
            try {
                if(!File.Exists(QueuePath))return;
                var raw=File.ReadAllText(QueuePath,Encoding.UTF8);
                var js=new JavaScriptSerializer();
                js.MaxJsonLength=Int32.MaxValue;
                var stored=js.DeserializeObject(raw) as object[];
                if(stored==null)return;
                var pending=new List<Dictionary<string,object>>();
                foreach(var o in stored) {
                    var d=o as Dictionary<string,object>;
                    if(d==null)continue;
                    string state=StringValue(d,"state");
                    if(state!="完成"&&state!="已跳过")pending.Add(d);
                }
                if(pending.Count==0)return;
                if(MessageBox.Show("检测到上次未完成的 "+pending.Count+" 个任务。\n\n是否现在继续下载？","恢复任务",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) {
                    File.WriteAllText(QueuePath,"[]",Encoding.UTF8);
                    return;
                }
                queueArmed=true;
                jobs.Clear();
                tasks.Items.Clear();
                var links=new List<string>();
                foreach(var d in pending) {
                    var j=new DownloadJob();
                    j.Url=StringValue(d,"url");
                    j.Title=StringValue(d,"title");
                    j.State="等待恢复";
                    j.File=StringValue(d,"file");
                    j.Error=StringValue(d,"error");
                    j.Resolution=StringValue(d,"resolution");
                    j.Size=StringValue(d,"size");
                    j.Progress=0;
                    j.WholeList=BoolValue(d,"whole");
                    j.Group=StringValue(d,"group");
                    j.Key=StringValue(d,"key");
                    j.Requested=StringValue(d,"requested");
                    j.Actual=StringValue(d,"actual");
                    j.EstimatedBytes=LongObject(d,"estimated");
                    j.GroupIndex=IntObject(d,"groupIndex");
                    j.GroupCount=IntObject(d,"groupCount");
                    string old;
                    if(history.TryGetValue(j.Key,out old)&&File.Exists(old)) {
                        j.Existing=true;
                        j.File=old;
                    }
                    j.Row=new ListViewItem(new[] {
                        j.Title,j.State,"0%","--"
                    }
                    );
                    j.Row.Tag=j;
                    jobs.Add(j);
                    tasks.Items.Add(j.Row);
                    if(!links.Contains(j.Url))links.Add(j.Url);
                }
                urls.Lines=links.ToArray();
                SetStage("已恢复上次任务");
                SaveQueue(true);
                await RunJobs(new List<DownloadJob>(jobs));
            }
            catch(Exception ex) {
                details.Text="上次的任务记录无法读取，已忽略。\r\n"+ex.Message;
            }
        }
        void OnClosing(object sender,FormClosingEventArgs e) {
            SaveSettings();
            bool unfinished=queueArmed&&jobs.Exists(j=>j.State!="完成"&&j.State!="已跳过");
            if(!closingApproved&&(!start.Enabled||unfinished)) {
                if(MessageBox.Show("还有未完成的任务。现在退出会保留队列，下次打开可以继续。\n\n确定退出吗？","退出 VideoHarvester",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) {
                    e.Cancel=true;
                    return;
                }
                closingApproved=true;
            }
            SaveQueue(true);
            try {
                if(current!=null&&!current.HasExited)current.Kill();
            }
            catch {
            }
            notify.Visible=false;
        }
        string StringValue(Dictionary<string,object>d,string key) {
            return d.ContainsKey(key)&&d[key]!=null?d[key].ToString():"";
        }
        bool BoolValue(Dictionary<string,object>d,string key) {
            bool v;
            return d.ContainsKey(key)&&d[key]!=null&&Boolean.TryParse(d[key].ToString(),out v)&&v;
        }
        int IntObject(Dictionary<string,object>d,string key) {
            int v;
            return d.ContainsKey(key)&&d[key]!=null&&Int32.TryParse(d[key].ToString(),out v)?v:0;
        }
        long LongObject(Dictionary<string,object>d,string key) {
            long v;
            return d.ContainsKey(key)&&d[key]!=null&&Int64.TryParse(d[key].ToString(),out v)?v:0;
        }
    }
}
