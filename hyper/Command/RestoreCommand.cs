using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace hyper.Command
{
    public class RestoreCommand : BaseCommand
    {
        private static Regex regex = new Regex(@"^restore\s*([a-zA-Z_0-9.-]+)?\s*\!");

        public override bool Start()
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        public static bool IsMatch(string input)
        {
            return regex.IsMatch(input);
        }

        public static string GetFileName(string restoreCmd)
        {
            var file = regex.Match(restoreCmd).Groups[1].Value;
            if (String.IsNullOrEmpty(file))
            {
                return "eeprom.bin";
            }
            else
            {
                return file;
            }
        }
    }
}
