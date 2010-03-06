using System.Collections.Specialized;
using System.IO;

namespace VersionOne.Provisioning
{
    public static class Utils
    {
        public static string GetCommaDelimitedList(StringCollection strings)
        {
            string[] stringArray = new string[strings.Count];
            strings.CopyTo(stringArray, 0);
            return string.Join(", ", stringArray);
        }

        public static string ReadFile(string filename)
        {
            string s = "";
            using (StreamReader rdr = File.OpenText(filename))
            {
                s = rdr.ReadToEnd();
            }
            return s;
        }
    }
}