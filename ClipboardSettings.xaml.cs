using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using UserControl = System.Windows.Controls.UserControl;
using System;

namespace Wox.Plugin.ImprovedClipboard
{
    public partial class SettingControl : UserControl
    {
        Setting _setting;

        public SettingControl(Setting setting)
        {
            InitializeComponent();
            this._setting = setting;
            EnableImgSearch.IsChecked = _setting.EnableImgSearch;
            EnableImgSearch.Checked += (o, e) =>
            {
                _setting.EnableImgSearch = true;
                _setting.Save();
            };
            EnableImgSearch.Unchecked += (o, e) =>
            {
                _setting.EnableImgSearch = false;
                _setting.Save();

            };
            SearchType.Items.Add("SubString");
            SearchType.Items.Add("SubSequence");
            SearchType.Items.Add("Regex");
            SearchType.SelectedItem = setting.SearchType;
        }

        private void SearchType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _setting.SearchType = SearchType.SelectedItem.ToString();
            _setting.Save();
        }
    }
}