using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Mono.Cecil;
using Panacea.Tools.Release.Models;
using ServiceStack.Text;

namespace Panacea.Tools.Release
{
    public class ProjectHelper
    {
        private SolutionInfo _info;
        private List<RemoteProject> _projectInfo;
        private RemoteProject _coreInfo;

        public RemoteProject CoreProjectInfo
        {
            get
            {
                return _coreInfo;
            }
        }

        public List<RemoteProject> PluginsProjectInfo
        {
            get
            {
                return _projectInfo;
            }
        }

        public ProjectHelper(SolutionInfo info)
        {
            _info = info;
            _projectInfo =  new List<RemoteProject>();
        }


       
      

        public static string CreateMd5ForFolder(string path)
        {
            var dirs = Directory.GetDirectories(path, "obj", SearchOption.AllDirectories);
            if (dirs.Length > 0)
            {
                foreach (var dir in dirs)
                {
                    Directory.Delete(dir, true);
                }
                
            }
            dirs = Directory.GetDirectories(path, "bin", SearchOption.AllDirectories);
            if (dirs.Length> 0)
            {
                foreach (var dir in dirs)
                {
                    Directory.Delete(dir, true);
                }
            }
            // assuming you want to include nested folders
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                 .OrderBy(p => p).ToList();

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
    }
}
