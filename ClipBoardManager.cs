using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;


namespace Wox.Plugin.ImprovedClipboard
{
    public enum ResultType
    {
        Text,
        Image,
        Clear
    }
    public class SearchResult
    {
        public ResultType Type;
        public string Name;
        public string Path;
        public string Text;
        public Image Image;
    }
    
       class ClipboardManager
    {
        
        public string SaveDir;
        private List<string> textList;
        private List<string> imgPathList;
        private Setting setting;

        [DllImport("kernel32.dll")]
        public static extern IntPtr _lopen(string lpPathName, int iReadWrite);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        public const int OF_READWRITE = 2;
        public const int OF_SHARE_DENY_NONE = 0x40;
        public readonly IntPtr HFILE_ERROR = new IntPtr(-1);
        public void check(string p)
        {
            if (!File.Exists(p))
            {
                return;
            }
            IntPtr vHandle = _lopen(p, OF_READWRITE | OF_SHARE_DENY_NONE);
            if (vHandle == HFILE_ERROR)
            {
                Loggers.Logger.LogInfo(p + " occupied");
                return;
            }
            CloseHandle(vHandle);
            Loggers.Logger.LogInfo(p + " not occupied");
        }

        public ClipboardManager(string dir, Setting setting)
        {
            this.textList = new List<string>();
            this.imgPathList = new List<string>();
            // there's a bug in wox
            // loading the image in icoPath will lock the file when icoPath is in Wox dir
            System.IO.DirectoryInfo di = new DirectoryInfo(dir);
            di = di.Parent.Parent.Parent; // -> plugins -> wox -> parent
            SaveDir = Path.Combine(di.FullName, "clipboard_images");
            di = new DirectoryInfo(SaveDir);
            if (!di.Exists)
            {
                di.Create();
            }

            foreach (FileInfo file in di.EnumerateFiles())
            {
                imgPathList.Add(file.FullName);
            }
            
            this.setting = setting;
        }
        public void Init()
        {
            ClipboardMonitor.OnClipboardChange += onclipboardChanged;
            ClipboardMonitor.Start();
        }

        private void onclipboardChanged(ClipboardFormat format, object data)
        {

            if (format == ClipboardFormat.Html ||
                format == ClipboardFormat.SymbolicLink ||
                format == ClipboardFormat.Text ||
                format == ClipboardFormat.UnicodeText)
            {
                string s = data.ToString().Trim();
                if (s.Length == 0) return;
                textList.Remove(s);
                textList.Insert(0,s);
            }
            else if (setting.EnableImgSearch && format == ClipboardFormat.Bitmap)
            {
                Bitmap bmp = (Bitmap)data;
                
                string path = Path.Combine(SaveDir, DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".bmp");
                // save to file because result only accept icon path
                // img.GetThumbnailImage(50, 50, () => { return false; }, IntPtr.Zero).Save(path);
                bmp.Save(path);
                imgPathList.Insert(0,path);
            }
        }
        public static bool IsSubsequence(string s, string t)
        {
            var sIndex = 0;
            for (var tIndex = 0; tIndex < t.Length; tIndex++)
            {
                if (s[sIndex] == t[tIndex])
                    sIndex++;

                if (sIndex == s.Length)
                    return true;
            }
            return false;
        }

        public List<SearchResult> Search(string q)
        {
            List<string> res = textList;

            List<SearchResult> ret = new List<SearchResult>();
            if (q != "")
            {
                if (setting.SearchType == "SubString")
                {
                    res = res.Where(i => i.ToLower().Contains(q.ToLower())).ToList();
                }else if(setting.SearchType == "SubSequence")
                {
                    res = res.Where(i => IsSubsequence(q,i.ToLower())).ToList();
                }else if(setting.SearchType == "Regex")
                {
                    res = res.Where(i => Regex.Match(i.ToLower(),q).Success).ToList();
                }
                
            }
            ret.AddRange(res.Select(o =>
                 new SearchResult
                 {
                     Type = ResultType.Text,
                     Name = o.Length > 40 ? o.Substring(0, 40) +" ..." : o,
                     Text = o,
                 }
            )) ;
            return ret;
        }
        private Image GetImage(string path)
        {
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var img = Image.FromStream(fileStream);
            fileStream.Close();
            fileStream.Dispose();
            return img;
        }
        public List<SearchResult> GetImgList()
        {
            List<SearchResult> ret = new List<SearchResult>();
            int cnt = 0;
            foreach(string p in imgPathList)
            {
                if (File.Exists(p))
                {
                    ret.Add(new SearchResult
                    {
                        Type = ResultType.Image,
                        Name = Path.GetFileName(p),
                        Path = p,
                        Image = GetImage(p),
                    });
                    cnt++;
                }
                // more than 20 will not be displayed
                if (cnt > 20) break;
            }
            return ret;
        }
        public void ClearText(string target)
        {
            textList.Remove(target);
        }
        public void ClearText()
        {
            textList.Clear();
        }
        
        public void ClearImage()
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(SaveDir);

            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            imgPathList.Clear();
        }
        public void ClearImage(string path)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(SaveDir);

            imgPathList.Remove(path);
            foreach (FileInfo file in di.EnumerateFiles())
            {
                if (file.FullName == path) {
                    file.Delete();
                }
            }
        }
    }
}
