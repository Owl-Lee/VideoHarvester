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
    internal sealed class DownloadJob {
        public string Url, Title="等待解析", State="等待", File="", Error="";
        public string Resolution="--", Size="--";
        public int Progress;
        public bool WholeList;
        public StringBuilder Diagnostic=new StringBuilder();
        public List<string> Friendly=new List<string>();
        public string Group="", Key="", Requested="", Actual="待确认";
        public long EstimatedBytes;
        public int GroupIndex, GroupCount;
        public bool Existing;
        public ListViewItem Row;
    }
}
