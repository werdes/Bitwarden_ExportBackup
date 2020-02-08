using Bitwarden_ExportBackup.Model;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace Bitwarden_ExportBackup.OutputMethod.ZipAes256
{
    public class ZipAes256Exporter : IOutputMethod
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string _outputName;
        private string _exportDirectory;
        private List<ZipAes256ExportFormat> _exportFormats;
        private bool? _deleteOldFiles;
        private int? _keepFilesAmount;
        private string _backupPasswordName;

        private List<ZipAes256ExportFormat> EXPORT_FORMATS
        {
            get
            {
                if (_exportFormats == null || _exportFormats.Count == 0)
                {
                    _exportFormats = ConfigurationManager.AppSettings["ZIPAES256_EXPORT_FORMATS"].Split(',').Select(x => (ZipAes256ExportFormat)Enum.Parse(typeof(ZipAes256ExportFormat), x)).ToList();
                }
                return _exportFormats;
            }
        }

        public string EXPORT_DIRECTORY
        {
            get
            {
                if (string.IsNullOrEmpty(_exportDirectory))
                {
                    _exportDirectory = ConfigurationManager.AppSettings["ZIPAES256_EXPORT_DIRECTORY"];
                }
                return _exportDirectory;
            }
        }

        private bool DELETE_OLD_FILES
        {
            get
            {
                if (!_deleteOldFiles.HasValue)
                {
                    _deleteOldFiles = bool.Parse(ConfigurationManager.AppSettings["ZIPAES256_DELETE_OLD_FILES"]);
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
                    _keepFilesAmount = int.Parse(ConfigurationManager.AppSettings["ZIPAES256_KEEP_FILES_AMOUNT"]);
                }
                return _keepFilesAmount.Value;
            }
        }
        private string BACKUP_PASSWORD_NAME
        {
            get
            {
                if (string.IsNullOrEmpty(_backupPasswordName))
                {
                    _backupPasswordName = ConfigurationManager.AppSettings["ZIPAES256_BACKUP_PASSWORD_NAME"];
                }
                return _backupPasswordName;
            }
        }

        /// <summary>
        /// Constructor
        /// > Ouputname for Filename
        /// </summary>
        /// <param name="password"></param>
        /// <param name="outputName"></param>
        public ZipAes256Exporter(string outputName)
        {
            _outputName = outputName;


            if (DELETE_OLD_FILES)
            {
                DeleteOldArchives();
            }

        }


        /// <summary>
        /// Archives the Vault as AES256 Encrypted Zip
        /// </summary>
        /// <param name="exportItems"></param>
        /// <returns></returns>
        public bool Archive(List<ExportItem> exportItems)
        {
            bool retVal = false;
            byte[] bufferJsonFile = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(exportItems, Formatting.Indented));
            Dictionary<BitwardenObject.ItemType, List<ExportItem>> dictItemTypes = exportItems.GroupBy(x => x.Object.Type).ToDictionary(k => k.First().Object.Type, v => v.ToList());
            string password = GetZipPassword();
            string outputPath = Path.Combine(EXPORT_DIRECTORY, _outputName);

            using (FileStream zipFileStream = File.Create(outputPath))
            {
                using (ZipOutputStream zipOutputStream = new ZipOutputStream(zipFileStream))
                {
                    zipOutputStream.SetLevel(9);
                    zipOutputStream.Password = password;

                    //Each output format gets its own folder, each item type gets its own file
                    foreach (ZipAes256ExportFormat currentExportFormat in EXPORT_FORMATS)
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
                                _log.Error($"Could not export [{currentItemType}/{currentExportFormat}]", ex);
                            }
                        }
                    }

                    //Pack the raw data as JSON into the ZIP just so people can do their own stuff with it
                    using (MemoryStream jsonStream = new MemoryStream(bufferJsonFile))
                    {
                        ZipEntry entry = new ZipEntry(Path.GetFileNameWithoutExtension(_outputName) + ".json");
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
        private static void AddFileToArchive(Dictionary<BitwardenObject.ItemType, List<ExportItem>> dictItemTypes, ZipOutputStream zipOutputStream, BitwardenObject.ItemType currentItemType, ZipAes256ExportFormat currentExportFormat)
        {
            _log.Info($"Archiving [{currentExportFormat}/{currentItemType}]");
            IZipAes256Exporter exporter = ZipAes256ExportSelector.GetExporter(currentExportFormat);

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
                    _log.Error($"No output could be created for {currentExportFormat}:{currentItemType}!");
                }
            }
            else
            {
                _log.Warn($"[{currentExportFormat} does not support [{currentItemType}]! No file will be archived, the information will be available from the json export only!");
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
                _log.Info($"Deleting old file: {file.FullName}");
                File.Delete(file.FullName);
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
            BitwardenObject passwordItem = BitwardenCli.Instance.GetObjects(BitwardenCli.ObjectType.Items).Where(x => x.Name.Trim().Equals(BACKUP_PASSWORD_NAME)).FirstOrDefault();

            if (passwordItem != null)
            {
                retVal = passwordItem.Login.Password;
            }
            else
            {
                _log.Warn($"No password found for name [{BACKUP_PASSWORD_NAME}]");

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

            BitwardenItemTemplate template = BitwardenCli.Instance.GetItemTemplate();
            template.Name = BACKUP_PASSWORD_NAME;
            template.Notes = "Your Bitwarden ExportBackup password.";
            template.Login = new BitwardenLogin()
            {
                Password = password,
                Username = string.Empty
            };

            BitwardenCli.Instance.CreateItem(template);
            BitwardenCli.Instance.Sync();
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

        public string GetExtension()
        {
            return ".zip";
        }
    }
}
