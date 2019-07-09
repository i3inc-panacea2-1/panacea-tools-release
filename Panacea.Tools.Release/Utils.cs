using Mono.Cecil;
using PluginPackager2.Models;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Panacea.Tools.Release
{
    public static class Utils
    {

        public static string CreateMd5ForFolder(string path, List<string> foldersToExclude)
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                 .OrderBy(p => p).ToList();
            foreach(var file in files.ToList())
            {
                if(foldersToExclude.Any(x=> file.StartsWith(x)))
                {
                    files.Remove(file);
                }
            }
            var md5 = MD5.Create();

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];

                // hash path
                var relativePath = file.Substring(path.Length);
                var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                // hash contents
                var contentBytes = File.ReadAllBytes(file);
                if (i == files.Count - 1)
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                else
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }

        public static void Panic(string message, params string[] param)
        {
            MessageBox.Show(String.Format(message, param), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });

        }
    }
}
