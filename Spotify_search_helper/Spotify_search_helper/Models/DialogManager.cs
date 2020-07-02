﻿using Windows.UI.Xaml.Controls;

namespace Spotify_search_helper.Models
{
    public class DialogManager
    {
        public DialogManager() { }

        public DialogManager(DialogType type, DialogAction action)
        {
            Type = type;
            Action = action;
        }

        public DialogManager(string title, string message, string primaryButtonText, DialogType type, DialogAction action)
        {
            Title = title;
            Message = message;
            PrimaryButtonText = primaryButtonText;
            Type = type;
            Action = action;
        }

        public string Title { get; set; }
        public string Message { get; set; }
        public string PrimaryButtonText { get; set; }
        public string SecondaryButtonText { get; set; }
        public DialogAction Action { get; set; }
        public DialogType Type { get; set; }
    }

    public class DialogResult
    {
        public DialogResult(DialogType type, ContentDialogResult resultType)
        {
            Type = type;
            ResultType = resultType;
        }

        public DialogType Type { get; set; }
        public ContentDialogResult ResultType { get; set; }
    }

    public enum DialogAction
    {
        Show,
        Hide
    }

    public enum DialogType
    {
        Merge,
        CreatePlaylist,
        Default,
        Unfollow,
        AddToPlaylist
    }
}
