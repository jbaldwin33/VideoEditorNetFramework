﻿using System;
using System.Threading.Tasks;
using VideoUtilities;
using static VideoEditorUi.ViewModels.EditorViewModel;
using static VideoUtilities.BaseClass;

namespace VideoEditorUi.Services
{
    public interface IVideoEditorService
    {
        void SetEditor(BaseClass editor);
        void SetupEditor(StartedDownloadEventHandler startedDownload, ProgressEventHandler progressDownload, FinishedDownloadEventHandler finishedDownload, ErrorEventHandler errorDownload, MessageEventHandler libraryMessageHandler, UpdatePlaylistEventHandler updatePlaylist, PreWorkFinishedEventHandler preWorkFinished, FirstWorkFinishedEventHandler firstWorkFinished);
        void DoPreWork();
        void DoSetup();
        void ExecuteVideoEditor(StageEnum stage, string label);
        bool IsInitialized();
        void SetInitialized(bool initialized);
        void CancelOperation(string message);
    }

    public class VideoEditorService : IVideoEditorService
    {
        private static readonly Lazy<VideoEditorService> lazy = new Lazy<VideoEditorService>(() => new VideoEditorService());
        public static VideoEditorService Instance => lazy.Value;
        private BaseClass videoEditor;
        private bool editorInitialized;
        public bool IsInitialized() => editorInitialized;

        public void SetEditor(BaseClass editor)
        {
            videoEditor = editor;
        }

        public void SetupEditor(StartedDownloadEventHandler startedDownload, ProgressEventHandler progressDownload, FinishedDownloadEventHandler finishedDownload, ErrorEventHandler errorDownload, MessageEventHandler libraryMessageHandler, UpdatePlaylistEventHandler updatePlaylist, PreWorkFinishedEventHandler preWorkFinished, FirstWorkFinishedEventHandler firstWorkFinished)
        {
            if (editorInitialized)
                return;

            videoEditor.StartedDownload += startedDownload;
            videoEditor.ProgressDownload += progressDownload;
            videoEditor.FinishedDownload += finishedDownload;
            videoEditor.ErrorDownload += errorDownload;
            videoEditor.MessageHandler += libraryMessageHandler;
            videoEditor.UpdatePlaylist += updatePlaylist;
            videoEditor.PreWorkFinished += preWorkFinished;
            videoEditor.FirstWorkFinished += firstWorkFinished;
            editorInitialized = true;
        }
        public void DoSetup() => videoEditor.Setup();
        public void DoPreWork() => videoEditor.PreWork();
        public void ExecuteVideoEditor(StageEnum stage, string label)
        {
            switch (stage)
            {
                case StageEnum.Pre: Task.Run(() => videoEditor.PreWork()); break;
                case StageEnum.Primary: Task.Run(() => videoEditor.DoWork(label)); break;
                case StageEnum.Secondary: Task.Run(() => videoEditor.SecondaryWork()); break;
                default: throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
        }

        public void SetInitialized(bool initialized) => editorInitialized = initialized;

        public void CancelOperation(string message) => videoEditor.CancelOperation(message);
    }
}
