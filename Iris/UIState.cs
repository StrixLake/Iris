using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Iris.UI
{
    // a singleton class to hold the ui state
    public partial class UIState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private UIState() { }
        static UIState()
        {
            state = new();
        }

        static UIState state;

        public static UIState StateInstance
        {
            get => state;
        }

        Tuple<string, string> displayFile = new Tuple<string, string>("", "");

        public Tuple<string, string> DisplayFile
        {
            get => displayFile;
            set => OnPropertyChanged(ref displayFile, value);
        }

        void OnPropertyChanged<T>(ref T field, T value, [CallerMemberName] string? propertyname = null)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }
    }
}