﻿using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using MVVMFramework;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoEditorUi.Utilities;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class ReverseViewModel : ViewModel
    {
        private CSVideoPlayer.VideoPlayerWPF player;
        private string inputPath;
        private bool fileLoaded;
        private RelayCommand selectFileCommand;
        private RelayCommand reverseCommand;
        private ProgressBarViewModel progressBarViewModel;
        private VideoReverser reverser;

        public CSVideoPlayer.VideoPlayerWPF Player
        {
            get => player;
            set => SetProperty(ref player, value);
        }
        
        public string InputPath
        {
            get => inputPath;
            set => SetProperty(ref inputPath, value);
        }

        public bool FileLoaded
        {
            get => fileLoaded;
            set => SetProperty(ref fileLoaded, value);
        }

        public ProgressBarViewModel ProgressBarViewModel
        {
            get => progressBarViewModel;
            set => SetProperty(ref progressBarViewModel, value);
        }

        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand ReverseCommand => reverseCommand ?? (reverseCommand = new RelayCommand(ReverseCommandExecute, ReverseCommandCanExecute));

        public string ReverseLabel => Translatables.ReverseLabel;
        public string SelectFileLabel => Translatables.SelectFileLabel;
        public string NoFileLabel => Translatables.NoFileSelected;

        public ReverseViewModel()
        {
            
        }

        public override void OnUnloaded()
        {
            UtilityClass.ClosePlayer(player);
            FileLoaded = false;
            base.OnUnloaded();
        }

        private bool ReverseCommandCanExecute() => FileLoaded;

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFileDialog.ShowDialog() == false)
                return;

            InputPath = openFileDialog.FileName;
            player.Open(new Uri(openFileDialog.FileName));
            FileLoaded = true;
        }

        private void ReverseCommandExecute()
        {
            var messageArgs = new MessageBoxEventArgs(Translatables.ReverseVideoMessage, MessageBoxEventArgs.MessageTypeEnum.Warning, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            ShowMessage(messageArgs);
            if (messageArgs.Result == MessageBoxResult.No)
                return;
            reverser = new VideoReverser(InputPath);
            reverser.StartedDownload += Reverser_DownloadStarted;
            reverser.ProgressDownload += Reverser_ProgressDownload;
            reverser.FinishedDownload += Reverser_FinishedDownload;
            reverser.ErrorDownload += Reverser_ErrorDownload;
            reverser.MessageHandler += Reverser_MessageHandler;
            ProgressBarViewModel = new ProgressBarViewModel();
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
            {
                try
                {
                    reverser.CancelOperation(string.Empty);
                }
                catch (Exception ex)
                {
                    ShowMessage(new MessageBoxEventArgs(ex.Message, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
            Task.Run(() => reverser.ReverseVideo());
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
        }

        private void Reverser_DownloadStarted(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        private void Reverser_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressBarViewModel.ProgressBarCollection[e.ProcessIndex].ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage);
        }

        private void Reverser_FinishedDownload(object sender, FinishedEventArgs e)
        {
            CleanUp();
            var message = e.Cancelled
                ? $"{Translatables.OperationCancelled} {e.Message}"
                : Translatables.VideoSuccessfullyReversed;
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void Reverser_ErrorDownload(object sender, ProgressEventArgs e)
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            ShowMessage(new MessageBoxEventArgs($"{Translatables.ErrorOccurred}\n\n{e.Error}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void Reverser_MessageHandler(object sender, MessageEventArgs e)
        {
            var args = new MessageBoxEventArgs(e.Message, MessageBoxEventArgs.MessageTypeEnum.Warning, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            ShowMessage(args);
            e.Result = args.Result == MessageBoxResult.Yes;
        }

        private void CleanUp()
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            InputPath = string.Empty;
            FileLoaded = false;
        }
    }
}