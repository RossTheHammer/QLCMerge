using QLCMerge.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace QLCMerge
{
    internal class Program
    {
        // TODO: make these configurable options
        private const int _deltaLeadInLength = 30;
        private const int _deltaPreviewLength = 95;

        private static void ShowSyntax()
        {
            var fullName = Assembly.GetEntryAssembly()?.Location;
            var appName = Path.GetFileNameWithoutExtension(fullName);
            WriteOutLine($"{appName} {{leftfile}} {{rightfile}}");
        }

        static void Main(string[] args)
        {
            var (argsValidated, leftPath, rightPath) = ParseArgs( args);

            if(argsValidated && leftPath != null && rightPath != null)
            {
                var left = Loader.OpenProjectFile(leftPath);
                var right = Loader.OpenProjectFile(rightPath);

                if(left.IsValid && right.IsValid)
                {
                    var (leftDefs, rightDefs) = Loader.ShowDivergenceReport(left.XmlDoc, right.XmlDoc);

                    // TODO: auto-merge any new Functions beyond point where files diverge
                    // MERGING NOTES:
                    // "Chaser" Functions have "Step" children with Function ID reference as InnerText
                    // "Show" Functions have "Track" children having "ShowFunction" subchildren with ID referring to Function
                }
            }
        }

        private static (bool Success, string? LeftPath, string? RightPath) ParseArgs(string[] args)
        {
            bool success = false;
            string? leftPath = null;
            string? rightPath = null;

            if (args == null || args.Length == 0 || Regex.IsMatch(args[0], @"^[/\-]+\?$") || args.Length != 2)
            {
                ShowSyntax();
            }
            else
            {
                success = true;
                leftPath = args[0];
                rightPath = args[1];
                if (!File.Exists(leftPath))
                {
                    WriteOutLine($"Missing '{leftPath}'");
                    success = false;
                }
                if (!File.Exists(rightPath))
                {
                    WriteOutLine($"Missing '{rightPath}'");
                    success = false;
                }
            }
            return (success, leftPath, rightPath);
        }
        private static void WriteOutLine(string msg)
        {
            Debug.WriteLine(msg);
            Console.WriteLine(msg);
        }

        private static void WriteOut(string msg)
        {
            Debug.Write(msg);
            Console.Write(msg);
        }
    }
}