using Ionic.Zip;
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
                    GetVersion();
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
                using (ZipFile downloadedUpdate = ZipFile.Read(streamDownloadedFile))
                {
                    string outputDirectory = Path.GetDirectoryName(_executable);

                    _logWriter.Info($"Extracting to {outputDirectory}");
                    downloadedUpdate.ExtractSelectedEntries("name = " + Path.GetFileName(_executable), string.Empty, outputDirectory);
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


        public void Login(string username, string password)
        {
            string usernamePasswordArguments = string.Empty;

            _logWriter.Info("Logging in..");

            if(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
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
            }
        }

        public void LogoutIfRequired(TimeSpan loginDuration)
        {
            DateTime logoutDate = GetLastLoginTime() + loginDuration;
            if (logoutDate <= DateTime.Now)
            {
                Logout();
            }
        }

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

        private DateTime GetLastLoginTime()
        {
            if (!File.Exists(LAST_LOGIN_FILE)) return DateTime.MinValue;
            return DateTime.Parse(File.ReadAllText(LAST_LOGIN_FILE, Encoding.UTF8).Trim());
        }
        private void SetLastLoginTime()
        {
            File.WriteAllText(LAST_LOGIN_FILE, DateTime.Now.ToString("s"), Encoding.UTF8);
        }
    }
}
