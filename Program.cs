using System;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log.config", Watch = false)]

namespace Bitwarden_ExportBackup
{
    class Program
    {
        static void Main(string[] args)
        {
            Bitwarden_ExportBackup program = new Bitwarden_ExportBackup();
        }
    }
}
