using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Tools.Release
{
    class LocalProject
    {
        public LocalProject(string path)
        {
            CsProjPath = path;
        }

        public string CsProjPath { get; }

        public string BasePath { get => Path.GetDirectoryName(CsProjPath); }

        public string FullName { get => Path.GetFileName(CsProjPath).Replace(".csproj", ""); }

        public string Name { get => Path.GetFileName(CsProjPath).Replace(".csproj", "").Split('.').Last(); }

        public ProjectType ProjectType
        {
            get
            {
                var type = FullName.Split('.')[1];
                switch (type)
                {
                    case "Applications":
                        return ProjectType.Application;
                    case "Modules":
                        return ProjectType.Module;
                    case "Tools":
                        return ProjectType.Tool;
                    default: return ProjectType.Library;
                }
            }
        }

        public string GetDirectoryHashCode()
        {
            return Utils.CreateMd5ForFolder(BasePath, new List<string>() { Path.Combine(BasePath, "bin"), Path.Combine(BasePath, "obj") });
        }
    }


    enum ProjectType
    {
        Application,
        Module,
        Tool,
        Library
    }
}
