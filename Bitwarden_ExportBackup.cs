using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Bitwarden_ExportBackup
{
    public class Bitwarden_ExportBackup
    {
        private static readonly log4net.ILog _logWriter = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private BitwardenCli _cli;

        public Bitwarden_ExportBackup()
        {
            TimeSpan loginDuration;
            try
            {
                loginDuration = TimeSpan.Parse(ConfigurationManager.AppSettings["LOGIN_DURATION"]);

                _cli = new BitwardenCli(ConfigurationManager.AppSettings["BITWARDEN_CLI"]);
                _cli.CheckForUpdates();

                _cli.Login(ConfigurationManager.AppSettings["USERNAME"], ConfigurationManager.AppSettings["PASSWORD"]);

                //TODO


                _cli.LogoutIfRequired(loginDuration);
            }
            catch(Exception ex)
            {
                _logWriter.Error(ex);
            }
        }
    }
}
