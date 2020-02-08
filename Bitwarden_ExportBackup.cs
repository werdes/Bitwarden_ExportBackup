using Bitwarden_ExportBackup.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Linq;
using Bitwarden_ExportBackup.Extensions;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Newtonsoft.Json;
using Bitwarden_ExportBackup.OutputMethod;
using Bitwarden_ExportBackup.OutputMethod.ZipAes256;

namespace Bitwarden_ExportBackup
{
    public class Bitwarden_ExportBackup
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        #region Config

        private TimeSpan _loginDuration;
        private bool? _updateCli;
        private string _userPassword;
        private string _userName;

        private TimeSpan LOGIN_DURATION
        {
            get
            {
                if (_loginDuration == TimeSpan.FromSeconds(0))
                {
                    _loginDuration = TimeSpan.Parse(ConfigurationManager.AppSettings["LOGIN_DURATION"]);
                }
                return _loginDuration;
            }
        }

        private bool UPDATE_CLI
        {
            get
            {
                if (!_updateCli.HasValue)
                {
                    _updateCli = Boolean.Parse(ConfigurationManager.AppSettings["UPDATE_CLI"]);
                }
                return _updateCli.Value;
            }
        }

        private string USER_NAME
        {
            get
            {
                if (string.IsNullOrEmpty(_userName))
                {
                    _userName = ConfigurationManager.AppSettings["USERNAME"];
                }
                return _userName;
            }
        }

        private string USER_PASSWORD
        {
            get
            {
                if (string.IsNullOrEmpty(_userPassword))
                {
                    _userPassword = ConfigurationManager.AppSettings["PASSWORD"];
                }
                return _userPassword;
            }
        }



        #endregion

        public Bitwarden_ExportBackup()
        {
            bool exportSuccessful = false;
            try
            {
                if (UPDATE_CLI)
                {
                    BitwardenCli.Instance.CheckForUpdates();
                }

                if (BitwardenCli.Instance.Login(USER_NAME, USER_PASSWORD))
                {
                    BitwardenCli.Instance.Sync();
                    exportSuccessful = Export();
                }

                BitwardenCli.Instance.LogoutIfRequired(LOGIN_DURATION);

                if (!exportSuccessful)
                {
                    _log.Warn("Some errors may have occured during export. Please check the logs and results.");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error logging in", ex);
                Console.ReadLine();
            }
        }


        /// <summary>
        /// Exports the exportable items via CLI into a AES encrypted zip file and saves it
        /// </summary>
        /// <returns></returns>
        private bool Export()
        {
            bool retVal = true;
            List<BitwardenObject> lstItems = null;
            List<BitwardenObject> lstFolders = null;
            List<BitwardenObject> lstOrganizations = null;
            List<BitwardenObject> lstCollections = null;
            List<ExportItem> lstExportItems = new List<ExportItem>();

            try
            {

                _log.Info("Exporting..");

                lstItems = BitwardenCli.Instance.GetObjects(BitwardenCli.ObjectType.Items);
                lstFolders = BitwardenCli.Instance.GetObjects(BitwardenCli.ObjectType.Folders);
                lstOrganizations = BitwardenCli.Instance.GetObjects(BitwardenCli.ObjectType.Organizations);
                lstCollections = BitwardenCli.Instance.GetObjects(BitwardenCli.ObjectType.Collections);

                foreach (BitwardenObject obj in lstItems)
                {
                    lstExportItems.Add(new ExportItem()
                    {
                        Collections = (obj.CollectionIds != null && obj.CollectionIds.Count > 0) ? lstCollections.Where(x => obj.CollectionIds.Contains<Guid>(x.Id.Value)).ToList() : null,
                        Folder = lstFolders.Where(x => x.Id.Equals(obj.FolderId)).FirstOrDefault(),
                        Object = obj,
                        Organization = lstOrganizations.Where(x => x.Id.Equals(obj.OrganizationId)).FirstOrDefault()
                    });
                }

                retVal = ArchiveVault(lstExportItems);

            }
            catch (Exception ex)
            {
                _log.Error("Error while exporting", ex);
                retVal = false;
            }
            return retVal;
        }



        /// <summary>
        /// Saves the exported items as a csv file, encrypts it as zip file
        /// </summary>
        /// <param name="lstItems"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private bool ArchiveVault(List<ExportItem> lstItems)
        {
            bool retVal = false;
            string outputName = string.Format("Bitwarden_ExportBackup_{0:yyyy-MM-dd_HH-mm-ss}.zip",
                DateTime.Now);

            IOutputMethod exporter = OutputMethodSelector.GetOutputMethod(outputName);
            if (exporter != null)
            {
                retVal = exporter.Archive(lstItems);
            }

            return retVal;
        }


    }
}
