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
        public AsyncRelayCommand CalculateCommand { get; }
        public ObservableCollection<Model> UrlCollection { get; private set; }
        public Model MaxElement { get; private set; }
        public string CalculateButtonContent => _isCalculating ? "Stop" : "Calculate";

        public ViewModel()
        {
            _errorModelsList = new List<Model>();

            OpenCommand = new AsyncRelayCommand(OpenMethodAsync, ()=>!_isCalculating);
            ClearCommand = new RelayCommand(ClearMethod, () => CanExecute() && !_isCalculating);
            CalculateCommand = new AsyncRelayCommand(CalculateMethod, CanExecute);
        }

        private bool CanExecute() => UrlCollection is { Count: > 0 };

        private async Task OpenMethodAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Environment.CurrentDirectory,
                Filter = "Txt files (*.txt)|*.txt",
                Title = "Open file"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var lines = await File.ReadAllLinesAsync(openFileDialog.FileName);
                GetUrlCollection(lines);
                CalculateCommand.NotifyCanExecuteChanged();
                ClearCommand.NotifyCanExecuteChanged();
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
        }

        private void NotifyCanExecuteChangedCommands()
        {
            OpenCommand.NotifyCanExecuteChanged();
            ClearCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CalculateButtonContent));
        }

        private async Task CalculateMethod()
        {
            if (!_isCalculating)
            {
                _isCalculating = true;
                NotifyCanExecuteChangedCommands();
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
            NotifyCanExecuteChangedCommands();
        }

        private void CalculateTags()
        {
            using var client = new WebClient();
            foreach (var item in UrlCollection)
            {
                if (_tokenSource.Token.IsCancellationRequested)
                    _tokenSource.Token.ThrowIfCancellationRequested();
                if (item.IsCalculated) continue;
                string htmlCode;
                try
                {
                    htmlCode = client.DownloadString(item.Address);
                }
                catch (WebException)
                {
                    _errorModelsList.Add(item);
                    continue;
                }
                item.SetData(htmlCode);
            }
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
