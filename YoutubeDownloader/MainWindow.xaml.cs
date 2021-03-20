﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Winforms = System.Windows.Forms;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<DownloadElement> downloads;

        public MainWindow()
        {
            InitializeComponent();

            downloads = new();

            LoadHistory();
            txtbx_folder.Text = Directory.GetCurrentDirectory();
        }

        private void OnTop_Checked(object sender, RoutedEventArgs e)
        {
            Topmost = true;
        }

        private void OnTop_Unchecked(object sender, RoutedEventArgs e)
        {
            Topmost = false;
        }

        private async void txtbx_input_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                // We check to see if it is a playlist, then we create a DownloadElement for each video of the playlist
                // Else we check if it is a video
                try
                {
                    var youtube = new YoutubeClient();
                    var playlist = await youtube.Playlists.GetAsync(txtbx_input.Text.Trim());

                    txtbx_input.Text = "";
                    await foreach (var video in youtube.Playlists.GetVideosAsync(playlist.Id))
                    {
                        var dl = new DownloadElement(video.Url);
                        downloads.Insert(0, dl);

                        LoadHistory();

                        _ = dl.StartDownloadAsync(txtbx_folder.Text);
                    }
                }
                catch (ArgumentException)
                {
                    var dl = new DownloadElement(txtbx_input.Text.Trim());

                    downloads.Insert(0, dl);
                    txtbx_input.Text = "";

                    LoadHistory();

                    await dl.StartDownloadAsync(txtbx_folder.Text);
                }
            }
        }

        private void btn_folderDialog_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new Winforms.FolderBrowserDialog();
            Winforms.DialogResult result = fbd.ShowDialog();

            if (result == Winforms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                txtbx_folder.Text = fbd.SelectedPath;
        }

        private void LoadHistory()
        {
            history.Children.Clear();

            foreach (DownloadElement dl in downloads)
                history.Children.Add(dl);
        }
    }
}