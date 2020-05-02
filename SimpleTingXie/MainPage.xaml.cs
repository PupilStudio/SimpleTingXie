using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace SimpleTingXie
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(300, 500);

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            //WordsInput.AcceptsReturn = false;
            //WordsInput.TextWrapping = TextWrapping.WrapWholeWords;
        }

        private void TalkButton_Click(object sender, RoutedEventArgs e)
        {
            TalkArgs args = new TalkArgs();
            args.Words = this.WordsInput.Text;
            args.SplitChar = ' ';

            if (!int.TryParse(this.Speed.Text, out args.Speed) || args.Speed < 1 || args.Speed > 15)
            {
                args.Speed = 4;
            }

            args.DisableBackward = ((CheckDisableGoback.IsChecked == null
                ? false 
                : (bool)(CheckDisableGoback.IsChecked)));
            args.DisableRepeat = ((CheckDisableReplay.IsChecked == null
                ? false
                : (bool)(CheckDisableReplay.IsChecked)));
            args.AutoNext = ((CheckEnableAutoNext.IsChecked == null
                ? false
                : (bool)(CheckEnableAutoNext.IsChecked)));
            args.RandomSort = ((CheckRandomSort.IsChecked == null
                ? false
                : (bool)(CheckRandomSort.IsChecked)));

            if (!int.TryParse(this.TextAutoNextTimes.Text, out args.AutoNextSeconds) || args.Speed < 1)
            {
                args.AutoNextSeconds = 20;
            }            

            Frame.Navigate(typeof(TalkPage), args);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            WordsInput.Text = "";
        }

        private void CheckEnableAutoNext_Checked(object sender, RoutedEventArgs e)
        {
            CheckDisableGoback.IsChecked = true;
            CheckDisableReplay.IsChecked = true;
        }
    }
}
