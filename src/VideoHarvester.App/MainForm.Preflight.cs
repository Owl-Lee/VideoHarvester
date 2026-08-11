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
        async Task PreflightAll() {
            SetStage("正在分析画质与文件大小");
            int samples=jobs.Count>1?Math.Min(3,jobs.Count):jobs.Count;
            long sampled=0;
            int known=0;
            for(int i=0;i<jobs.Count;i++) {
                var j=jobs[i];
                j.State="预检中";
                UpdateRow(j);
                if(i<samples||jobs.Count<=3) {
                    await PreflightJob(j);
                    if(j.EstimatedBytes>0) {
                        sampled+=j.EstimatedBytes;
                        known++;
                    }
                }
                else {
                    j.State="等待下载";
                    UpdateRow(j);
                }
            }
            if(jobs.Count>samples&&known>0) {
                long avg=sampled/known;
                foreach(var j in jobs)if(j.EstimatedBytes==0)j.EstimatedBytes=avg;
            }
        }
        async Task PreflightJob(DownloadJob j) {
            if(j.WholeList) {
                string flat="--flat-playlist --dump-single-json --skip-download --yes-playlist --no-warnings"+(login.Checked?" --cookies-from-browser "+browser.SelectedItem:"")+" \""+j.Url.Replace("\"","")+"\"";
                var listData=await RunCapture(engine,flat);
                j.Diagnostic.AppendLine("PREFLIGHT LIST ARGS: "+flat);
                j.Diagnostic.AppendLine(listData.Item2);
                try {
                    var js=new JavaScriptSerializer();
                    js.MaxJsonLength=Int32.MaxValue;
                    var p=js.DeserializeObject(listData.Item1) as Dictionary<string,object>;
                    if(p!=null&&p.ContainsKey("title")) {
                        j.Title=p["title"].ToString();
                        j.Group=j.Title;
                    }
                    var entries=p!=null&&p.ContainsKey("entries")?p["entries"] as object[]:null;
                    j.GroupCount=entries==null?0:entries.Length;
                    j.Actual="逐项确认";
                    j.State="预检完成";
                    UpdateRow(j);
                    return;
                }
                catch(Exception ex) {
                    j.Diagnostic.AppendLine(ex.ToString());
                }
            }
            string limit=quality.SelectedIndex==0?"":"[height<="+quality.SelectedItem.ToString().Replace("p","")+"]";
            string fmt=audio.Checked?"ba/b":"bv*"+limit+"+ba/b"+limit;
            string args="--dump-single-json --skip-download --no-warnings --no-playlist -f \""+fmt+"\" --js-runtimes \"deno:"+Path.Combine(root,"deno.exe")+"\""+(login.Checked?" --cookies-from-browser "+browser.SelectedItem:"")+" \""+j.Url.Replace("\"","")+"\"";
            var output=await RunCapture(engine,args);
            j.Diagnostic.AppendLine("PREFLIGHT ARGS: "+args);
            j.Diagnostic.AppendLine(output.Item2);
            if(output.Item1=="") {
                j.State="预检失败";
                j.Error=TranslateTechnical(output.Item2);
                UpdateRow(j);
                return;
            }
            try {
                var js=new JavaScriptSerializer();
                js.MaxJsonLength=Int32.MaxValue;
                var data=js.DeserializeObject(output.Item1) as Dictionary<string,object>;
                if(data.ContainsKey("title"))j.Title=data["title"].ToString();
                if(j.Key.StartsWith("URL:")&&data.ContainsKey("id")) {
                    string extractor=data.ContainsKey("extractor_key")?data["extractor_key"].ToString():"media";
                    j.Key=extractor+":"+data["id"];
                }
                int height=0;
                long bytes=0;
                if(data.ContainsKey("requested_downloads")) {
                    var downloads=data["requested_downloads"] as object[];
                    if(downloads!=null)foreach(var item in downloads) {
                        var d=item as Dictionary<string,object>;
                        if(d==null)continue;
                        height=Math.Max(height,IntValue(d,"height"));
                        long exact=LongValue(d,"filesize");
                        bytes+=exact>0?exact:LongValue(d,"filesize_approx");
                    }
                }
                if(height==0)height=IntValue(data,"height");
                j.Actual=audio.Checked?"MP3 音频":height>0?height+"p":"下载时确认";
                j.EstimatedBytes=bytes;
                j.State="预检完成";
                string old;
                if(history.TryGetValue(j.Key,out old)&&File.Exists(old)) {
                    j.Existing=true;
                    j.File=old;
                    j.State="已下载";
                }
                UpdateRow(j);
            }
            catch(Exception ex) {
                j.Diagnostic.AppendLine(ex.ToString());
                j.State="预检完成";
                j.Actual="下载时确认";
                UpdateRow(j);
            }
        }
        async Task<Tuple<string,string>> RunCapture(string file,string args) {
            var p=new Process();
            p.StartInfo=new ProcessStartInfo(file,args) {
                UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8
            }
            ;
            p.Start();
            var a=p.StandardOutput.ReadToEndAsync();
            var b=p.StandardError.ReadToEndAsync();
            await Task.WhenAll(a,b);
            p.WaitForExit();
            return Tuple.Create(a.Result,b.Result);
        }
        int IntValue(Dictionary<string,object>d,string key) {
            if(!d.ContainsKey(key)||d[key]==null)return 0;
            int x;
            return Int32.TryParse(d[key].ToString(),out x)?x:0;
        }
        long LongValue(Dictionary<string,object>d,string key) {
            if(!d.ContainsKey(key)||d[key]==null)return 0;
            long x;
            return Int64.TryParse(d[key].ToString().Split('.')[0],out x)?x:0;
        }
        bool ConfirmPreflight() {
            long bytes=0;
            int existing=0;
            foreach(var j in jobs) {
                bytes+=j.EstimatedBytes;
                if(j.Existing)existing++;
            }
            int count=jobs.Count==1&&jobs[0].GroupCount>1?jobs[0].GroupCount:jobs.Count;
            bool collection=count>1;
            string sharedGroup=jobs.Count>0?jobs[0].Group:"";
            bool sameGroup=sharedGroup!=""&&jobs.TrueForAll(j=>j.Group==sharedGroup);
            string title=jobs.Count==1?jobs[0].Title:sameGroup?sharedGroup:"多个下载任务";
            string actual=jobs.Count>0?jobs[0].Actual:"下载时确认";
            string downgrade="";
            int requested=quality.SelectedIndex==0?0:Int32.Parse(quality.SelectedItem.ToString().Replace("p",""));
            int actualN=0;
            Int32.TryParse(actual.Replace("p",""),out actualN);
            if(requested>0&&actualN>0&&actualN<requested)downgrade="\n画质提示：请求 "+requested+"p，预计实际 "+actualN+"p；可能受原视频或账号权限限制。";
            string size=bytes>0?FormatBytes(bytes)+"（估算）":"暂时无法预估";
            string save=sameGroup?Path.Combine(folder.Text,SafeName(sharedGroup)):folder.Text;
            long free=FreeSpace(folder.Text);
            string spaceWarning=free>0&&bytes>free?"\n⚠ 当前磁盘空间可能不足。":"";
            string text="已识别：《"+title+"》\n类型："+(collection?"合集，共 "+count+" 个视频":"单个视频")+"\n内容："+(audio.Checked?"MP3 音频":"视频")+"\n请求画质："+(audio.Checked?"不适用":quality.SelectedItem.ToString())+"\n预计实际画质："+actual+downgrade+"\n登录状态："+(login.Checked?"使用 "+browser.SelectedItem+" 登录信息":"未启用")+"\n预计大小："+size+"\n磁盘剩余："+(free>0?FormatBytes(free):"无法读取")+spaceWarning+"\n已下载："+existing+" 个\n保存位置："+save+"\n\n确认开始下载吗？";
            return MessageBox.Show(text,"下载前确认",MessageBoxButtons.YesNo,bytes>0&&free>0&&bytes>free?MessageBoxIcon.Warning:MessageBoxIcon.Information)==DialogResult.Yes;
        }
        string FormatBytes(long n) {
            return DownloadRules.FormatBytes(n);
        }
        long FreeSpace(string path) {
            try {
                string full=Path.GetFullPath(path);
                return new DriveInfo(Path.GetPathRoot(full)).AvailableFreeSpace;
            }
            catch {
                return 0;
            }
        }
        string SafeName(string text) {
            return DownloadRules.CreateSafeFileName(text);
        }
        string MakeKey(string url) {
            return DownloadRules.CreateDownloadKey(url);
        }
    }
}
