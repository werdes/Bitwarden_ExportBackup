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
using Bitwarden_ExportBackup.ExportFormats;

namespace Bitwarden_ExportBackup
{
    public class Bitwarden_ExportBackup
    {
        private static readonly log4net.ILog _logWriter = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private BitwardenCli _cli;

        #region Config

        private string _exportDirectory;
        private TimeSpan _loginDuration;
        private bool? _updateCli;
        private string _cliPath;
        private string _userPassword;
        private string _userName;
        private string _backupPasswordName;
        private bool? _deleteOldFiles;
        private int? _keepFilesAmount;
        private List<ExportFormat> _exportFormats;

        public string EXPORT_DIRECTORY
        {
            get
            {
                if (string.IsNullOrEmpty(_exportDirectory))
                {
                    _exportDirectory = ConfigurationManager.AppSettings["EXPORT_DIRECTORY"];
                }
                return _exportDirectory;
            }
        }

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

        private string CLI_PATH
        {
            get
            {
                if (string.IsNullOrEmpty(_cliPath))
                {
                    _cliPath = ConfigurationManager.AppSettings["BITWARDEN_CLI"];
                }
                return _cliPath;
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

        private string BACKUP_PASSWORD_NAME
        {
            get
            {
                if (string.IsNullOrEmpty(_backupPasswordName))
                {
                    _backupPasswordName = ConfigurationManager.AppSettings["BACKUP_PASSWORD_NAME"];
                }
                return _backupPasswordName;
            }
        }

        private bool DELETE_OLD_FILES
        {
            get
            {
                if (!_deleteOldFiles.HasValue)
                {
                    _deleteOldFiles = bool.Parse(ConfigurationManager.AppSettings["DELETE_OLD_FILES"]);
                }
                return _deleteOldFiles.Value;
            }
        }

        private int KEEP_FILES_AMOUNT
        {
            get
            {
                if (!_keepFilesAmount.HasValue)
                {
                    _keepFilesAmount = int.Parse(ConfigurationManager.AppSettings["KEEP_FILES_AMOUNT"]);
                }
                return _keepFilesAmount.Value;
            }
        }

        private List<ExportFormat> EXPORT_FORMATS
        {
            get
            {
                if (_exportFormats == null || _exportFormats.Count == 0)
                {
                    _exportFormats = ConfigurationManager.AppSettings["EXPORT_FORMATS"].Split(',').Select(x => (ExportFormat)Enum.Parse(typeof(ExportFormat), x)).ToList();
                }
                return _exportFormats;
            }
        }

        #endregion

        public Bitwarden_ExportBackup()
        {
            bool exportSuccessful = false;
            try
            {

                _cli = new BitwardenCli(CLI_PATH);

                if (UPDATE_CLI)
                {
                    _cli.CheckForUpdates();
                }

                if (_cli.Login(USER_NAME, USER_PASSWORD))
                {
                    _cli.Sync();
                    exportSuccessful = Export();
                }

                if (DELETE_OLD_FILES)
                {
                    DeleteOldArchives();
                }

                _cli.LogoutIfRequired(LOGIN_DURATION);

                if(!exportSuccessful)
                {
                    _logWriter.Warn("Some errors may have occured during export. Please check the logs and results.");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                _logWriter.Error("Error logging in", ex);
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Returns the password used for locking the to be created ZIP archive
        ///  > Will either use the one queried from the vault or create a new Entry in the vault with a new password entered by the user
        /// </summary>
        /// <returns></returns>
        private string GetZipPassword()
        {
            string retVal = null;
            BitwardenObject passwordItem = _cli.GetObjects(BitwardenCli.ObjectType.Items).Where(x => x.Name.Trim().Equals(BACKUP_PASSWORD_NAME)).FirstOrDefault();

            if (passwordItem != null)
            {
                retVal = passwordItem.Login.Password;
            }
            else
            {
                _logWriter.Warn($"No password found for name [{BACKUP_PASSWORD_NAME}]");

                CreatePasswordItem();
                retVal = GetZipPassword();
            }

            return retVal;
        }

        /// <summary>
        /// Creates a password item in the Bitwarden Vault that contains the ZIP Backup password
        /// </summary>
        private void CreatePasswordItem()
        {
            Console.Write("Please set a new password [input is hidden]: ");
            string password = ReadConsoleLineWithoutEcho();

            BitwardenItemTemplate template = _cli.GetItemTemplate();
            template.Name = BACKUP_PASSWORD_NAME;
            template.Notes = "Your Bitwarden ExportBackup password.";
            template.Login = new BitwardenLogin()
            {
                Password = password,
                Username = string.Empty
            };

            _cli.CreateItem(template);
            _cli.Sync();
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

                _logWriter.Info("Exporting..");

                lstItems = _cli.GetObjects(BitwardenCli.ObjectType.Items);
                lstFolders = _cli.GetObjects(BitwardenCli.ObjectType.Folders);
                lstOrganizations = _cli.GetObjects(BitwardenCli.ObjectType.Organizations);
                lstCollections = _cli.GetObjects(BitwardenCli.ObjectType.Collections);

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

                retVal = ArchiveVault(lstExportItems, GetZipPassword());

            }
            catch (Exception ex)
            {
                _logWriter.Error("Error while exporting", ex);
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
        private bool ArchiveVault(List<ExportItem> lstItems, string password)
        {
            bool retVal = true;
            string outputPath = Path.Combine(EXPORT_DIRECTORY, string.Format("Bitwarden_ExportBackup_{0:yyyy-MM-dd_HH-mm-ss}.zip",
                DateTime.Now));

            byte[] bufferJsonFile = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(lstItems, Formatting.Indented));
            Dictionary<BitwardenObject.ItemType, List<ExportItem>> dictItemTypes = lstItems.GroupBy(x => x.Object.Type).ToDictionary(k => k.First().Object.Type, v => v.ToList());

            using (FileStream zipFileStream = File.Create(outputPath))
            {
                using (ZipOutputStream zipOutputStream = new ZipOutputStream(zipFileStream))
                {
                    zipOutputStream.SetLevel(9);
                    zipOutputStream.Password = password;

                    //Each output format gets its own folder, each item type gets its own file
                    foreach (ExportFormat currentExportFormat in EXPORT_FORMATS)
                    {
                        foreach (BitwardenObject.ItemType currentItemType in dictItemTypes.Keys)
                        {
                            try
                            {
                                AddFileToArchive(dictItemTypes, zipOutputStream, currentItemType, currentExportFormat);
                            }
                            //Catch EVERYTHING here. Why? Rather export everything else than nothing just because something didn't work.
                            //Json stuff should still be exported in the worst case
                            catch (Exception ex)
                            {
                                retVal = false;
                                _logWriter.Error($"Could not export [{currentItemType}/{currentExportFormat}]", ex);
                            }
                        }
                    }

                    //Pack the raw data as JSON into the ZIP just so people can do their own stuff with it
                    using (MemoryStream jsonStream = new MemoryStream(bufferJsonFile))
                    {
                        ZipEntry entry = new ZipEntry(Path.GetFileNameWithoutExtension(outputPath) + ".json");
                        entry.AESKeySize = 256;
                        entry.Size = bufferJsonFile.Length;

                        zipOutputStream.PutNextEntry(entry);
                        StreamUtils.Copy(jsonStream, zipOutputStream, new byte[4096]);
                        zipOutputStream.CloseEntry();

                        jsonStream.Close();
                    }

                    zipOutputStream.IsStreamOwner = true;
                    zipOutputStream.Close();
                    zipFileStream.Close();
                }

            }

            return retVal;
        }

        /// <summary>
        /// Adds a file to the zip stream
        /// </summary>
        /// <param name="dictItemTypes"></param>
        /// <param name="zipOutputStream"></param>
        /// <param name="currentItemType"></param>
        /// <param name="currentExportFormat"></param>
        private static void AddFileToArchive(Dictionary<BitwardenObject.ItemType, List<ExportItem>> dictItemTypes, ZipOutputStream zipOutputStream, BitwardenObject.ItemType currentItemType, ExportFormat currentExportFormat)
        {
            _logWriter.Info($"Archiving [{currentExportFormat}/{currentItemType}]");
            IExporter exporter = ExportSelector.GetExporter(currentExportFormat);

            //Not all Export Formats do support all item-types (ex. 1Password has no Identity format specified)
            // -> No files for that will be exported.
            if (exporter.GetSupportedItemTypes().Contains(currentItemType))
            {
                byte[] buffer = exporter.GetFileContent(dictItemTypes[currentItemType], currentItemType);

                if (buffer != null)
                {
                    using (MemoryStream currentFormatFileStream = new MemoryStream(buffer))
                    {
                        ZipEntry entry = new ZipEntry($"{currentExportFormat}\\{currentExportFormat}-{currentItemType}.{exporter.GetFileExtension()}");
                        entry.AESKeySize = 256;
                        entry.Size = buffer.Length;

                        zipOutputStream.PutNextEntry(entry);
                        StreamUtils.Copy(currentFormatFileStream, zipOutputStream, new byte[4096]);
                        zipOutputStream.CloseEntry();

                        currentFormatFileStream.Close();

                    }
                }
                else
                {
                    _logWriter.Error($"No output could be created for {currentExportFormat}:{currentItemType}!");
                }
            }
            else
            {
                _logWriter.Warn($"[{currentExportFormat} does not support [{currentItemType}]! No file will be archived, the information will be available from the json export only!");
            }
        }

        /// <summary>
        /// Deletes old archives
        /// </summary>
        private void DeleteOldArchives()
        {

            FileInfo[] filesForDeletion = Directory.GetFiles(EXPORT_DIRECTORY, "*.zip", SearchOption.TopDirectoryOnly).Select(x => new FileInfo(x)).OrderByDescending(x => x.LastAccessTime).Skip(KEEP_FILES_AMOUNT).Where(x => x.LastAccessTime < DateTime.Now).ToArray();
            foreach (FileInfo file in filesForDeletion)
            {
                _logWriter.Info($"Deleting old file: {file.FullName}");
                File.Delete(file.FullName);
            }
        }

        /// <summary>
        /// Reads something from Console without the input appearing in the console
        /// </summary>
        /// <returns></returns>
        private string ReadConsoleLineWithoutEcho()
        {
            string password = "";
            ConsoleKeyInfo ch = Console.ReadKey(true);
            while (ch.Key != ConsoleKey.Enter)
            {
                if (ch.Key == ConsoleKey.Backspace)
                {
                    password = password.Substring(0, password.Length - 1);
                }
                else
                {
                    password += ch.KeyChar;
                }
                ch = Console.ReadKey(true);
            }
            Console.WriteLine();

            return password;
        }
    }
}
