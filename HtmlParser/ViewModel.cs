using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;

namespace HtmlParser
{
    public class ViewModel:ObservableObject
    {
        private bool _isCalculating;
        private CancellationTokenSource _tokenSource;
        private readonly List<Model> _errorModelsList;

        public AsyncRelayCommand OpenCommand { get;}
        public RelayCommand ClearCommand { get; }
        public RelayCommand ResetCommand { get; }
        public AsyncRelayCommand CalculateCommand { get; }
        public ObservableCollection<Model> UrlCollection { get; private set; }
        public Model MaxElement { get; private set; }
        public int CurrentCount { get; private set; }
        public string CalculateButtonContent => _isCalculating ? "Stop" : "Calculate";

        public ViewModel()
        {
            _errorModelsList = new List<Model>();

            OpenCommand = new AsyncRelayCommand(OpenMethodAsync, ()=>!_isCalculating);
            ClearCommand = new RelayCommand(ClearMethod, () => CanExecute() && !_isCalculating);
            ResetCommand = new RelayCommand(ResetMethod, () => CanExecute() && !_isCalculating);
            CalculateCommand = new AsyncRelayCommand(CalculateMethod, CanExecute);
        }

        private bool CanExecute() => UrlCollection is { Count: > 0 };

        private void ResetMethod()
        {
            CurrentCount = 0;
            foreach (var item in UrlCollection)
                item.ResetData();
            NotifyCanExecuteChangedCommands();
        }

        private async Task OpenMethodAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Environment.CurrentDirectory,
                Filter = "Txt files (*.txt)|*.txt",
                Title = "Open file",
                RestoreDirectory = true
            };
            if (openFileDialog.ShowDialog() == true)
            {
                CurrentCount = 0;
                var lines = await File.ReadAllLinesAsync(openFileDialog.FileName);
                GetUrlCollection(lines);
                NotifyCanExecuteChangedCommands();
            }
        }

        private void GetUrlCollection(IEnumerable<string> lines)
        {
            UrlCollection = new ObservableCollection<Model>();
            foreach (var item in lines)
                if (Uri.TryCreate(item, UriKind.Absolute, out var uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    UrlCollection.Add(new Model(uriResult));
        }

        private void ClearMethod()
        {
            UrlCollection.Clear();
            CalculateCommand.NotifyCanExecuteChanged();
            ClearCommand.NotifyCanExecuteChanged();
            ResetCommand.NotifyCanExecuteChanged();
            CurrentCount = 0;
        }

        private void NotifyCanExecuteChangedCommands()
        {
            OpenCommand.NotifyCanExecuteChanged();
            ClearCommand.NotifyCanExecuteChanged();
            ResetCommand.NotifyCanExecuteChanged();
            CalculateCommand.NotifyCanExecuteChanged();
        }

        private void NotifyCanExecuteChangedCommandsButton()
        {
            NotifyCanExecuteChangedCommands();
            OnPropertyChanged(nameof(CalculateButtonContent));
        }

        private async Task CalculateMethod()
        {
            if (!_isCalculating)
            {
                _isCalculating = true;
                NotifyCanExecuteChangedCommandsButton();
                _tokenSource = new CancellationTokenSource();
                try
                {
                    await Task.Run(CalculateTags, _tokenSource.Token);
                }
                catch (OperationCanceledException)
                {

                }
                CheckErrors();
            }
            else
                _tokenSource.Cancel();

            MaxElement = UrlCollection.MaxBy(item => item.Count)!;
            _isCalculating = false;
            NotifyCanExecuteChangedCommandsButton();
        }

        private void CalculateTags()
        {
            Parallel.ForEach(UrlCollection, (item, loopState) =>
            {
                using var client = new WebClient();
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    _tokenSource.Token.ThrowIfCancellationRequested();
                    loopState.Break();
                }

                if (item.IsCalculated) return;
                string htmlCode;
                try
                {
                    htmlCode = client.DownloadString(item.Address);
                }
                catch (WebException)
                {
                    _errorModelsList.Add(item);
                    return;
                }
                CurrentCount++;
                item.SetData(htmlCode);
            });
        }

        private void CheckErrors()
        {
            if (_errorModelsList.Count <= 0) return;
            var output = string.Empty;
            for (var i = 0; i < _errorModelsList.Count - 1; i++)
                output += $"{_errorModelsList[i].Address} ,\r\n";
            output += $"{_errorModelsList.Last().Address} .";
            MessageBox.Show($"Please, check your internet connection or input data:\r\n\r\n{output}",
                "Program message");
            _errorModelsList.Clear();
        }
    }

}
