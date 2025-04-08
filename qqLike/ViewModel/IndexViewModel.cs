using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using qqLike.Functional;
using qqLike.Model;

namespace qqLike.ViewModel;

public class IndexViewModel : INotifyPropertyChanged
{
    private readonly Index view;
    private String content;

    public String Content
    {
        get => content;
        set
        {
            content = value;
            OnPropertyChanged(nameof(Content));
        }
    }

    public ICommand SendMessageCommand { get; }
    public ICommand SendFileCommand { get; }
    public ICommand UserInfoCardCommand { get; }
    public ICommand ChatUICommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public IndexViewModel(Index index)
    {
        SendMessageCommand = new IndexCommand(_ => SendMessage());
        UserInfoCardCommand = new IndexCommand(_ => ShowUserInfoCard());
        SendFileCommand = new IndexCommand(_ => SendFile());
        ChatUICommand = new IndexCommand(_ => ShowChatUI());
        view = index;
    }

    private void SendMessage()
    {
        ChatMessage message = new ChatMessage
        {
            Content = content,
            Type = MessageType.Common.ToString(),
            Time = DateTime.Now,
            Port = view.ContactPort,
        };
        byte[] buffer = Encoding.UTF8.GetBytes(JSON.Serialize(message));
        view.Client.Send(buffer);
        IPEndPoint endPoint = view.Client.LocalEndPoint as IPEndPoint;
        view.chatContent.AppendText($"{endPoint.Port} {message.Time.ToString("yyyy-MM-dd HH:mm:ss")}:{Content}\r\n");
        Content = "";
    }

    private void SendFile()
    {
        OpenFileDialog dialog = new OpenFileDialog();
        dialog.Filter = "All files (*.*)|*.*";
        dialog.Multiselect = false;
        dialog.Title = "选择文件";
        bool? result = dialog.ShowDialog();
        if (result.HasValue && result.Value)
        {
            List<byte> res = new List<byte>();
            using (Stream stream = dialog.OpenFile())
            {
                byte[] buffer = new byte[8192];
                while (stream.Position < stream.Length)
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    for (int i = 0; i < bytes; i++)
                        res.Add(buffer[i]);
                }
            }

            ChatMessage message = new ChatMessage
            {
                Time = DateTime.Now,
                Port = view.ContactPort,
                Content = res.ToArray(),
                Type = MessageType.File.ToString()
            };
            byte[] data = Encoding.UTF8.GetBytes(JSON.Serialize(message));
            view.Client.Send(data);
            Content = "文件";
            IPEndPoint endPoint = view.Client.LocalEndPoint as IPEndPoint;
            view.chatContent.AppendText(
                $"{endPoint.Port} {message.Time.ToString("yyyy-MM-dd HH:mm:ss")}:{Content}\r\n");
            Content = "";
        }
    }


    private void ShowUserInfoCard()
    {
        view.userInfoCard.Visibility = Visibility.Visible;
        view.chatUI.Visibility = Visibility.Collapsed;
    }

    private void ShowChatUI()
    {
        view.userInfoCard.Visibility = Visibility.Collapsed;
        view.chatUI.Visibility = Visibility.Visible;
    }


    protected virtual void OnPropertyChanged(String propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class IndexCommand : ICommand
{
    public Action<object?>? ExecuteFunc { get; set; }
    public Func<object?, bool>? CanExecuteFunc { get; set; }

    public IndexCommand(Action<object?>? executeFunc)
    {
        ExecuteFunc = executeFunc;
        CanExecuteFunc = null;
    }

    public bool CanExecute(object? parameter)
    {
        if (CanExecuteFunc == null) return true;
        return CanExecuteFunc(parameter);
    }

    public void Execute(object? parameter)
    {
        ExecuteFunc?.Invoke(parameter);
    }

    public event EventHandler? CanExecuteChanged;
}