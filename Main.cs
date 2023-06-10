using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Control = System.Windows.Controls.Control;


namespace Wox.Plugin.ImprovedClipboard
{

    public class Main : IPlugin, IContextMenu, ISettingProvider
    {
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);


        private PluginInitContext context;
        private ImprovedClipboard.ClipboardManager clipboardManager;
        public const string icoPath = "Images\\clipboard.png";
        List<string> dataList = new List<string>();

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            ImprovedClipboard.SearchResult record = selectedResult.ContextData as ImprovedClipboard.SearchResult;
            List<Result> contextMenus = new List<Result>();
            if (record == null) return contextMenus;



            switch (record.Type)
            {
                case ImprovedClipboard.ResultType.Text:
                    contextMenus.Add(new Result
                    {
                        Title = "Copy",
                        Action = (context) =>
                        {
                            System.Windows.Forms.Clipboard.SetText(record.Text);
                            return true;
                        },
                        IcoPath = icoPath
                    });
                    contextMenus.Add(new Result
                    {
                        Title = "Delete",
                        Action = (context) =>
                        {
                            clipboardManager.ClearText(record.Text);
                            return true;
                        },
                        IcoPath = icoPath
                    });
                    break;
                case ImprovedClipboard.ResultType.Image:
                    contextMenus.Add(new Result
                    {
                        Title = "Copy",
                        Action = (ctx) =>
                        {
                            System.Windows.Forms.Clipboard.SetImage(record.Image);
                            return true;
                        },
                        IcoPath = icoPath
                    });
                    contextMenus.Add(new Result
                    {
                        Title = "Open Clipboard Images Folder",
                        Action = (ctx) =>
                        {
                            try
                            {
                                var process = Process.Start("explorer.exe", " /select,\"{path}\"".Replace("{path}",record.Path));
                                SetForegroundWindow(process.MainWindowHandle);
                                return true;
                            }
                            catch(Exception e)
                            {
                                context.API.ShowMsg("Error", e.Message, null);
                                return false;
                            }
                        },
                        IcoPath = icoPath
                    });
                    contextMenus.Add(new Result
                    {
                        Title = "Delete",
                        Action = (ctx) =>
                        {
                            clipboardManager.ClearImage(record.Path);
                            return true;
                        },
                        IcoPath = icoPath
                    });
                    break;

            }

            System.IO.DirectoryInfo di = new DirectoryInfo(clipboardManager.SaveDir);

            return contextMenus;
        }
        Setting _setting;
        public Control CreateSettingPanel()
        {
            return new SettingControl(_setting);
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            var keywords = query.Search.Trim().ToLower();
            var SearchData = clipboardManager.Search(keywords);
            if (_setting.EnableImgSearch && keywords == "img")
            {
                SearchData = clipboardManager.GetImgList();
                results.InsertRange(0, SearchData.Select(o => new Result
                {
                    Title = o.Name,
                    IcoPath = o.Path,
                    Action = c =>
                    {
                        try
                        {
                            System.Windows.Forms.Clipboard.SetImage(o.Image);
                            context.API.HideApp();
                            return true;
                        }
                        catch (Exception e)
                        {
                            context.API.ShowMsg("Error", e.Message, null);
                            return false;
                        }
                    },
                    ContextData = o,
                }));

            }
            else if (keywords == "clear")
            {

                results.Insert(0, new Result
                {
                    Title = "Clear Clipboard History",
                    IcoPath = icoPath,
                    Action = c =>
                    {
                        try
                        {
                            clipboardManager.ClearText();
                            return true;
                        }
                        catch (Exception e)
                        {
                            context.API.ShowMsg("Error", e.Message, null);
                            return false;
                        }
                    },
                    ContextData = new SearchResult { Type = ResultType.Clear },
                });
            }
            else if (_setting.EnableImgSearch && keywords == "img clear")
            {

                results.Insert(0, new Result
                {
                    Title = "Clear Clipboard Image History",
                    SubTitle = "this will also clear the image cache",
                    IcoPath = icoPath,
                    Action = c =>
                    {
                        try
                        {
                            clipboardManager.ClearImage();
                            return true;
                        }
                        catch (Exception e)
                        {
                            context.API.ShowMsg("Error", e.Message, null);
                            return false;
                        }
                    },
                    ContextData = new SearchResult { Type = ResultType.Clear },
                });
            }
            else
            {
                results.AddRange(SearchData.Select(o => new Result
                {
                    Title = o.Name,
                    SubTitle = o.Text,
                    IcoPath = icoPath,
                    Action = c =>
                    {
                        try
                        {
                            System.Windows.Forms.Clipboard.SetText(o.Text);
                            context.API.HideApp();
                            return true;
                        }
                        catch (Exception e)
                        {
                            context.API.ShowMsg("Error", e.Message, null);
                            return false;
                        }
                    },
                    ContextData = o,
                })) ;
            }


            if (results.Count > 20) results = results.Take(20).ToList();
            return results;
        }

        public void Init(PluginInitContext context)
        {
            this.context = context;
            
            _setting = Setting.Load(context);
            Loggers.Logger.UpdateLoggerPath(context.CurrentPluginMetadata.PluginDirectory);
            this.clipboardManager = new ImprovedClipboard.ClipboardManager(context.CurrentPluginMetadata.PluginDirectory,_setting);
            clipboardManager.Init();
        }
    }
}
