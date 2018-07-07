using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WAS_LoginServer
{
    public class Configuration
    {
        private Dictionary<string, string> objConfig = new Dictionary<string, string>();

        public string this[string strKey]
        {
            get
            {
                return stringGetKeyValue(strKey);
            }
        }

        public Configuration(string strPath)
        {
            if (!File.Exists(strPath))
            {
                // Create a file to write to.
                string[] createText =
                {
                    "###############################################################################",
                    "# ,^.                 __    __                _                           /^\\ #",
                    "# |||                / / /\\ \\ \\__ _ _ __   __| |___                  /\\   \"V\" #",
                    "# |||       _T_      \\ \\/  \\/ / _` | '_ \\ / _` / __|                /__\\   I  #",
                    "# |||   .-.[:|:].-.   \\  /\\  / (_| | | | | (_| \\__ \\               //..\\\\  I  #",
                    "# ===_ /\\|  \"'\"  |/    \\/  \\/ \\__,_|_| |_|\\__,_|___/               \\].`[/  I  #",
                    "#  E]_|\\/ \\--|-|''''|         /_\\  _ __   __| |                    /l\\/j\\  (] #",
                    "#  O  `'  '=[:]| A  |        //_\\\\| '_ \\ / _` |                   /. ~~ ,\\/I  #",
                    "#         /\"\"\"\"|  P |       /  _  \\ | | | (_| |                   \\\\L__j^\\/I  #",
                    "#        /\"\"\"\"\"`.__.'       \\_/ \\_/_| |_|\\__,_|           _        \\/--v}  I  #",
                    "#       []\"/\"\"\"\\\"[]             / _\\_      _____  _ __ __| |___    |    |  I  #",
                    "#       | \\     / |             \\ \\\\ \\ /\\ / / _ \\| '__/ _` / __|   |    |  I  #",
                    "#       | |     | |             _\\ \\\\ V  V / (_) | | | (_| \\__ \\   |    l  I  #",
                    "#     <\\\\\\)     (///>           \\__/ \\_/\\_/ \\___/|_|  \\__,_|___/ _/j  L l\\_!  #",
                    "###############################################################################",
                    "",
                    "###############",
                    "# DB - Server #",
                    "###############",
                    "server      = localhost",
                    "user        = root",
                    "database    = was",
                    "port        = 3306",
                    "password    = s25"
                };
                File.WriteAllLines(strPath, createText);
            }

            string[] readText = File.ReadAllLines(strPath);
            foreach (string s in readText)
            {
                if (isComment(s))
                    continue;

                if (!hasValues(s))
                    continue;

                string strKey = "";
                string strValue = "";

                if (getValue(s, ref strKey, ref strValue))
                    objConfig.Add(strKey, strValue);
            }
        }

        private string stringGetKeyValue(string strKey)
        {
            if (!objConfig.ContainsKey(strKey))
                return "";

            return objConfig[strKey];
        }

        private bool isComment(string strLine)
        {
            return strLine.Length == 0 ? false : strLine.Trim().Substring(0, 1) == "#";
        }

        private bool hasValues(string strLine)
        {
            return strLine.Length == 0 ? false : strLine.Contains("=");
        }

        private bool getValue(string strLine, ref string strKey, ref string strValue)
        {
            string[] strSplit = strLine.Trim().Split('=');

            if (strSplit.Length != 2)
                return false;

            strKey = strSplit[0].Trim();
            strValue = strSplit[1].Trim();

            return true;
        }

    }
}
