# VersionOne Provisioning for LDAP

VersionOne Provisioning for LDAP keeps the list of active VersionOne members synchronized with users that belong to an LDAP group. For more about using this product, see the [User Documentation](http://versionone.github.io/VersionOne.Provisioning.LDAP/).

## Build Prerequisites

### Install or upgrade to the latest NuGet

We use NuGet to manage external dependencies. [Install NuGet 2.7](http://docs.nuget.org/docs/release-notes/nuget-2.7) or greater.

### Developer Tools

We code with Visual Studio 2012 Professional and use git-bash for automation.

Since we work on many projects that may have different tool chains, we automate the installation of our developer tools so our tools are listed in the Chocolatey [packages.config](packages.config) file. If you do not already have these tools, you can use Chocolatey to obtain them. Not familiar with [Chocolatey](http://chocolatey.org/)? It's a package manager for Windows, similar to apt-get in the Linux world. To install Chocolatey:

* First, see [Chocolatey's requirements](https://github.com/chocolatey/chocolatey/wiki)
* Next, assuming you already Cloned or Downloaded this repository from GitHub into `C:\Projects\VersionOne.Provisioning.LDAP`, open an `Admininstrator` command prompt in that folder and run the following:
    @powershell -NoProfile -ExecutionPolicy unrestricted -Command "iex ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))" && SET PATH=%PATH%;%systemdrive%\chocolatey\bin

If the Chocolatey install worked, then:

* **First:** open up [packages.config](packages.config) and remove any entries for developer tools you already have installed. This is expecially important for Visual Studio to help avoid downloading its large package over the internet.
* Close the command prompt and open a new `Administrator` command prompt so that you get an updated PATH environment variable and navigate back to the repository folder.
* Run `cinst packages.config`.

This should start downloading and automatically installing the tools listed in [packages.config](packages.config).

#### Alternatively: If you don't want or cannot use Chocolatey, you can manually install developer tools

* [Install Visual Studio 2012 Professional or higher](http://msdn.microsoft.com/en-US/library/vstudio/e2h7fzkw.aspx)
* [Install Update 3 for Visual Studio 2012](http://support.microsoft.com/kb/2835600)
* **Install Bash shell** -- our `build.sh` and other scripts are written in Bash, so you need a good Bash shell to execute them. People on the VersionOne team use both [http://git-scm.com/download/win](Git Bash) and 
[Cygwin with the Bash package](http://www.cygwin.com/) successfully.

# How to Build

Assuming you have followed the previous steps and your environment is all setup correctly now:

* Open a Bash prompt as `Administrator`
* Change to the repository directory, for example `/c/Projects/VersionOne.Client.VisualStudio`
* Run `./build.sh`
