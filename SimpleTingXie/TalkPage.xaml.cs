﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Net.WebSockets;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Net.NetworkInformation;
using Windows.UI.Popups;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Windows.Storage;
using Windows.System.Threading;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Activation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace SimpleTingXie
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class TalkPage : Page
    {
        List<string> voices = null;
        ThreadPoolTimer currentTimer = null;
        string wordsRefer = "Oops!程序出现了亿些问题";
        string[] vs;
        int voicesIndex = 0;
        bool autoNext;
        bool disableRepeat, disableGoback;
        int secs;
        bool randSort;
        TalkArgs args;

        string RemoveSound(string word)
        {
            StringBuilder builder = new StringBuilder();
            return "";
        }
        void Delay(long millSecond)
        {
            long end = System.DateTime.UtcNow.Millisecond + millSecond;
            while (System.DateTime.UtcNow.Millisecond < end) ;
        }

        
        async Task<bool> ProcessTTSAPI(TalkArgs args)
        {
            wordsRefer = args.Words;
            vs = args.Words.Split(' ');

            if (randSort)
            {
                Random r = new Random();
                int rndCnt = r.Next(10, 101);
                for (int k = 0; k != rndCnt; ++k)
                {
                    int a = r.Next(0, vs.Length),
                        b = r.Next(0, vs.Length);
                    string t = vs[a];
                    vs[a] = vs[b];
                    vs[b] = t;
                }
            }

            wordsRefer = string.Join(" ", vs);


            //Baidu.Aip.Speech.TtsResponse[] resps = new Baidu.Aip.Speech.TtsResponse[0];
            voices = new List<string>();
            voicesIndex = 0;

            string tokenJson = await new HttpClient()
                .GetAsync("https://openapi.baidu.com/oauth/2.0/token?grant_type=client_credentials&client_id=y28tMmG81RCmLFkAfrhM81ql&client_secret=f3nrSGaQSmCHpPql3Deb16pDNaYPWhyv")
                .Result.Content.ReadAsStringAsync();
            var tokenRespJson = JObject.Parse(tokenJson);
            string token = tokenRespJson["access_token"].ToString();
            //BlockWords.Text = token;

            if (Directory.Exists(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Voices")))
            {
                Directory.Delete(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Voices"), true);
            }
            Directory.CreateDirectory(
                Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Voices"));

            int cnt = 0;
            foreach (var i in vs)
            {
                ++cnt;
                BlockWords.Text = "正在生成第" + cnt.ToString() + "个词语...";
                if (i == "")
                    continue;
                var resp = await
                    new HttpClient()
                    .GetAsync("https://tsn.baidu.com/text2audio?lan=zh&ctp=1&cuid=DeepDarkFantasy&tok=" 
                    + token 
                    + "&tex=" + Uri.EscapeDataString(Uri.EscapeDataString(i))
                    + "&aue=6"
                    + "&spd=" + args.Speed.ToString());
                
                Debug.WriteLine("https://tsn.baidu.com/text2audio?lan=zh&ctp=1&cuid=DeepDarkFantasy&tok="
                    + token
                    + "&tex=" + Uri.EscapeDataString(Uri.EscapeDataString(i))
                    + "&aue=6"
                    + "&spd=" + args.Speed.ToString());

                if (resp.Content.Headers.ContentType.MediaType != "audio/wav")
                {
                    //Debug.Assert(false);
                    BlockWords.Text = "Oops!这个软件出现了亿些问题......";
                    Debug.WriteLine(await resp.Content.ReadAsStringAsync());
                    return false;
                }
                string wordId = System.Guid.NewGuid().ToString();
                File.WriteAllBytes(
                    Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Voices", wordId + ".wav"),
                    await resp.Content.ReadAsByteArrayAsync());
                voices.Add(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Voices", wordId + ".wav"));

                new System.Threading.ManualResetEvent(false).WaitOne(100);
            }
            return true;
        }

        public TalkPage()
        {
            
            this.InitializeComponent();
            KeyboardAccelerator GoBack = new KeyboardAccelerator();
            GoBack.Key = VirtualKey.GoBack;
            GoBack.Invoked += BackInvoked;
            KeyboardAccelerator AltLeft = new KeyboardAccelerator();
            AltLeft.Key = VirtualKey.Left;
            AltLeft.Invoked += BackInvoked;
            this.KeyboardAccelerators.Add(GoBack);
            this.KeyboardAccelerators.Add(AltLeft);
            // ALT routes here
            AltLeft.Modifiers = VirtualKeyModifiers.Menu;
        }

        void InitUI()
        {
            BlockWords.Text = "第" + (voicesIndex + 1).ToString() + 
                "个词语, 共" + voices.Count.ToString() + "个词语";
            if (voicesIndex == 0)
            {
                ButtonPrev.IsEnabled = false;
                ButtonNext.IsEnabled = true;
                ButtonRepeat.Visibility = ButtonStop.Visibility =
                ButtonPrev.Visibility = ButtonNext.Visibility = BlockWords.Visibility = Visibility.Visible;
                ButtonNext.Content = "下一个词语";
            } else if (voicesIndex == voices.Count - 1)
            {
                ButtonPrev.IsEnabled = true;
                ButtonNext.IsEnabled = true;
                ButtonNext.Content = "完成";
                ButtonRepeat.Visibility = ButtonStop.Visibility =
                ButtonPrev.Visibility = ButtonNext.Visibility = BlockWords.Visibility = Visibility.Visible;
                BlockWords.Text = "最后一个词语, 共" + voices.Count.ToString() + "个词语";
            } else if (voicesIndex == voices.Count)
            {
                ButtonRepeat.Visibility = ButtonStop.Visibility =
                ButtonPrev.Visibility = ButtonNext.Visibility = BlockWords.Visibility = Visibility.Collapsed;
                BlockKeys.Visibility = BlockCurWordsTip.Visibility = Visibility.Visible;
                BlockKeys.Text = wordsRefer;

                //foreach (var i in vs)
                //{
                //    CheckBox wrongBox = new CheckBox()
                //    {
                //        Content = i,                        
                //    };
                //    StackMain.Children.Add(wrongBox);
                //}
            }
            else
            {
                ButtonPrev.IsEnabled = ButtonNext.IsEnabled = true;
                ButtonNext.Content = "下一个词语";
                ButtonRepeat.Visibility = ButtonStop.Visibility =
                ButtonPrev.Visibility = ButtonNext.Visibility = BlockWords.Visibility = Visibility.Visible;
            }
            if (disableRepeat)
                ButtonRepeat.IsEnabled = false;
            if (disableGoback)
                ButtonPrev.IsEnabled = false;
        }
        void PlayCurrent()
        {
            if (voicesIndex == voices.Count)
                return;
            MediaElem.Source = new Uri(voices[voicesIndex]);
            MediaElem.Volume = 1;
            MediaElem.Stop();
            MediaElem.Play();
        }

        void InitAutoNextTimers()
        {
            if (!autoNext)
                return;
            TimeSpan ts = TimeSpan.FromSeconds(secs);
            if (currentTimer != null)
                currentTimer.Cancel();
            currentTimer = ThreadPoolTimer.CreateTimer(
                async (source) => {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
                    () =>
                    {
                        ButtonNext_Click(null, null);
                    });
                }, ts);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            currentTimer = null;
            voices = null;
            wordsRefer = "Oops!程序出现了亿些问题";
            voicesIndex = 0;

            StackWrongs.Children.Clear();
            ButtonOK.Visibility = Visibility.Collapsed;
            BackButton.IsEnabled = this.Frame.CanGoBack;
            BlockWords.Text = "正在初始化......";
            ButtonStop.IsEnabled =
            ButtonNext.IsEnabled = ButtonPrev.IsEnabled = ButtonRepeat.IsEnabled = false;
            randSort = ((TalkArgs)e.Parameter).RandomSort;
            await ProcessTTSAPI((TalkArgs)e.Parameter);
            BlockWords.Text = "初始化完成!你准备好了吗?";
            ButtonOK.Visibility = Visibility.Visible;

            args = ((TalkArgs)e.Parameter);            
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            On_BackRequested();
        }

        // Handles system-level BackRequested events and page-level back button Click events
        private bool On_BackRequested()
        {
            if (this.Frame.CanGoBack)
            {
                if (currentTimer != null)
                    currentTimer.Cancel();
                this.Frame.GoBack();
                return true;
            }
            return false;
        }

        private void BackInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            On_BackRequested();
            args.Handled = true;
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            ++voicesIndex;
            InitAutoNextTimers();
            InitUI();
            PlayCurrent();
        }

        private void ButtonPrev_Click(object sender, RoutedEventArgs e)
        {
            --voicesIndex;
            InitAutoNextTimers();
            InitUI();
            PlayCurrent();
        }

        private void ButtonRepeat_Click(object sender, RoutedEventArgs e)
        {
            PlayCurrent();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            ButtonOK.Visibility = Visibility.Collapsed;
            ButtonStop.IsEnabled =
            ButtonNext.IsEnabled = ButtonPrev.IsEnabled = ButtonRepeat.IsEnabled = true;

            disableRepeat = args.DisableRepeat;
            disableGoback = args.DisableBackward;
            autoNext = args.AutoNext;
            secs = args.AutoNextSeconds;

            InitAutoNextTimers();
            InitUI();
            PlayCurrent();
        }

        private void StopPlay_Click(object sender, RoutedEventArgs e)
        {
            MediaElem.Stop();
        }
    }
}
