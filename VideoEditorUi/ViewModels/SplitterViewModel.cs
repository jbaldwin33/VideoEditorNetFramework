﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoUtilities;
using static VideoUtilities.Enums;
using MVVMFramework.Localization;
using System.Windows.Input;

namespace VideoEditorUi.ViewModels
{
    public class SplitterViewModel : EditorViewModel
    {
        #region Fields and props

        private RelayCommand seekBackCommand;
        private RelayCommand playCommand;
        private RelayCommand seekForwardCommand;
        private RelayCommand startCommand;
        private RelayCommand endCommand;
        private RelayCommand splitCommand;
        private RelayCommand selectFileCommand;
        private RelayCommand rectCommand;
        private RelayCommand jumpToTimeCommand;
        private TimeSpan startTime;
        private TimeSpan endTime;
        private string startTimeString;
        private string currentTimeString;
        private bool startTimeSet;
        private bool reEncodeVideo;
        private string inputPath;
        private ObservableCollection<SectionViewModel> sectionViewModels;
        private ObservableCollection<RectClass> rectCollection;
        private List<FormatTypeViewModel> formats;
        private FormatEnum formatType;
        private bool canCombine;
        private bool combineVideo;
        private bool outputDifferentFormat;
        private string textInput;
        private bool timesImported;

        public TimeSpan StartTime
        {
            get => startTime;
            set
            {
                SetProperty(ref startTime, value);
                StartTimeString = StartTime.ToString("hh':'mm':'ss':'fff");
            }
        }

        public TimeSpan EndTime
        {
            get => endTime;
            set => SetProperty(ref endTime, value);
        }

        public string StartTimeString
        {
            get => startTimeString;
            set => SetProperty(ref startTimeString, value);
        }

        public string CurrentTimeString
        {
            get => currentTimeString;
            set => SetProperty(ref currentTimeString, value);
        }

        public bool StartTimeSet
        {
            get => startTimeSet;
            set => SetProperty(ref startTimeSet, value);
        }

        public bool ReEncodeVideo
        {
            get => reEncodeVideo;
            set => SetProperty(ref reEncodeVideo, value);
        }

        public string InputPath
        {
            get => inputPath;
            set => SetProperty(ref inputPath, value);
        }

        public ObservableCollection<SectionViewModel> SectionViewModels
        {
            get => sectionViewModels;
            set => SetProperty(ref sectionViewModels, value);
        }


        public ObservableCollection<RectClass> RectCollection
        {
            get => rectCollection;
            set => SetProperty(ref rectCollection, value);
        }

        public FormatEnum FormatType
        {
            get => formatType;
            set => SetProperty(ref formatType, value);
        }

        public List<FormatTypeViewModel> Formats
        {
            get => formats;
            set => SetProperty(ref formats, value);
        }

        public bool CanCombine
        {
            get => canCombine;
            set => SetProperty(ref canCombine, value);
        }

        public bool CombineVideo
        {
            get => combineVideo;
            set => SetProperty(ref combineVideo, value);
        }

        public bool OutputDifferentFormat
        {
            get => outputDifferentFormat;
            set
            {
                SetProperty(ref outputDifferentFormat, value);
                ReEncodeVideo = value;
            }
        }

        public string TextInput
        {
            get => textInput;
            set => SetProperty(ref textInput, value);
        }

        public bool TimesImported
        {
            get => timesImported;
            set => SetProperty(ref timesImported, value);
        }

        #endregion

        #region  Labels

        public string StartLabel => new StartTimeLabelTranslatable();
        public string EndLabel => new EndTimeLabelTranslatable();
        public string SplitLabel => new SplitLabelTranslatable();
        public string CombineSectionsLabel => new CombineSectionsQuestionTranslatable();
        public string OutputFormatLabel => new OutputFormatQuestionTranslatable();
        public string ReEncodeQuestionLabel => new ReEncodeQuestionTranslatable();
        public string ReEncodeComment => new ReEncodeCommentTranslatable();
        public string ConfirmLabel => new ConfirmTranslatable();
        public string JumpToTimeLabel => new JumpToTimeLabelTranslatable();
        public string TagText => new EnterTitleTranslatable();
        public string DragFileLabel => new DragFileTranslatable();

        #endregion

        #region Commands

        public RelayCommand SeekBackCommand => seekBackCommand ?? (seekBackCommand = new RelayCommand(SeekBackCommandExecute, () => FileLoaded));
        public RelayCommand SeekForwardCommand => seekForwardCommand ?? (seekForwardCommand = new RelayCommand(SeekForwardCommandExecute, () => FileLoaded));
        public RelayCommand PlayCommand => playCommand ?? (playCommand = new RelayCommand(PlayCommandExecute, () => FileLoaded));
        public RelayCommand StartCommand => startCommand ?? (startCommand = new RelayCommand(StartCommandExecute, () => !StartTimeSet && FileLoaded && !TimesImported));
        public RelayCommand EndCommand => endCommand ?? (endCommand = new RelayCommand(EndCommandExecute, () => StartTimeSet && !TimesImported));
        public RelayCommand SplitCommand => splitCommand ?? (splitCommand = new RelayCommand(SplitCommandExecute, () => SectionViewModels?.Count > 0));
        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand RectCommand => rectCommand ?? (rectCommand = new RelayCommand(RectCommandExecute, () => true));
        public RelayCommand JumpToTimeCommand => jumpToTimeCommand ?? (jumpToTimeCommand = new RelayCommand(JumpToTimeCommandExecute, () => FileLoaded));


        #endregion

        private static readonly object _lock = new object();
        public Action AddRectangleEvent;

        public override void OnUnloaded()
        {
            FileLoaded = false;
            TimesImported = false;
            SectionViewModels.CollectionChanged -= Times_CollectionChanged;
            base.OnUnloaded();
        }

        public override void Initialize()
        {
            CanCombine = false;
            StartTimeSet = false;
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
            CurrentTimeString = "00:00:00:000";
            PositionChanged = time => CurrentTimeString = time.ToString("hh':'mm':'ss':'fff");
            SectionViewModels = new ObservableCollection<SectionViewModel>();
            RectCollection = new ObservableCollection<RectClass>();
            Formats = FormatTypeViewModel.CreateViewModels();
            FormatType = FormatEnum.avi;
            SectionViewModels.CollectionChanged += Times_CollectionChanged;

            BindingOperations.EnableCollectionSynchronization(RectCollection, _lock);
            BindingOperations.EnableCollectionSynchronization(SectionViewModels, _lock);
        }

        protected override void DragFilesCallback(string[] files)
        {
            InputPath = files[0];
            GetDetailsEvent?.Invoke(files[0]);
            OpenEvent?.Invoke(files[0]);
            FileLoaded = true;
            CommandManager.InvalidateRequerySuggested();
            ResetAll();
        }

        private void Times_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => CanCombine = SectionViewModels.Count > 1;

        private void SeekBackCommandExecute()
        {
            SeekEvent?.Invoke(-5000);
            CurrentTimeString = new TimeSpan(0, 0, 0, 0, (int)SliderValue).ToString("hh':'mm':'ss':'fff");
        }

        private void SeekForwardCommandExecute()
        {
            SeekEvent?.Invoke(5000);
            CurrentTimeString = new TimeSpan(0, 0, 0, 0, (int)SliderValue).ToString("hh':'mm':'ss':'fff");
        }

        private void JumpToTimeCommandExecute()
        {
            TimeSpan.TryParseExact(CurrentTimeString, "hh':'mm':'ss':'fff", CultureInfo.CurrentCulture, out var result);
            SeekEvent?.Invoke(result.TotalMilliseconds);
        }

        private void SplitCommandExecute()
        {
            var splitterArgs = new SplitterArgs(SectionViewModels.ToList(), InputPath, CombineVideo, OutputDifferentFormat, $".{FormatType}", ReEncodeVideo);
            Setup(true, false, splitterArgs, null, Splitter_SplitFinished, SectionViewModels.Count);
            Execute(StageEnum.Primary, new SplittingLabelTranslatable());
        }

        private void StartCommandExecute()
        {
            StartTimeSet = true;
            StartTime = GetPlayerPosition();
        }

        private void EndCommandExecute()
        {
            if (GetPlayerPosition() <= StartTime)
            {
                StartTimeSet = false;
                StartTime = TimeSpan.FromMilliseconds(0);
                ShowMessage(new MessageBoxEventArgs(new EndTimeAfterStartTimeTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }
            EndTime = GetPlayerPosition();
            AddRectangleEvent?.Invoke();
            SectionViewModels.Add(new SectionViewModel(StartTime, EndTime, TextInput));
            TextInput = string.Empty;
            StartTimeSet = false;
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
        }

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
            GetDetailsEvent(openFileDialog.FileName);
            OpenEvent(openFileDialog.FileName);
            FileLoaded = true;
            ResetAll();
        }

        private void RectCommandExecute(object obj)
        {
            var rect = obj as RectClass;
            var args = new MessageBoxEventArgs(new DeleteSectionConfirmTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Question, MessageBoxButton.YesNo, MessageBoxImage.Question);
            ShowMessage(args);
            if (args.Result != MessageBoxResult.Yes)
                return;

            var index = RectCollection.IndexOf(rect);
            RectCollection.Remove(rect);
            SectionViewModels.RemoveAt(index);
        }

        private void ResetAll()
        {
            StartTimeSet = false;
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
            CurrentTimeString = "00:00:00:000";
            TimesImported = false;
            RectCollection.Clear();
            SectionViewModels.Clear();
        }

        protected override void FinishedDownload(object sender, FinishedEventArgs e)
        {
            base.FinishedDownload(sender, e);
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()} {e.Message}"
                : new VideoSuccessfullySplitTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        protected override void ErrorDownload(object sender, ProgressEventArgs e)
        {
            base.ErrorDownload(sender, e);
            ShowMessage(new MessageBoxEventArgs($"{new ErrorOccurredTranslatable()}\n\n{e.Error}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void Splitter_SplitFinished(object sender, EventArgs e)
        {
            UtilityClass.CloseChildWindow(false);
            Setup(false, false, null, null, null);
            Execute(StageEnum.Secondary, null);
        }

        public override void CleanUp(bool isError)
        {
            if (!isError)
            {
                CombineVideo = false;
                OutputDifferentFormat = false;
                ReEncodeVideo = false;
                FileLoaded = false;
                FormatType = FormatEnum.avi;
                ResetAll();
            }
            base.CleanUp(isError);
        }
    }

    public class RectClass
    {
        public Thickness Margin { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public SolidColorBrush Fill { get; set; }
        public RelayCommand RectCommand { get; set; }
    }
}
