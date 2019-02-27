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

namespace Bitwarden_ExportBackup
{
    public class Bitwarden_ExportBackup
    {
        private static readonly log4net.ILog _logWriter = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private BitwardenCli _cli;

        public Bitwarden_ExportBackup()
        {
            TimeSpan loginDuration;
            bool updateCli;

            try
            {
                loginDuration = TimeSpan.Parse(ConfigurationManager.AppSettings["LOGIN_DURATION"]);
                updateCli = Boolean.Parse(ConfigurationManager.AppSettings["UPDATE_CLI"]);

                _cli = new BitwardenCli(ConfigurationManager.AppSettings["BITWARDEN_CLI"]);

                if (updateCli)
                {
                    _cli.CheckForUpdates();
                }

                if (_cli.Login(ConfigurationManager.AppSettings["USERNAME"], ConfigurationManager.AppSettings["PASSWORD"]))
                {
                    _cli.Sync();
                    Export();
                }

                _cli.LogoutIfRequired(loginDuration);
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
            string passwordObjectName = ConfigurationManager.AppSettings["BACKUP_PASSWORD_NAME"];
            BitwardenObject passwordItem = _cli.GetObjects("items").Where(x => x.Name.Trim().Equals(passwordObjectName)).FirstOrDefault();

            if (passwordItem != null)
            {
                retVal = passwordItem.Login.Password;
            }
            else
            {
                _logWriter.Warn($"No password found for name [{passwordObjectName}]");

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
            template.Name = ConfigurationManager.AppSettings["BACKUP_PASSWORD_NAME"];
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
            bool retVal = false;
            List<BitwardenObject> lstItems = null;
            List<BitwardenObject> lstFolders = null;
            List<ExportItem> lstExportItems = new List<ExportItem>();

            try
            {

                _logWriter.Info("Exporting..");

                lstItems = _cli.GetObjects("items");
                lstFolders = _cli.GetObjects("folders");

                foreach (BitwardenObject obj in lstItems)
                {
                    lstExportItems.Add(new ExportItem()
                    {
                        Favorite = obj.IsFavorite,
                        Fields = obj.Fields?.Select(x => x.Name + ":" + x.Value).Join(", "),
                        Folder = lstFolders.Where(x => x.Id.Equals(obj.FolderId)).FirstOrDefault()?.Name,
                        LoginPassword = obj.Login?.Password,
                        LoginTotp = obj.Login?.Totp,
                        LoginUris = obj.Login?.Uris?.Select(x => x.Uri).Join(", "),
                        LoginUsername = obj.Login?.Username,
                        Name = obj.Name,
                        Notes = obj.Notes,
                        Type = obj.Type.ToString()
                    });
                }

                SaveFile(lstExportItems, GetZipPassword());

            }
            catch (Exception ex)
            {
                _logWriter.Error("Error while exporting", ex);
                Console.ReadLine();
            }
            return retVal;
        }



        /// <summary>
        /// Saves the exported items as a csv file, encrypts it as zip file
        /// </summary>
        /// <param name="lstItems"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private bool SaveFile(List<ExportItem> lstItems, string password)
        {
            bool retVal = false;
            string outputPath = Path.Combine(ConfigurationManager.AppSettings["EXPORT_DIRECTORY"], string.Format("Bitwarden_ExportBackup_{0:yyyy-MM-dd_HH-mm-ss}.zip",
                DateTime.Now));
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Folder;Favorite;Type;Name;Notes;Fields;LoginUris;LoginUsername;LoginPassword;LoginTotp");

            lstItems.ForEach(x => builder.AppendLine(x.ToCsv()));

            byte[] bufferCsvFile = Encoding.GetEncoding("iso-8859-1").GetBytes(builder.ToString());
            byte[] bufferJsonFile = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(lstItems, Formatting.Indented));

            using (FileStream zipFileStream = File.Create(outputPath))
            {
                using (ZipOutputStream zipOutputStream = new ZipOutputStream(zipFileStream))
                {
                    zipOutputStream.SetLevel(9);
                    zipOutputStream.Password = password;

                    using (MemoryStream csvStream = new MemoryStream(bufferCsvFile))
                    {
                        ZipEntry entry = new ZipEntry(Path.GetFileNameWithoutExtension(outputPath) + ".csv");
                        entry.AESKeySize = 256;
                        entry.Size = bufferCsvFile.Length;

                        zipOutputStream.PutNextEntry(entry);
                        StreamUtils.Copy(csvStream, zipOutputStream, new byte[4096]);
                        zipOutputStream.CloseEntry();

                        csvStream.Close();
                     
                    }

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
