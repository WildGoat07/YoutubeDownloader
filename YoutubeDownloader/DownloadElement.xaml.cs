﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader
{
    /// <summary>
    /// Logique d'interaction pour DownloadElement.xaml
    /// </summary>
    public partial class DownloadElement : UserControl
    {
        public DownloadElement(string link, string path)
        {
            InitializeComponent();

            this.Link = link;
            this.FolderPath = path;
            CancelTokenSource = new CancellationTokenSource();

            label.Text = Link;
        }

        public string FolderPath { get; private set; }
        public string Link { get; }
        public string? VideoPath { get; private set; }
        private CancellationTokenSource CancelTokenSource { get; set; }

        public async Task StartDownloadAsync()
        {
            // Add back indetermination to progressbar
            progressbar.IsIndeterminate = true;
            redo.Visibility = Visibility.Hidden;
            progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#179c22");

            try
            {
                var video = await MainWindow.Youtube.Videos.GetAsync(Link);
                if (CancelTokenSource.IsCancellationRequested)
                    throw new OperationCanceledException();

                // Now that the video have loaded, we can display the video title
                VideoPath = System.IO.Path.Combine(FolderPath, Utils.RemoveInvalidChars(video.Title) + ".mp3");
                label.Text = video.Title;

                var streamManifest = await MainWindow.Youtube.Videos.Streams.GetManifestAsync(Link);
                var streamInfo = streamManifest.GetAudioOnly().WithHighestBitrate();
                if (CancelTokenSource.IsCancellationRequested)
                    throw new OperationCanceledException();

                if (streamInfo != null)
                {
                    if (File.Exists(VideoPath))
                    {
                        // ask the user if we should overwrite or abandon
                        MessageBoxResult result = MessageBox.Show("This file already exist, do you want overwrite it ?\n" + VideoPath, "File already exist", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        // Process message box results
                        switch (result)
                        {
                            case MessageBoxResult.Yes:
                                File.Delete(VideoPath);
                                break;

                            case MessageBoxResult.No:
                            default:
                                progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#b8200f");
                                redo.Visibility = Visibility.Visible;
                                return;
                        }
                    }

                    var progress = new Progress<double>(percent =>
                    {
                        progressbar.Value = Math.Round(percent * 100);
                    });

                    progressbar.IsIndeterminate = false;
                    progressbar.Value = 1;

                    await MainWindow.Youtube.Videos.Streams.DownloadAsync(streamInfo, VideoPath, progress, CancelTokenSource.Token);
                    if (CancelTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException();

                    progressbar.Value = 100;
                    progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#4e88d9");
                    redo.Visibility = Visibility.Hidden;
                    open.Visibility = Visibility.Visible;
                    openFolder.Visibility = Visibility.Visible;
                    CancelTokenSource.Cancel();
                }
            }
            catch (ArgumentException)
            {
                // Link is not a valid youtube link
                close.Visibility = Visibility.Visible;
                progressbar.IsIndeterminate = false;
                progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#b8200f");
                progressbar.Value = 100;
                CancelTokenSource.Cancel();
            }
            catch (OperationCanceledException)
            {
                // ConcellationToken event
                CancelTokenSource.Cancel();
                progressbar.IsIndeterminate = false;
                progressbar.Value = 100;
                progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#b8200f");

                if (VideoPath != null && File.Exists(VideoPath))
                {
                    File.Delete(VideoPath);
                }

                redo.Visibility = Visibility.Visible;
                close.Visibility = Visibility.Visible;
            }
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            if (!CancelTokenSource.IsCancellationRequested)
            {
                CancelTokenSource.Cancel();
            }
            else
            {
                try
                {
                    ((StackPanel)this.Parent).Children.Remove(this);
                }
                catch (Exception)
                {
                    close.Visibility = Visibility.Hidden;
                }
            }
        }

        private void open_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPath != null)
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo("explorer.exe", VideoPath)
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
        }

        private void openFolder_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPath != null)
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo("explorer.exe", $"/select, \"{VideoPath}\"")
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
        }

        private async void redo_Click(object sender, RoutedEventArgs e)
        {
            CancelTokenSource = new CancellationTokenSource();
            await StartDownloadAsync();
        }
    }
}