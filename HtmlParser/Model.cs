using System;
using System.Text.RegularExpressions;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace HtmlParser
{
    public class Model: ObservableObject
    {
        public Uri Address { get; }
        public int Count { get; private set; }
        public bool IsCalculated { get; private set; }
        public Model(Uri address)
        {
            Address = address;
        }

        public void SetData(string input, string str)
        {
            Count = Regex.Matches(input, str).Count;
            IsCalculated = true;
        }

        public void ResetData()
        {
            Count = 0;
            IsCalculated = false;
        }
    }
}