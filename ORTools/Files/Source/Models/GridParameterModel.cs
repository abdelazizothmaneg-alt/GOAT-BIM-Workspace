using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace ORTools.Models
{
    public class GridTypeItem
    {
        public string Name { get; set; }
        public ElementId TypeId { get; set; }
        public override string ToString() => Name;
    }

    public enum BubbleDisplayMode
    {
        GridTypeDefault,
        BothEnds,
        End1Only,
        End2Only,
        None
    }

    public class GridRowItem : INotifyPropertyChanged
    {
        private int _index;
        private string _name;
        private double _fromPreviousMm;
        private bool _hasNameIssue;

        public int Index
        {
            get => _index;
            set { _index = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public double FromPreviousMm
        {
            get => _fromPreviousMm;
            set { _fromPreviousMm = value; OnPropertyChanged(); }
        }

        public bool HasNameIssue
        {
            get => _hasNameIssue;
            set { _hasNameIssue = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public static class GridFormulaHelper
    {
        public static List<double> ParseSpacingFormula(string formula, int count)
        {
            List<double> result = new List<double>();
            if (count <= 0) return result;

            result.Add(0.0);
            if (count == 1) return result;

            List<double> parsedSteps = new List<double>();
            if (!string.IsNullOrWhiteSpace(formula))
            {
                string[] tokens = formula.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string rawToken in tokens)
                {
                    string token = rawToken.Trim();
                    if (token.Contains("*"))
                    {
                        string[] parts = token.Split('*');
                        if (parts.Length == 2 &&
                            int.TryParse(parts[0].Trim(), out int repeat) &&
                            double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dist))
                        {
                            for (int r = 0; r < Math.Max(1, repeat); r++)
                                parsedSteps.Add(dist);
                        }
                    }
                    else if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double singleDist))
                    {
                        parsedSteps.Add(singleDist);
                    }
                }
            }

            if (parsedSteps.Count == 0)
                parsedSteps.Add(6000.0);

            for (int i = 1; i < count; i++)
            {
                double step = (i - 1) < parsedSteps.Count ? parsedSteps[i - 1] : parsedSteps[parsedSteps.Count - 1];
                result.Add(step);
            }

            return result;
        }

        public static List<string> GenerateGridNames(int count, string startToken, string prefix, string suffix, bool skipIAndO)
        {
            List<string> names = new List<string>();
            if (count <= 0) return names;

            string st = string.IsNullOrWhiteSpace(startToken) ? "1" : startToken.Trim();
            string pre = prefix ?? string.Empty;
            string suf = suffix ?? string.Empty;

            if (int.TryParse(st, out int startNum))
            {
                for (int i = 0; i < count; i++)
                {
                    names.Add($"{pre}{startNum + i}{suf}");
                }
                return names;
            }

            if (st.Length == 1 && char.IsLetter(st[0]))
            {
                bool isUpper = char.IsUpper(st[0]);
                int currentCode = char.ToUpperInvariant(st[0]) - 'A';

                for (int i = 0; i < count; i++)
                {
                    string letter = IndexToAlpha(currentCode, isUpper);
                    while (skipIAndO && ContainsIOrO(letter))
                    {
                        currentCode++;
                        letter = IndexToAlpha(currentCode, isUpper);
                    }
                    names.Add($"{pre}{letter}{suf}");
                    currentCode++;
                }
                return names;
            }

            int splitIdx = st.Length;
            while (splitIdx > 0 && char.IsDigit(st[splitIdx - 1]))
            {
                splitIdx--;
            }

            if (splitIdx < st.Length && splitIdx > 0)
            {
                string head = st.Substring(0, splitIdx);
                string numPart = st.Substring(splitIdx);
                if (int.TryParse(numPart, out int baseNum))
                {
                    for (int i = 0; i < count; i++)
                    {
                        names.Add($"{pre}{head}{baseNum + i}{suf}");
                    }
                    return names;
                }
            }

            for (int i = 0; i < count; i++)
            {
                names.Add($"{pre}{st}_{i + 1}{suf}");
            }
            return names;
        }

        private static bool ContainsIOrO(string s)
        {
            foreach (char c in s)
            {
                char u = char.ToUpperInvariant(c);
                if (u == 'I' || u == 'O') return true;
            }
            return false;
        }

        private static string IndexToAlpha(int index, bool upper)
        {
            if (index < 0) index = 0;
            string result = string.Empty;
            int n = index;
            while (n >= 0)
            {
                char c = (char)('A' + (n % 26));
                result = c + result;
                n = (n / 26) - 1;
            }
            return upper ? result : result.ToLowerInvariant();
        }
    }
}
