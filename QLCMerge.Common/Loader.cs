using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace QLCMerge.Common
{
    public static class Loader
    {
        // TODO: make these configurable options
        private const int _deltaLeadInLength = 30;
        private const int _deltaPreviewLength = 95;

        public static (bool IsValid, XmlDocument XmlDoc) OpenProjectFile(string path)
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

        public static Dictionary<int, FixtureDef> DiscoverFixtures(XmlDocument xml)
        {
            var fixtureDefs = new Dictionary<int, FixtureDef>();

            var fixtureXmlNodes = GetFixtures(xml);

            if (fixtureXmlNodes != null)
            {
                var offsetPointer = 0;
                while (offsetPointer < fixtureXmlNodes.Count)
                {
                    var funcDef = GetKeyFixtureValues(fixtureXmlNodes[offsetPointer] as XmlElement);

                    if (funcDef == null)
                    {
                        // Maybe not an element, or other failure, just increment to skip
                        offsetPointer++;
                    }
                    else
                    {
                        fixtureDefs.Add(offsetPointer++, funcDef);
                    }
                }
            }
            return fixtureDefs;
        }

        public static Dictionary<int, FunctionDef> DiscoverFunctions(XmlDocument xml)
        {
            var funcDefs = new Dictionary<int, FunctionDef>();
            
            var funcXmlNodes = GetFunctions(xml);
            
            if(funcXmlNodes != null)
            {
                var offsetPointer = 0;
                while (offsetPointer < funcXmlNodes.Count)
                {
                    var funcDef = GetKeyFunctionValues(funcXmlNodes[offsetPointer] as XmlElement);

                    if (funcDef == null)
                    {
                        // Maybe not an element, or other failure, just increment to skip
                        offsetPointer++;
                    } 
                    else
                    {
                        funcDefs.Add(offsetPointer++, funcDef);
                    }
                }
            }
            return funcDefs;
        }

        public static (Dictionary<int, FunctionDef> LeftDefs, Dictionary<int, FunctionDef> RightDefs) ShowDivergenceReport(XmlDocument xml1, XmlDocument xml2)
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

                while (leftOffset < leftFunctions.Count && rightOffset < rightFunctions.Count)
                {
                    var leftDef = GetKeyFunctionValues(leftFunctions[leftOffset] as XmlElement);
                    var rightDef = GetKeyFunctionValues(rightFunctions[rightOffset] as XmlElement);

                    if (leftDef == null)
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
                                CompareInners(leftDef.Inner, rightDef.Inner, "    ");
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
                        leftDefs.Add(leftOffset++, leftDef);
                        rightDefs.Add(rightOffset++, rightDef);
                    }
                }
                WriteOutLine($"*** FORKED AT: [{leftForkedAt}]|[{rightForkedAt}] ***");
                if (leftFunctions.Count > leftOffset)
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

        public static XmlNodeList? GetFixtures(XmlDocument xmlDoc)
        {
            var engine = xmlDoc.DocumentElement?.GetElementsByTagName("Engine");
            return engine != null ? (engine[0] as XmlElement)?.GetElementsByTagName("Fixture") : null;
        }

        public static XmlNodeList? GetFunctions(XmlDocument xmlDoc)
        {
            var engine = xmlDoc.DocumentElement?.GetElementsByTagName("Engine");
            return engine != null ? (engine[0] as XmlElement)?.GetElementsByTagName("Function") : null;
        }

        private static FunctionDef? FindInRight(FunctionDef compDef, XmlNodeList list, FunctionDef? ignoreDef = null) => FindIn(compDef, list, "->", ignoreDef);
        private static FunctionDef? FindInLeft(FunctionDef compDef, XmlNodeList list, FunctionDef? ignoreDef = null) => FindIn(compDef, list, "<-", ignoreDef);

        private static FunctionDef? FindIn(FunctionDef compDef, XmlNodeList list, string direction, FunctionDef? ignoreDef = null)
        {
            if (list != null)
            {
                foreach (var item in list)
                {
                    if (item is XmlElement elem)
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

        private static FixtureDef? GetKeyFixtureValues(XmlElement? element)
        {
            if (element == null)
            {
                return null;
            }

            var fixture = new FixtureDef()
            { 
                Inner = element.InnerXml 
            };

            foreach (var item in element.ChildNodes)
            {
                if (item is XmlElement elem)
                {
                    switch (elem.Name)
                    {
                        case "ID":
                            if(int.TryParse(elem.InnerText, out var id))
                            {
                                fixture.Id = id;
                            }
                            break;
                        case "Name":
                            fixture.Name = elem.InnerText; 
                            break;
                        case "Manufacturer":
                            fixture.Manufacturer = elem.InnerText;
                            break;
                        case "Model":
                            fixture.Model = elem.InnerText;
                            break;
                        case "Address":
                            if (int.TryParse(elem.InnerText, out var addr))
                            {
                                fixture.Address = addr;
                            }
                            break;
                        case "Channels":
                            if (int.TryParse(elem.InnerText, out var chans))
                            {
                                fixture.Channels = chans;
                            }
                            break;
                    }
                }
            }
            return fixture;
        }

        private static FunctionDef? GetKeyFunctionValues(XmlElement? element)
        {
            if (element == null)
            {
                return null;
            }

            int.TryParse(element.GetAttribute("ID"), out var id);
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
                    while (c++ < endPtr)
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
