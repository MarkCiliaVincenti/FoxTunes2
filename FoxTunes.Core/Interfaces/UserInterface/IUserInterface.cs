﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IUserInterface : IStandardComponent
    {
        IEnumerable<IUserInterfaceWindow> Windows { get; }

        Task Show();

        void Activate();

        void Warn(string message);

        void Fatal(Exception exception);

        bool Confirm(string message);

        string Prompt(string message, UserInterfacePromptFlags flags = UserInterfacePromptFlags.None);

        void SelectInShell(string fileName);

        void OpenInShell(string fileName);

        Task ShowSettings(string title, IEnumerable<string> sections);

        void Restart();

        event UserInterfaceWindowEventHandler WindowCreated;

        event UserInterfaceWindowEventHandler WindowDestroyed;
    }

    public interface IUserInterfaceWindow
    {
        string Id { get; }

        IntPtr Handle { get; }

        UserInterfaceWindowRole Role { get; }
    }

    public enum UserInterfaceWindowRole : byte
    {
        None,
        Main
    }

    public delegate void UserInterfaceWindowEventHandler(object sender, UserInterfaceWindowEventArgs e);

    public class UserInterfaceWindowEventArgs : EventArgs
    {
        public UserInterfaceWindowEventArgs(IUserInterfaceWindow window)
        {
            this.Window = window;
        }

        public IUserInterfaceWindow Window { get; private set; }
    }

    [Flags]
    public enum UserInterfacePromptFlags : byte
    {
        None = 0,
        Password = 1
    }
}
