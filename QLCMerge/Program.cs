using System.Text.RegularExpressions;
using System.Xml;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System;

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
                var left = OpenProjectFile(leftPath);
                var right = OpenProjectFile(rightPath);

                if(left.IsValid && right.IsValid)
                {
                    var (leftDefs, rightDefs) = ShowDivergenceReport(left.XmlDoc, right.XmlDoc);

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

        private static (bool IsValid, XmlDocument XmlDoc) OpenProjectFile(string path)
        {
            bool isValid = true;
            var xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = false;

            try
            {
                xmlDoc.Load(path);
            }
            catch (Exception e)
            {
                WriteOutLine($"<{path}> Not a valid XML document: {e.Message}");
                isValid = false;
            }

            return (isValid, xmlDoc);
        }

        private static (Dictionary<int,FunctionDef> LeftDefs, Dictionary<int,FunctionDef> RightDefs) ShowDivergenceReport(XmlDocument xml1, XmlDocument xml2)
        {
            var leftDefs = new Dictionary<int, FunctionDef>();
            var rightDefs = new Dictionary<int, FunctionDef>();

            var leftForkedAt = 0;
            var rightForkedAt = 0;

            var leftFunctions = GetFunctions(xml1);
            var rightFunctions = GetFunctions(xml2);
            if (leftFunctions == null || rightFunctions == null)
            {
                WriteOutLine("Could not find functions in one or both of the files");
            }
            else
            {
                var synced = true;
                var leftOffset = 0;
                var rightOffset = 0;
                var lastId = 0;

                while(leftOffset < leftFunctions.Count && rightOffset < rightFunctions.Count) 
                {
                    var leftDef = GetKeyFunctionValues(leftFunctions[leftOffset] as XmlElement);
                    var rightDef = GetKeyFunctionValues(rightFunctions[rightOffset] as XmlElement);

                    if(leftDef == null)
                    {
                        // Maybe not an element, or other failure, just increment to skip
                        leftOffset++;
                    }
                    else if (rightDef == null)
                    {
                        // Maybe not an element, or other failure, just increment to skip
                        rightOffset++;
                    }
                    else if (leftDef.Id > rightDef.Id)
                    {
                        var found = FindInLeft(rightDef, leftFunctions);
                        if (found == null)
                        {
                            WriteOutLine($"RIGHT ONLY:\n  {FormatForType(rightDef)}");
                        }
                        else
                        {
                            rightDef.MapTo = found.Id;
                        }
                        rightDefs.Add(rightOffset++, rightDef);
                    }
                    else if (leftDef.Id < rightDef.Id)
                    {
                        var found = FindInRight(leftDef, rightFunctions);
                        if (found == null)
                        {
                            WriteOutLine($"LEFT ONLY:\n  {FormatForType(leftDef)}");
                        }
                        else
                        {
                            leftDef.MapTo = found.Id;
                        }
                        leftDefs.Add(leftOffset++, leftDef);
                    }
                    else
                    {
                        int innerUnmatched = 0;
                        if (leftDef.Inner != rightDef.Inner)
                        {
                            innerUnmatched++;
                            WriteOutLine($"CONTENT:\n  {FormatForType(leftDef)}");

                            var right = FindInRight(leftDef, rightFunctions, rightDef);
                            var left = FindInLeft(rightDef, leftFunctions, leftDef);
                            if (right == null || left == null)
                            {
                                CompareInners(leftDef.Inner,rightDef.Inner, "    ");
                            }
                        }
                        if (leftDef.Id != rightDef.Id)
                        {
                            WriteOutLine($"ID:\n  [{leftDef.Id}]\n  [{rightDef.Id}]");
                            innerUnmatched++;
                        }
                        if (leftDef.ElemType != rightDef.ElemType)
                        {
                            WriteOutLine($"TYPE:\n  {FormatForType(leftDef)}\n  {FormatForType(rightDef)}");
                            innerUnmatched++;
                        }
                        if (leftDef.Name != rightDef.Name)
                        {
                            WriteOutLine($"NAME:\n  {FormatForName(leftDef)}\n  {FormatForName(rightDef)}");
                            innerUnmatched++;
                        }
                        if (innerUnmatched >= 2)
                        {
                            if (synced)
                            {
                                leftForkedAt = leftOffset;
                                rightForkedAt = rightOffset;
                            }
                            synced = false;

                        }
                        lastId = Math.Max(leftDef.Id, rightDef.Id);
                        leftDefs.Add( leftOffset++, leftDef);
                        rightDefs.Add( rightOffset++, rightDef);
                    }
                }
                WriteOutLine($"*** FORKED AT: [{leftForkedAt}]|[{rightForkedAt}] ***");
                if(leftFunctions.Count > leftOffset)
                {
                    WriteOutLine($"*** LEFT HAS {leftFunctions.Count - leftOffset} MORE FUNCTIONS AFTER ID [{lastId}] (index {leftOffset - 1}) ***");

                    CaptureRemainder(leftDefs, leftFunctions, leftOffset);
                }
                if (rightFunctions.Count > rightOffset)
                {
                    WriteOutLine($"*** RIGHT HAS {rightFunctions.Count - rightOffset} MORE FUNCTIONS AFTER ID [{lastId}] (index {rightOffset - 1}) ***");

                    CaptureRemainder(rightDefs, rightFunctions, rightOffset);
                }
            }

            return (leftDefs, rightDefs);
        }

        private static void CaptureRemainder(Dictionary<int, FunctionDef> defs, XmlNodeList functionList, int baseOffset)
        {
            for (int o = baseOffset; o < functionList.Count; o++)
            {
                var fill = GetKeyFunctionValues(functionList[o] as XmlElement);
                if (fill != null)
                {
                    defs.Add(o, fill);
                }
            }
        }

        private static XmlNodeList? GetFunctions(XmlDocument xmlDoc)
        {
            var engine = xmlDoc.DocumentElement?.GetElementsByTagName("Engine");
            return engine != null ? (engine[0] as XmlElement)?.GetElementsByTagName("Function") : null;
        }

        private static FunctionDef? FindInRight(FunctionDef compDef, XmlNodeList list, FunctionDef? ignoreDef = null) => FindIn(compDef, list, "->", ignoreDef);
        private static FunctionDef? FindInLeft(FunctionDef compDef, XmlNodeList list, FunctionDef? ignoreDef = null) => FindIn(compDef, list, "<-", ignoreDef);

        private static FunctionDef? FindIn(FunctionDef compDef, XmlNodeList list, string direction, FunctionDef? ignoreDef = null)
        {
            if(list != null)
            {
                foreach( var item in list)
                {
                    if(item is XmlElement elem)
                    {
                        var itemDef = GetKeyFunctionValues(elem);
                        if (itemDef != null)
                        {
                            if (ignoreDef != null && ignoreDef.Id == itemDef.Id && ignoreDef.Name == itemDef.Name)
                            {
                                // ignored
                            }
                            else if (itemDef.ElemType == compDef.ElemType && (itemDef.Name.StartsWith(compDef.Name) || compDef.Name.StartsWith(itemDef.Name)))
                            {
                                if (itemDef.Inner == compDef.Inner)
                                {
                                    WriteOutLine($" -- MATCHED: {compDef.Id} {direction} {itemDef.Id}");
                                    return itemDef;
                                }
                                WriteOutLine($" -- POSSIBLE: {compDef.Id} {direction} {itemDef.Id}");
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static FunctionDef? GetKeyFunctionValues(XmlElement? element)
        {
            if(element == null)
            {
                return null;
            }

            int.TryParse( element.GetAttribute("ID"), out var id);
            var name = element.GetAttribute("Name");
            var elType = element.GetAttribute("Type");

            return new FunctionDef(id, name, elType, element.InnerXml);
        }

        private static string FormatForId(FunctionDef def) => $"[{def.Id}]";
        private static string FormatForName(FunctionDef def) => $"[{def.Id}]:\"{def.Name}\"";
        private static string FormatForType(FunctionDef def) => $"[{def.Id}]:({def.ElemType}):\"{def.Name}\"";


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

        private class FunctionDef
        {
            public int Id { get; set; }
            public int? MapTo { get; set; }
            public string Name { get; set; }
            public string? Rename { get; set; }
            public string ElemType { get; set; }
            public string Inner { get; set; }
            public FunctionDef(int id, string name, string elemType, string inner, int? mapTo = null, string? rename = null)
            {
                Id = id;
                MapTo = mapTo;
                Name = name;
                Rename = rename;
                ElemType = elemType;
                Inner = inner;
            }
        }

        private class FixedStringBuffer
        {
            public int MaxLength { get; }
            private StringBuilder _sb = new StringBuilder();

            public FixedStringBuffer(int maxLength)
            {
                MaxLength = maxLength;
            }

            public void Push(char c) 
            {
                _sb.Insert(0, c);
                while (_sb.Length > MaxLength)
                {
                    _sb.Remove(MaxLength, 1);
                }
            }

            public void Append(char c)
            {
                _sb.Append(c);
                while (_sb.Length > MaxLength)
                {
                    _sb.Remove(0,1);
                }
            }

            public void SafeAppend(char[] chars, int index, char? filler = null)
            {
                if (chars.Length > index)
                {
                    this.Append(chars[index]);
                }
                else if(filler != null)
                {
                    this.Append(filler.Value);
                }
            }

            public override string ToString()
            {
                return _sb.ToString();
            }
        }

        private static void CompareInners(string left, string right, string prefix = "")
        {
            var leftBuffer = new FixedStringBuffer(_deltaPreviewLength);
            var rightBuffer = new FixedStringBuffer(_deltaPreviewLength);

            var leftChars = left.ToCharArray();
            var rightChars = right.ToCharArray();

            for (var c = 0; c <= leftChars.Length && c <= rightChars.Length; c++)
            {
                leftBuffer.SafeAppend(leftChars, c, ' ');
                rightBuffer.SafeAppend(rightChars, c, ' ');

                if (leftChars.Length <= c || rightChars.Length <= c || leftChars[c] != rightChars[c])
                {
                    var endPtr = c + _deltaPreviewLength - _deltaLeadInLength;
                    while(c++ < endPtr)
                    {
                        leftBuffer.SafeAppend(leftChars, c, ' ');
                        rightBuffer.SafeAppend(rightChars, c, ' ');
                    }
                    break;
                }
            }

            var leftFinal = leftBuffer.ToString().Trim();
            var rightFinal = rightBuffer.ToString().Trim();
            WriteOutLine(prefix + leftFinal); // + (leftFinal.Length < _deltaPreviewLength ? "..." : ""));
            WriteOutLine(prefix + rightFinal); // + (rightFinal.Length < _deltaPreviewLength ? "..." : ""));
        }
    }
}