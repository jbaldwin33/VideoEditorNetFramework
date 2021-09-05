﻿using System;
using System.Threading.Tasks;
using System.Windows;
using CSVideoPlayer;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoEditorUi.Utilities;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public abstract class EditorViewModel : ViewModel
    {
        public enum StageEnum { Pre, Primary, Secondary }
        protected BaseClass VideoEditor;
        protected ProgressBarViewModel ProgressBarViewModel;
        public VideoPlayerWPF Player;

        public override void OnLoaded()
        {
            Initialize();
            base.OnLoaded();
        }

        public override void OnUnloaded()
        {
            UtilityClass.ClosePlayer(Player);
            base.OnUnloaded();
        }

        protected void SetupEditor()
        {
            VideoEditor.StartedDownload += StartedDownload;
            VideoEditor.ProgressDownload += ProgressDownload;
            VideoEditor.FinishedDownload += FinishedDownload;
            VideoEditor.ErrorDownload += ErrorDownload;
            VideoEditor.MessageHandler += LibraryMessageHandler;
        }

        protected void SetupProgressBarViewModel(int count)
        {
            ProgressBarViewModel = new ProgressBarViewModel(count);
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
            {
                try
                {
                    VideoEditor.CancelOperation(string.Empty);
                }
                catch (Exception ex)
                {
                    ShowMessage(new MessageBoxEventArgs(ex.Message, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
        }

        protected void Execute(bool doSetup, StageEnum stage, string label = "", int count = 1)
        {
            SetupEditor();
            SetupProgressBarViewModel(count);
            if (doSetup)
                VideoEditor.Setup();
            switch (stage)
            {
                case StageEnum.Pre: Task.Run(() => VideoEditor.PreWork()); break;
                case StageEnum.Primary: Task.Run(() => VideoEditor.DoWork(label)); break;
                case StageEnum.Secondary: Task.Run(() => VideoEditor.SecondaryWork()); break;
                default: throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
        }

        protected void StartedDownload(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        protected void ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressBarViewModel.ProgressBarCollection[e.ProcessIndex].ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage, e.ProcessIndex);
        }

        protected virtual void Initialize() => throw new NotImplementedException();

        protected virtual void FinishedDownload(object sender, FinishedEventArgs e)
        {
            CleanUp();
            if (Player != null)
                UtilityClass.ClosePlayer(Player);
        }

        protected virtual void ErrorDownload(object sender, ProgressEventArgs e)
        {
            CleanUp();
            ShowMessage(new MessageBoxEventArgs($"{new ErrorOccurredTranslatable()}\n\n{e.Error}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        protected virtual void LibraryMessageHandler(object sender, MessageEventArgs e)
        {
            var args = new MessageBoxEventArgs(e.Message, MessageBoxEventArgs.MessageTypeEnum.Question, MessageBoxButton.YesNo, MessageBoxImage.Question);
            ShowMessage(args);
            e.Result = args.Result == MessageBoxResult.Yes;
        }

        protected virtual void CleanUp() => Navigator.Instance.CloseChildWindow.Execute(false);
    }
}