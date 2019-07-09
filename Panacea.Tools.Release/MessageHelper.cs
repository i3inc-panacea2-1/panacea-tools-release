using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Tools.Release
{
    public static class MessageHelper
    {
        public static void OnMessage(string s)
        {
            if (Message != null) Message(null, s);
        }

        public static event EventHandler<string> Message;

    }
}
