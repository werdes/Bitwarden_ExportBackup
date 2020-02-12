using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model.Exceptions
{
    public class BitwardenSessionEmptyException : Exception
    {
        public BitwardenCli _cli;

        public BitwardenSessionEmptyException(BitwardenCli cli)
        {
            _cli = cli;
        }

        public override string ToString()
        {
            return _cli.ToString() + " " + base.ToString();
        }
    }
}
