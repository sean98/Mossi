using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MossiApi
{
    public static class Log
    {
        public static void WriteLine(string msg, [CallerMemberName] string caller = null, [CallerLineNumber] int lineNumber = 0)
        {
            System.Diagnostics.Debug.WriteLine("From: " + caller + ". at: " + lineNumber + "\tLog: " + msg);
        }
    }
}
