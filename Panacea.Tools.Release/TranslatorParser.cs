using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ServiceStack.Text;

namespace Panacea.Tools.Release
{
    internal class TranslatorParser
    {
        public static Task<Dictionary<string, List<string>>> GetTranslations(string path)
        {
            return Task.Run(() =>
            {
                var info = new DirectoryInfo(path);
                var translations = new Dictionary<string, List<string>>();
                MessageHelper.OnMessage("Parsing Xaml files for translations");
                ParseXamlFiles(info, ref translations);
                MessageHelper.OnMessage("Parsing Guide files for translations");
                ParseGuideFiles(info, ref translations);
                MessageHelper.OnMessage("Parsing C# files for translations");
                ParseCSharpFiles(info, ref translations);
                return translations;
            });
        }

        private static void ParseXamlFiles(DirectoryInfo info, ref Dictionary<string, List<string>> translations)
        {
            foreach (var xaml in from file in Directory.GetFiles(info.FullName, "*.xaml", SearchOption.AllDirectories) 
                                 select File.ReadAllText(file) into xaml let doc = XDocument.Parse(xaml) select xaml)
            {
                using (var reader = XmlReader.Create(new StringReader(xaml)))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType != XmlNodeType.Element) continue;
                        while (reader.MoveToNextAttribute())
                        {
                            if (!reader.ReadAttributeValue()) continue;
                            if (!reader.Value.Contains(":Translate")) continue;
                            var regex = new Regex(@"'([^']+)'");
                            var match = regex.Matches(reader.Value);
                            if (match.Count != 2) continue;
                           
                            var plugin = match[1].Groups[1].Value;
                            var text = match[0].Groups[1].Value;
                            if (!translations.ContainsKey(plugin) || translations[plugin] == null)
                            {
                                translations[plugin] = new List<string>();
                            }

                            if (String.IsNullOrEmpty(text)) continue;
                            if (!translations[plugin].Contains(text))
                                translations[plugin].Add(text);
                        }
                    }
                }
            }
        }

        private static void ParseGuideFiles(DirectoryInfo info, ref Dictionary<string, List<string>> translations)
        {
            foreach (
                var file in
                    Directory.GetFiles(info.FullName, "*.guide", SearchOption.AllDirectories))
            {
                var json = File.ReadAllText(file);
                var obj = JsonSerializer.DeserializeFromString<Guide>(json);
                foreach (var action in obj.Actions.Where(action => !string.IsNullOrEmpty(action.Text)))
                {
                    if (!translations.ContainsKey(obj.Plugin) || translations[obj.Plugin] == null)
                    {
                        translations[obj.Plugin] = new List<string>();
                    }
                    if (String.IsNullOrEmpty(action.Text)) continue;
                    if (!translations[obj.Plugin].Contains(action.Text))
                        translations[obj.Plugin].Add(action.Text);
                }
            }
        }

        private static void ParseCSharpFiles(DirectoryInfo info, ref Dictionary<string, List<string>> translations)
        {
            foreach (
                var file in
                    Directory.GetFiles(info.FullName, "*.xaml.cs", SearchOption.AllDirectories))
            {
                var code = File.ReadAllText(file);

                // example of code
                // var a = new Translator("core"); s.Translate("*");
                var regex = new Regex(@"([\s\r\n]*)(var|Translator)([\s\r\n]+)([^=\s]+)([\s\r\n]*)=([\s\r\n]*)new([\s\r\n]+)Translator([\s\r\n]*)\(([\s\r\n]*)\""([^\""""]+)\""([\s\r\n]*)\)");
                var match = regex.Matches(code);

                foreach (Match mm in match)
                {
                    var nameOfTranslator =mm.Groups[4].Value;
                    var plugin = mm.Groups[10].Value;

                    var regex1 = new Regex(nameOfTranslator + @"([\s\r\n]*)\.([\s\r\n]*)Translate([\s\r\n]*)\(([\s\r\n]*)\""([^\""""]+)\""([\s\r\n]*)\)");
                    var match1 = regex1.Matches(code);
                    if (match1.Count == 0)
                    {
                        Console.WriteLine("You have created a translator but do not use it anywhere. Please use it or remove it. Variable '{0}', File '{1}'", nameOfTranslator, file);
                    }
                    foreach (var text in from object m in match1 select match1[0].Groups[5].Value)
                    {
                        if(string.IsNullOrEmpty(text))continue;
                        if (!translations.ContainsKey(plugin) || translations[plugin] == null)
                        {
                            translations[plugin] = new List<string>();
                        }
                        if (!translations[plugin].Contains(text))
                            translations[plugin].Add(text);
                    }
                }
                

                // example code
                // new Tranlator("core").Translate("asdf");
               
                regex = new Regex(@"new([\s\r\n]+)Translator([\s\r\n]*)\(([\s\r\n]*)\""([^\""""]+)\""([\s\r\n]*)\)([\s\r\n]*)\.([\s\r\n]*)Translate([\s\r\n]*)\(([\s\r\n]*)\""([^\""""]+)\""([\s\r\n]*)\)");
                match = regex.Matches(code);

                foreach (Match m in match)
                {
                    var plugin = m.Groups[4].Value;
                    var text = m.Groups[10].Value;
                    if (string.IsNullOrEmpty(text)) continue;
                    if (!translations.ContainsKey(plugin) || translations[plugin] == null)
                    {
                        translations[plugin] = new List<string>();
                    }
                    if (!translations[plugin].Contains(text))
                        translations[plugin].Add(text);

                }
            }
        }

        public class GuideAction
        {
            public string ElementName { get; set; }

            public string Event { get; set; }

            public GuideActions Type { get; set; }

            public string Text { get; set; }

            public int Delay { get; set; }

            public bool Handled { get; set; }

            public string Image { get; set; }

            public HighlightType Highlight { get; set; }

            public string Background { get; set; }

            public CustomAction CustomAction { get; set; }
        }

        public class CustomAction
        {
            public CustomActionType Type { get; set; }
            public CustomActionFunction Function { get; set; }
            public string Argument { get; set; }
            public string Text { get; set; }
        }

        public class Guide
        {
            public string Plugin { get; set; }

            public List<GuideAction> Actions { get; set; }
        }

        public enum CustomActionType
        {
            None,
            Button,
            Link
        }

        public enum CustomActionFunction
        {
            None,
            CallPlugin,
            StartTutorial
        }

        public enum HighlightType
        {
            None,
            Glow,
            Border
        }

        public enum GuideActions
        {
            Responsive,
            Information
        }
    }
}


