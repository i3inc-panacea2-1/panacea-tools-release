using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PluginPackager2.Models
{
    [DataContract]
    public class GenericResponse
    {
        [DataMember(Name="success")]
        public bool Success { get; set; }

        [DataMember(Name="message")]
        public String Message { get; set; }
    }
}
