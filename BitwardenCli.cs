using Bitwarden_ExportBackup.Extensions;
using Bitwarden_ExportBackup.Model;
using Bitwarden_ExportBackup.Model.Exceptions;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace Bitwarden_ExportBackup
{
    public class BitwardenCli
    {
        private const string NO_UPDATE_MESSAGE = "No update available.";
        private const string ALREADY_LOGGED_IN_MESSAGE = "You are already logged in as";
        private const string LAST_LOGIN_FILE = "last_login.txt";

        private static readonly log4net.ILog _logWriter = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string _executable;
        private string _version;
        private string _currentSessionKey;

        public BitwardenCli(string executable)
        {
            _executable = executable;
            GetVersion();
        }

        /// <summary>
        /// Checks for update using the cli's 'update' call
        /// </summary>
        public void CheckForUpdates()
        {
            try
            {
                _logWriter.Info("Checking for updates..");

                Process process = new Process();
                process.StartInfo.FileName = _executable;
                process.StartInfo.Arguments = "update --raw";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;

                process.Start();
                process.WaitForExit();

                string processOutput = process.StandardOutput.ReadToEnd();

                _logWriter.Debug($"Output: {processOutput}");

                bool updateAvailable = !processOutput.Trim().Equals(NO_UPDATE_MESSAGE);
                if (updateAvailable)
                {
                    Update(processOutput);
                    CheckForUpdates();
                }
                else
                {
                    _logWriter.Info("No update available");
                }
            }
            catch (Exception ex)
            {
                _logWriter.Error("Error updating", ex);
            }
        }


        /// <summary>
        /// Updates the cli executable to the latest version while creating a backup of the old one
        /// </summary>
        /// <param name="downloadPath">Downloadpath of the new version provided by the cli 'update' call</param>
        public void Update(string downloadPath)
        {
            _logWriter.Info("Updating CLI...");

            string oldVersionDestFilePath = Path.Combine(Path.GetDirectoryName(_executable), Path.GetFileNameWithoutExtension(_executable) + "_" + _version);
            File.Move(_executable, oldVersionDestFilePath);

            _logWriter.Info($"Moved {_executable} to {oldVersionDestFilePath}");

            _logWriter.Info($"Downloading latest release from {downloadPath}");

            WebClient webClientUpdateDownload = new WebClient();
            byte[] fileContent = webClientUpdateDownload.DownloadData(downloadPath);

            using (MemoryStream streamDownloadedFile = new MemoryStream(fileContent))
            {
                using (ZipFile downloadedUpdate = new ZipFile(streamDownloadedFile))
                {
                    string outputDirectory = Path.GetDirectoryName(_executable);

                    _logWriter.Info($"Extracting to {outputDirectory}");
                    ZipEntry entry = downloadedUpdate.GetEntry(Path.GetFileName(_executable));

                    using (Stream newExecutable = downloadedUpdate.GetInputStream(entry))
                    {
                        byte[] executableBuffer = new byte[entry.Size];
                        newExecutable.Read(executableBuffer, 0, executableBuffer.Length);
                        File.WriteAllBytes(_executable, executableBuffer);

                        newExecutable.Close();
                    }

                    _logWriter.Info("Extraction completed");
                }
            }
        }

        /// <summary>
        /// Updates the version of the currently existing cli executable
        /// </summary>
        public void GetVersion()
        {
            _logWriter.Info("Checking CLI Version..");

            Process process = new Process();
            process.StartInfo.FileName = _executable;
            process.StartInfo.Arguments = "--version";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();
            process.WaitForExit();

            string processOutput = process.StandardOutput.ReadToEnd().Trim();

            _logWriter.Debug($"Output: {processOutput}");

            _version = processOutput;
        }



        /// <summary>
        /// Updates the version of the currently existing cli executable
        /// </summary>
        public void Sync()
        {
            _logWriter.Info("Synchronizing Vault..");

            if(string.IsNullOrEmpty(_currentSessionKey))
            {
                throw new BitwardenSessionEmptyException(this);
            }

            Process process = new Process();
            process.StartInfo.FileName = _executable;
            process.StartInfo.Arguments = $"sync --session {_currentSessionKey}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();
            process.WaitForExit();

            string processOutput = process.StandardOutput.ReadToEnd().Trim();

            _logWriter.Debug($"Output: {processOutput}");
        }

        /// <summary>
        /// Returns a List of objects depending on the type supplied
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<BitwardenObject> GetObjects(string type)
        {
            List<BitwardenObject> lstObjects = new List<BitwardenObject>();

            _logWriter.Info($"Retrieving objects [{type}]..");

            if (string.IsNullOrEmpty(_currentSessionKey))
            {
                throw new BitwardenSessionEmptyException(this);
            }

            Process process = new Process();
            process.StartInfo.FileName = _executable;
            process.StartInfo.Arguments = $"list {type} --session {_currentSessionKey}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();

            string processOutput = process.StandardOutput.ReadToEnd().Trim();
            string processError = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(processOutput))
            {
                byte[] encodedOutput = Console.OutputEncoding.GetBytes(processOutput);
                lstObjects = JsonConvert.DeserializeObject<List<BitwardenObject>>(Encoding.UTF8.GetString(encodedOutput));
            }
            if (!string.IsNullOrWhiteSpace(processError))
            {
                _logWriter.Warn($"StdError: {processError}");
            }

            return lstObjects;
        }

        /// <summary>
        /// Logs in to Bitwarden either via CLI (if no credentials are passed) or directly via parameter
        /// </summary>
        /// <param name="username">Bitwarden username - leave empty for cli handling</param>
        /// <param name="password">Bitwarden password - leave empty for cli handling</param>
        public bool Login(string username, string password)
        {
            bool retVal = false;
            string usernamePasswordArguments = string.Empty;

            _logWriter.Info("Logging in..");

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                usernamePasswordArguments = $"\"{username}\" \"{password}\"";
            }

            Process process = new Process();
            process.StartInfo.FileName = _executable;
            process.StartInfo.Arguments = $"login {usernamePasswordArguments} --raw";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();
            process.WaitForExit();

            string processOutput = process.StandardOutput.ReadToEnd().Trim();
            _logWriter.Debug($"Output: {processOutput}");

            bool wasAlreadyLoggedIn = processOutput.Trim().StartsWith(ALREADY_LOGGED_IN_MESSAGE);

            if (!wasAlreadyLoggedIn)
            {
                SetLastLoginTime();
                _currentSessionKey = processOutput;
                retVal = process.ExitCode == 0;

            }
            else
            {
                _logWriter.Info("Unlock is required after login..");
                retVal = Unlock(password);
            }

            return retVal;
        }


        /// <summary>
        /// Unlocks the vault 
        /// is used in case the cli was already logged in and no session key was provided
        /// </summary>
        /// <param name="password"></param>
        private bool Unlock(string password)
        {
            bool retVal = false;
            string passwordArgument = string.Empty;
            if (!string.IsNullOrWhiteSpace(password))
            {
                passwordArgument = $"\"{password}\"";
            }

            Process process = new Process();
            process.StartInfo.FileName = _executable;
            process.StartInfo.Arguments = $"unlock {passwordArgument} --raw";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();
            process.WaitForExit();

            string processOutput = process.StandardOutput.ReadToEnd().Trim();
            //string processError = process.StandardError.ReadToEnd().Trim();

            _logWriter.Debug($"Output: {processOutput}");
            _currentSessionKey = processOutput;
            retVal = process.ExitCode == 0;

            return retVal;
        }


        /// <summary>
        /// Logs the user out if the maximum login duration was surpassed
        /// </summary>
        /// <param name="loginDuration">Duration the user may be logged in</param>
        public void LogoutIfRequired(TimeSpan loginDuration)
        {
            DateTime logoutDate = GetLastLoginTime() + loginDuration;
            if (logoutDate <= DateTime.Now)
            {
                _logWriter.Info("Logout is required");
                Logout();
            }
        }

        /// <summary>
        /// Logs the user out
        /// </summary>
        private void Logout()
        {
            _logWriter.Info("Logging out..");

            Process process = new Process();
            process.StartInfo.FileName = _executable;
            process.StartInfo.Arguments += $"logout";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            process.WaitForExit();

            string processOutput = process.StandardOutput.ReadToEnd().Trim();
            _logWriter.Debug($"Output: {processOutput}");
        }

        /// <summary>
        /// Returns a template for creating a new item object
        /// </summary>
        /// <returns></returns>
        public BitwardenItemTemplate GetItemTemplate()
        {
            if (string.IsNullOrEmpty(_currentSessionKey))
            {
                throw new BitwardenSessionEmptyException(this);
            }

            Process process = new Process();
            process.StartInfo.FileName = _executable;
            process.StartInfo.Arguments += $"get template item --session {_currentSessionKey}";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            process.WaitForExit();

            string processOutput = process.StandardOutput.ReadToEnd().Trim();
            _logWriter.Debug($"Output: {processOutput}");

            return JsonConvert.DeserializeObject< BitwardenItemTemplate>(processOutput);
        }

        /// <summary>
        /// Creates a new item within the Bitwarden Vault
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool CreateItem(BitwardenItemTemplate item)
        {
            bool retVal = false;

            _logWriter.Info($"Creating Item [{item.Name}]");

            if (string.IsNullOrEmpty(_currentSessionKey))
            {
                throw new BitwardenSessionEmptyException(this);
            }

            string itemSerialized = JsonConvert.SerializeObject(item).ToBase64(Encoding.Default);

            Process process = new Process();
            process.StartInfo.FileName = _executable;
            process.StartInfo.Arguments += $"create item {itemSerialized} --session {_currentSessionKey}";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            process.WaitForExit();

            string processOutput = process.StandardOutput.ReadToEnd().Trim();
            _logWriter.Debug($"Output: {processOutput}");

            retVal = process.ExitCode == 0;

            return retVal;
        }

        private DateTime GetLastLoginTime()
        {
            if (!File.Exists(LAST_LOGIN_FILE)) return DateTime.MinValue;
            return DateTime.Parse(File.ReadAllText(LAST_LOGIN_FILE, Encoding.UTF8).Trim());
        }
        private void SetLastLoginTime()
        {
            File.WriteAllText(LAST_LOGIN_FILE, DateTime.Now.ToString("s"), Encoding.UTF8);
        }

        public override string ToString()
        {
            return _executable + " " + _version;
        }
    }
}
