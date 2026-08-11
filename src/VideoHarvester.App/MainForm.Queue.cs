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
        async Task StartAll() {
            var list=new List<string>();
            foreach(var x in urls.Lines) {
                var u=x.Trim();
                if(u!=""&&!list.Contains(u))list.Add(u);
            }
            if(list.Count==0) {
                MessageBox.Show("请粘贴至少一个链接。");
                return;
            }
            queueArmed=false;
            jobs.Clear();
            tasks.Items.Clear();
            details.Clear();
            completedFiles.Clear();
            runFiles.Clear();
            start.Enabled=false;
            try {
                SetStage("正在准备解析组件");
                await EnsureTools();
                foreach(var u in list) {
                    if(IsYouTubeList(u)) {
                        var pure=u.IndexOf("/playlist",StringComparison.OrdinalIgnoreCase)>=0;
                        var text=pure?"检测到 YouTube 播放列表。\n\n是否下载整个播放列表？":"检测到当前视频属于 YouTube 播放列表。\n\n是否下载整个播放列表？\n选择“否”将只下载当前视频。";
                        var choice=MessageBox.Show(text,"发现播放列表",pure?MessageBoxButtons.YesNo:MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
                        if(choice==DialogResult.Cancel||pure&&choice==DialogResult.No)continue;
                        if(choice==DialogResult.Yes) {
                            var expanded=await ExpandYouTubeList(u);
                            if(expanded.Count>0) {
                                for(int i=0;i<expanded.Count;i++) {
                                    var j=AddJob(expanded[i].Item1,false,detectedGroupName,i+1,expanded.Count);
                                    j.Title=expanded[i].Item2;
                                    UpdateRow(j);
                                }
                            }
                            else AddJob(u,true,"YouTube 播放列表",0,0);
                        }
                        else AddJob(u,false,"",0,0);
                    }
                    else {
                        var expanded=await DetectBiliCollection(u);
                        for(int i=0;i<expanded.Count;i++)AddJob(expanded[i],false,expanded.Count>1?detectedGroupName:"",i+1,expanded.Count);
                    }
                }
                if(jobs.Count==0)return;
                await PreflightAll();
                if(!ConfirmPreflight())return;
                queueArmed=true;
                SaveSettings();
                SaveQueue(true);
                await RunJobs(new List<DownloadJob>(jobs));
            }
            catch(Exception ex) {
                details.Text="解析链接失败，请检查网络；需要排查时可复制诊断日志。";
                SetStage("预检失败");
                if(active!=null)active.Diagnostic.AppendLine(ex.ToString());
            }
            finally {
                start.Enabled=true;
            }
        }
        DownloadJob AddJob(string url,bool whole,string group,int index,int count) {
            var j=new DownloadJob {
                Url=url,WholeList=whole,Group=group,GroupIndex=index,GroupCount=count,Key=MakeKey(url),Requested=quality.SelectedItem.ToString()
            }
            ;
            string old;
            if(history.TryGetValue(j.Key,out old)&&File.Exists(old)) {
                j.Existing=true;
                j.File=old;
                j.State="已下载";
            }
            j.Row=new ListViewItem(new[] {
                j.Title,j.State,"0%","--"
            }
            );
            j.Row.Tag=j;
            jobs.Add(j);
            tasks.Items.Add(j.Row);
            return j;
        }
        bool IsYouTubeList(string url) {
            return DownloadRules.IsYouTubePlaylist(url);
        }
        async Task<List<Tuple<string,string>>> ExpandYouTubeList(string url) {
            detectedGroupName="YouTube 播放列表";
            var result=new List<Tuple<string,string>>();
            try {
                SetStage("正在读取 YouTube 播放列表");
                string args="--flat-playlist --dump-single-json --skip-download --yes-playlist --no-warnings"+(login.Checked?" --cookies-from-browser "+browser.SelectedItem:"")+" \""+url.Replace("\"","")+"\"";
                var captured=await RunCapture(engine,args);
                if(String.IsNullOrWhiteSpace(captured.Item1))return result;
                var js=new JavaScriptSerializer();
                js.MaxJsonLength=Int32.MaxValue;
                var data=js.DeserializeObject(captured.Item1) as Dictionary<string,object>;
                if(data!=null&&data.ContainsKey("title"))detectedGroupName=data["title"].ToString();
                var entries=data!=null&&data.ContainsKey("entries")?data["entries"] as object[]:null;
                if(entries==null)return result;
                foreach(var item in entries) {
                    var e=item as Dictionary<string,object>;
                    if(e==null)continue;
                    string id=e.ContainsKey("id")?e["id"].ToString():"";
                    string link=e.ContainsKey("webpage_url")?e["webpage_url"].ToString():e.ContainsKey("url")?e["url"].ToString():"";
                    if(link!=""&&!link.StartsWith("http",StringComparison.OrdinalIgnoreCase)&&id!="")link="https://www.youtube.com/watch?v="+id;
                    if(link==""&&id!="")link="https://www.youtube.com/watch?v="+id;
                    if(link=="")continue;
                    string title=e.ContainsKey("title")?e["title"].ToString():"等待解析";
                    result.Add(Tuple.Create(link,title));
                }
            }
            catch(Exception ex) {
                details.Text="播放列表暂时无法完整展开，将交给下载器继续处理。";
                if(active!=null)active.Diagnostic.AppendLine(ex.ToString());
            }
            return result;
        }
        async Task<List<string>> DetectBiliCollection(string url) {
            detectedGroupName="";
            var result=new List<string> {
                url
            }
            ;
            var m=Regex.Match(url,@"bilibili\.com/video/(BV[0-9A-Za-z]+)",RegexOptions.IgnoreCase);
            if(!m.Success)return result;
            try {
                string bvid=m.Groups[1].Value;
                using(var wc=new WebClient()) {
                    wc.Encoding=Encoding.UTF8;
                    wc.Headers[HttpRequestHeader.UserAgent]="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
                    wc.Headers[HttpRequestHeader.Referer]=url;
                    string raw=await wc.DownloadStringTaskAsync(new Uri("https://api.bilibili.com/x/web-interface/view?bvid="+bvid));
                    var rootObj=new JavaScriptSerializer().DeserializeObject(raw) as Dictionary<string,object>;
                    var data=rootObj["data"] as Dictionary<string,object>;
                    var candidates=new List<string>();
                    string label="B站合集";
                    if(data.ContainsKey("ugc_season")&&data["ugc_season"]!=null) {
                        var season=data["ugc_season"] as Dictionary<string,object>;
                        if(season!=null&&season.ContainsKey("title"))label=season["title"].ToString();
                        var sections=season["sections"] as object[];
                        if(sections!=null)foreach(var so in sections) {
                            var section=so as Dictionary<string,object>;
                            var episodes=section["episodes"] as object[];
                            if(episodes!=null)foreach(var eo in episodes) {
                                var ep=eo as Dictionary<string,object>;
                                if(ep!=null&&ep.ContainsKey("bvid")) {
                                    string v=ep["bvid"].ToString();
                                    if(!candidates.Contains(v))candidates.Add(v);
                                }
                            }
                        }
                    }
                    else if(data.ContainsKey("pages")) {
                        var pages=data["pages"] as object[];
                        if(pages!=null&&pages.Length>1)for(int i=1;i<=pages.Length;i++)candidates.Add(bvid+"?p="+i);
                    }
                    if(candidates.Count>1) {
                        var choice=MessageBox.Show("检测到“"+label+"”，共 "+candidates.Count+" 个视频。\n\n是否下载整个合集？\n选择“否”将只下载当前视频；纯列表链接会跳过该任务。","发现 B站合集",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
                        if(choice==DialogResult.Yes) {
                            detectedGroupName=label;
                            result.Clear();
                            foreach(var v in candidates)result.Add("https://www.bilibili.com/video/"+v);
                        }
                    }
                }
            }
            catch(Exception) {
                details.Text="暂时无法确认该链接是否属于合集，将按当前视频处理。";
            }
            return result;
        }
        async Task RunJobs(List<DownloadJob> list) {
            if(list==null||list.Count==0)return;
            start.Enabled=false;
            cancel.Enabled=true;
            updateEngine.Enabled=false;
            overall.Value=0;
            metrics.Text="当前 0%";
            queueLabel.Text="总任务 0/"+list.Count;
            runFiles.Clear();
            runSuccess=runFailed=runSkipped=0;
            suppressRemainingPrompts=false;
            currentRunIsBatch=list.Count>1||list.Exists(x=>x.WholeList||x.GroupCount>1);
            try {
                await EnsureTools();
                Directory.CreateDirectory(folder.Text);
                int i=0;
                foreach(var j in list) {
                    active=j;
                    i++;
                    queueLabel.Text="总任务 "+i+"/"+list.Count;
                    string old;
                    if(duplicate.SelectedIndex==0&&history.TryGetValue(j.Key,out old)&&File.Exists(old)) {
                        j.Existing=true;
                        j.File=old;
                        j.Progress=100;
                        j.State="已跳过";
                        SetFriendly(j,"以前已经下载过这个视频，本次已自动跳过。");
                        UpdateRow(j);
                        runSkipped++;
                        SaveQueue(true);
                        continue;
                    }
                    if(history.TryGetValue(j.Key,out old)&&!File.Exists(old)) {
                        history.Remove(j.Key);
                        j.Existing=false;
                        SaveHistory();
                    }
                    await RunJob(j);
                    if(j.State=="完成") {
                        var files=new List<string>();
                        foreach(var file in completedFiles)if(File.Exists(file)&&!files.Contains(file))files.Add(file);
                        if(files.Count==0&&File.Exists(j.File))files.Add(j.File);
                        runSuccess+=Math.Max(1,files.Count);
                        foreach(var file in files) {
                            if(!runFiles.Contains(file))runFiles.Add(file);
                            if(completion.SelectedIndex==1&&!suppressRemainingPrompts)AskOpenFile(file,true);
                            else if(completion.SelectedIndex==2)OpenPath(file);
                        }
                    }
                    else if(j.State=="失败")runFailed++;
                    SaveQueue(true);
                }
                SetStage(runFailed>0?"任务处理完成 · 有失败项目":"全部任务处理完成");
                queueLabel.Text="总任务 "+list.Count+"/"+list.Count;
                Notify("下载任务已处理完成");
                if(completion.SelectedIndex==0) {
                    if(currentRunIsBatch)ShowBatchSummary();
                    else if(runFiles.Count==1)AskOpenFile(runFiles[0],false);
                }
            }
            catch(Exception ex) {
                SetStage("任务失败");
                details.Text="程序遇到问题，请点击“复制诊断日志”后发送给开发者。";
                if(active!=null) {
                    active.Diagnostic.AppendLine(ex.ToString());
                    active.Error="程序运行失败，请复制诊断日志。";
                }
                Notify("任务失败，请查看软件提示");
            }
            finally {
                start.Enabled=true;
                cancel.Enabled=false;
                updateEngine.Enabled=true;
                current=null;
                dots.Text="";
                SaveQueue(true);
            }
        }
        async Task RunJob(DownloadJob j) {
            j.State="正在解析";
            j.Row.Selected=true;
            j.Row.EnsureVisible();
            overall.Value=0;
            metrics.Text="当前 0%";
            UpdateRow(j);
            SetStage("正在解析网页");
            currentFile="";
            currentTitle="";
            completedFiles.Clear();
            var tcs=new TaskCompletionSource<int>();
            string limit=quality.SelectedIndex==0?"":"[height<="+quality.SelectedItem.ToString().Replace("p","")+"]";
            string fmt=audio.Checked?"ba/b":"bv*"+limit+"+ba/b"+limit;
            string baseName=duplicate.SelectedIndex==2?"%(title).160B [%(id)s]-%(epoch)s.%(ext)s":"%(title).170B [%(id)s].%(ext)s";
            string target=folder.Text,output=baseName;
            if(j.WholeList)output="%(playlist).100B/%(playlist_index)03d - "+baseName;
            else if(j.Group!="") {
                target=Path.Combine(folder.Text,SafeName(j.Group));
                int digits=Math.Max(2,j.GroupCount.ToString().Length);
                output=(j.GroupIndex>0?j.GroupIndex.ToString("D"+digits)+" - ":"")+baseName;
            }
            Directory.CreateDirectory(target);
            string args="--encoding utf-8 --newline --windows-filenames --continue --retries 8 --fragment-retries 8 --ffmpeg-location \""+Path.Combine(root,"ffmpeg.exe")+"\" --js-runtimes \"deno:"+Path.Combine(root,"deno.exe")+"\" -f \""+fmt+"\" --print \"before_dl:VH_TITLE:%(title)s\" --print \"after_move:VH_FILE:%(filepath)s\"";
            args+=" --print \"after_move:VH_META:%(resolution)s\"";
            args+=j.WholeList?" --yes-playlist":" --no-playlist";
            if(audio.Checked)args+=" -x --audio-format mp3 --audio-quality 0";
            else args+=" --merge-output-format mp4";
            if(login.Checked)args+=" --cookies-from-browser "+browser.SelectedItem;
            if(duplicate.SelectedIndex==1)args+=" --force-overwrites";
            else args+=" --no-overwrites";
            args+=" -o \""+Path.Combine(target,output)+"\" \""+j.Url.Replace("\"","")+"\"";
            if(login.Checked) {
                var ps=Process.GetProcessesByName(browser.SelectedItem.ToString());
                if(ps.Length>0)SetFriendly(j,"检测到浏览器正在运行；如登录读取失败，请关闭浏览器后重试。");
            }
            j.Diagnostic.AppendLine("URL: "+j.Url);
            j.Diagnostic.AppendLine("ARGS: "+args);
            SetFriendly(j,"正在分析视频页面…");
            current=new Process();
            current.StartInfo=new ProcessStartInfo(engine,args) {
                UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8
            }
            ;
            DataReceivedEventHandler h=(s,e)=> {
                if(e.Data==null)return;
                BeginInvoke((Action)(()=>HandleLine(j,e.Data)));
            }
            ;
            current.OutputDataReceived+=h;
            current.ErrorDataReceived+=h;
            current.EnableRaisingEvents=true;
            current.Exited+=(s,e)=>tcs.TrySetResult(current.ExitCode);
            current.Start();
            current.BeginOutputReadLine();
            current.BeginErrorReadLine();
            int code=await tcs.Task;
            current.WaitForExit();
            await Task.Delay(150);
            if(code==0) {
                j.State="完成";
                j.Progress=100;
                j.File=currentFile;
                if(File.Exists(j.File))j.Size=(new FileInfo(j.File).Length/1048576.0).ToString("0.0")+" MB";
                if(j.File!=""&&File.Exists(j.File)) {
                    history[j.Key]=j.File;
                    SaveHistory();
                }
                SetFriendly(j,"下载完成，文件已经保存。");
                UpdateRow(j);
                j.Row.Selected=true;
                j.Row.EnsureVisible();
                ShowSelected();
            }
            else {
                j.State="失败";
                if(String.IsNullOrEmpty(j.Error))j.Error="下载失败，请复制诊断日志后发送给开发者。";
                SetFriendly(j,j.Error);
                UpdateRow(j);
                ShowSelected();
            }
        }
    }
}
