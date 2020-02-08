

# Bitwarden Export Backup
## Description
This is a project used to back up parts of a Bitwarden Vault. 

## Installation

 1. This uses DotNet Core 3.1 as framework, so you'll have to install the runtime first [here](https://dotnet.microsoft.com/download).
 2. You'll need to download a version of the Bitwarden CLI executable [here](https://github.com/bitwarden/cli/releases) and place it somewhere accessible.
 3. Download a release or build it yourself (I used Visual Studio 2017 Community with Dotnet-Core Development package).
 4. Open the .config file associated to the Bitwarden_ExportBackup.dll and change the settings accordingly (more detail further down).
 5. Run the backup: `dotnet Bitwarden_ExportBackup.dll`.
## Configuration
**BITWARDEN_CLI**
This is the path where you put the CLI executable downloaded earlier. 

**UPDATE_CLI**
This will automatically fetch updates  when the CLI notices that a newer version is available.
Values: `true` or `false` .

**LOGIN_DURATION**
Determines how long the client will stay logged in before it will be logged out automatically. Once logged out you have to log in again using your Bitwarden credentials and possibly the 2FA code.
A duration of zero will automatically log you out after the backup has completed.
For formatting of this value see the [Microsoft documentation](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings#the-constant-c-format-specifier).

**USERNAME/PASSWORD**
You can enter your Bitwarden credentials here if you want to - they will be forwarded to the CLI for logging in so you don't have to manually enter them every time. Also works for unlocking.
CAUTION: this will leave your credentials exposed in an unencrypted config file on your hard disk - I advise not doing it this way and rather use a longer `LOGIN_DURATION`.

**OUTPUT_METHOD**
Output Method for exporting.
Currently possible Methods:
1. `ZipAes256`
2. `Kdbx`

### ZIPAES256 Configuration
|Key|Description|
|--|--|
|ZIPAES256_EXPORT_DIRECTORY|The directory you want your files to be exported to. Backups are AES256 encrypted ZIP archives, so you'll need a  program that can open these (Windows Explorer can not) like 7Zip or WinRAR|
|ZIPAES256_EXPORT_FORMATS|Formats you want your information to be exported as. Currently only these are available: <br>`BitwardenCsv`, `OnePasswordWinCsv`|
|ZIPAES256_BACKUP_PASSWORD_NAME|Password Key Name for the to-be-used password for encrypting the backup archives. THIS IS NOT THE PASSWORD ITSELF.<br>This tool uses the Bitwarden Vault to access a password set by you in the first run (or every time it can't find this key in the vault). Your Backup password will not be saved on your system. It will be created in your Bitwarden Vault with the name you specified here. <br>Example: leave the value unchanged and run the tool once - after that look up "Bitwarden ExportBackup" in your Vault - there should be an entry containing the password you selected during the backup process|
|ZIPAES256_DELETE_OLD_FILES|Wether you want to delete old archives or not. `true` or `false`|
|ZIPAES256_KEEP_FILES_AMOUNT|If you do want to delete old archives via `DELETE_OLD_FILES` this is the amount of files that will be kept|

### KDBX Configuration
*Not available at the moment.*

# Disclaimers
This is by no means an audited or in any other form professionally checked tool for backing up some of your most important data. **Use with caution and at your own risk** - i'm not a security specialist or anything, i just wanted my data backed up locally in case anything happens that makes the vault inaccessible either temporarily or permanently.

Also there is currently no way to back up attachments.
