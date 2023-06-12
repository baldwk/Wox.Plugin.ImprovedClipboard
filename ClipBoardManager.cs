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
using System.Security.Cryptography;
using System.Web.Script.Serialization;

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
        private HashSet<string> imgHashList;
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
            imgHashList = new HashSet<string>();

            foreach (FileInfo file in di.EnumerateFiles())
            {
                var img = GetImage(file.FullName);
                string hash = genHash(img);
                JavaScriptSerializer js = new JavaScriptSerializer();
                string s = js.Serialize(img);
                Loggers.Logger.LogInfo(s);
                if (imgHashList.Contains(hash))
                {
                    continue;
                }
                imgHashList.Add(hash);
                imgPathList.Add(file.FullName);
            }

            this.setting = setting;
        }
        public void Init()
        {
            ClipboardMonitor.OnClipboardChange += onclipboardChanged;
            ClipboardMonitor.Start();
        }
        private string genHash(Image image)
        {
            MemoryStream ms = new MemoryStream();
            Bitmap bmp = new Bitmap(image);
            bmp.Save(ms,System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] imgBytes = ms.ToArray();
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(imgBytes);
            string imageMD5 = BitConverter.ToString(hash).Replace("-", "").ToLower();
            ms.Dispose();
            return imageMD5;
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
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string s = serializer.Serialize(data);
                Loggers.Logger.LogInfo("clipboard: " + s);

                Bitmap bmp = (Bitmap)data;
                string hash = genHash(bmp);
                // avoid duplicate images
                if (imgHashList.Contains(hash))
                {
                    return;
                }
                
                string path = Path.Combine(SaveDir, DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".bmp");
                // save to file because result only accept icon path
                // img.GetThumbnailImage(50, 50, () => { return false; }, IntPtr.Zero).Save(path);
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);
                imgPathList.Insert(0,path);
                imgHashList.Add(hash);
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
            imgHashList.Clear();
        }
        public void ClearImage(string path)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(SaveDir);

            imgPathList.Remove(path);
            imgHashList.Remove(genHash(GetImage(path)));
            foreach (FileInfo file in di.EnumerateFiles())
            {
                if (file.FullName == path) {
                    file.Delete();
                }
            }
        }
    }
}
