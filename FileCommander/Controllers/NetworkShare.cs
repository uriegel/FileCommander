using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClrWinApi;

using FileCommander.Exceptions;

using WinRT.Interop;

namespace FileCommander.Controllers;

static class NetworkShare
{
    /// <summary>
    /// Executes a normal file-system operation. If access to a UNC share
    /// fails because credentials are required, the native Windows
    /// credential dialog is displayed and the operation is retried.
    /// </summary>
    public static async Task<T> ExecuteAsync<T>(string path, Func<T> operation, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(operation);

            cancellationToken.ThrowIfCancellationRequested();

            // First attempt: completely normal .NET file-system operation.
            try
            {
                return operation();
            }
            catch (Exception ex) when (IsAuthenticationFailure(path, ex))
            {
                // Continue below and ask Windows for credentials.
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Only UNC paths can be handled here.
            if (!TryGetShareRoot(path, out var share))
                throw new UnauthorizedAccessException($"Access to '{path}' was denied.");

            // Display the native Windows credential dialog.
            var res = await Task.Run(() => PromptForCredentials(share));
            if (res != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Establish the SMB connection.
                Connect(share, res.Username, res.Password, res.Save);
            }
            else
                // User pressed Cancel.
                throw new OperationCanceledException(cancellationToken);
        }
    }


    // --------------------------------------------------------------------
    // Authentication failure detection
    // --------------------------------------------------------------------
    static bool IsAuthenticationFailure(string path, Exception exception)
    {
        if (!IsUncPath(path))
            return false;

        /*
            * UnauthorizedAccessException is what .NET normally gives us
            * for an SMB access/authentication problem.
            *
            * IOException is also included because some Win32 network errors
            * are surfaced by .NET as IOException.
            */
        if (exception is UnauthorizedAccessException)
            return true;

        if (exception is IOException ioException)
        {
            int error = GetWin32Error(ioException);

            return error switch
            {
                ERROR_ACCESS_DENIED => true,
                ERROR_LOGON_FAILURE => true,
                ERROR_INVALID_PASSWORD => true,
                ERROR_ACCOUNT_RESTRICTION => true,
                ERROR_PASSWORD_EXPIRED => true,
                ERROR_ACCOUNT_DISABLED => true,
                ERROR_ACCOUNT_LOCKED_OUT => true,
                _ => false
            };
        }

        return false;
    }

    static int GetWin32Error(Exception exception)
    {
        if (exception.HResult != 0)
            return exception.HResult & 0xFFFF;

        return 0;
    }


    // --------------------------------------------------------------------
    // SMB connection
    // --------------------------------------------------------------------
    static void Connect(string share, string username, string password, bool save)
    {
        var resource = new NetResource
        {
            Scope = ResourceScope.GlobalNetwork,
            ResourceType = ResourceType.Disk,
            DisplayType = ResourceDisplaytype.Share,
            RemoteName = share
        };

        var flags = save
            ? (AddConnectionFlags)0
            : AddConnectionFlags.Temporary;

        int result = Api.WNetAddConnection2(resource, password, username, flags);

        if (result == NO_ERROR || result == ERROR_ALREADY_ASSIGNED)
            return;

        if (result == ERROR_SESSION_CREDENTIAL_CONFLICT)
        {
            throw new CredentialException($"Windows hat bereits eine Verbindung zu '{share}' " +
                "mit unterschiedlicher Anmeldeinformation.", new Win32Exception(result));
        }

        // try with other credential // throw new Win32Exception(result, $"'{share}' konnte nicht verbunden werden.");
    }


    // --------------------------------------------------------------------
    // Windows credential dialog
    // --------------------------------------------------------------------
    static Credential? PromptForCredentials(string target)
    {
        var username = string.Empty;
        var password = string.Empty;
        var save = false;

        var uiInfo = new CredUIInfo
        {
            Size = Marshal.SizeOf<CredUIInfo>(),
            Parent = WindowNative.GetWindowHandle(MainWindow.GetWindow()),
            MessageText = $"Gib' die Anmeldeinformation ein, um {target} zu verbinden",
            CaptionText = "File Commander",
            Banner = 0
        };

        uint authPackage = 0;

        nint authBuffer = 0;
        uint authBufferSize = 0;

        bool saveCredentials = true;

        try
        {
            int result = Api.CredUIPromptForWindowsCredentials(
                ref uiInfo,
                0,
                ref authPackage,
                IntPtr.Zero,
                0,
                out authBuffer,
                out authBufferSize,
                ref saveCredentials,
                PromptForWindowsCredentialsFlags.Generic);

            // User pressed Cancel.
            if (result == ERROR_CANCELLED)
                return null;

            if (result != NO_ERROR)
                throw new Win32Exception(result);

            // First call determines the required buffer sizes.
            uint usernameLength = 0;
            uint domainLength = 0;
            uint passwordLength = 0;

            Api.CredUnPackAuthenticationBuffer(
                0,
                authBuffer,
                authBufferSize,
                null,
                ref usernameLength,
                null,
                ref domainLength,
                null,
                ref passwordLength);

            var userBuffer =
                new StringBuilder((int)usernameLength + 1);

            var domainBuffer =
                new StringBuilder((int)domainLength + 1);

            var passwordBuffer =
                new StringBuilder((int)passwordLength + 1);

            if (!Api.CredUnPackAuthenticationBuffer(
                    0,
                    authBuffer,
                    authBufferSize,
                    userBuffer,
                    ref usernameLength,
                    domainBuffer,
                    ref domainLength,
                    passwordBuffer,
                    ref passwordLength))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            username = userBuffer.ToString();
            password = passwordBuffer.ToString();
            save = saveCredentials;

            if (domainBuffer.Length > 0 &&
                !username.Contains('\\') &&
                !username.Contains('@'))
            {
                username =
                    domainBuffer + "\\" + username;
            }

            return new(username, password, save);
        }
        finally
        {
            if (authBuffer != IntPtr.Zero)
                Marshal.FreeCoTaskMem(authBuffer);
        }
    }


    // --------------------------------------------------------------------
    // UNC path handling
    // --------------------------------------------------------------------
    static bool IsUncPath(string path) => path.StartsWith(@"\\", StringComparison.Ordinal);

    /// <summary>
    /// Converts:
    ///
    ///     \\server\share\directory\file.txt
    ///
    /// into:
    ///
    ///     \\server\share
    /// </summary>
    static bool TryGetShareRoot(string path, out string share)
    {
        share = string.Empty;

        if (!IsUncPath(path))
            return false;

        string remaining = path[2..];

        int serverEnd = remaining.IndexOf('\\');

        if (serverEnd <= 0)
            return false;

        int shareEnd =
            remaining.IndexOf('\\', serverEnd + 1);

        if (shareEnd < 0)
            shareEnd = remaining.Length;

        if (shareEnd <= serverEnd + 1)
            return false;

        string server =
            remaining[..serverEnd];

        string shareName =
            remaining[(serverEnd + 1)..shareEnd];

        share = $@"\\{server}\{shareName}";

        return true;
    }

    const int NO_ERROR = 0;
    const int ERROR_ACCESS_DENIED = 5;
    const int ERROR_ALREADY_ASSIGNED = 85;
    const int ERROR_INVALID_PASSWORD = 86;
    const int ERROR_CANCELLED = 1223;
    const int ERROR_LOGON_FAILURE = 1326;
    const int ERROR_ACCOUNT_RESTRICTION = 1327;
    const int ERROR_PASSWORD_EXPIRED = 1330;
    const int ERROR_ACCOUNT_DISABLED = 1331;
    const int ERROR_ACCOUNT_LOCKED_OUT = 1909;
    const int ERROR_SESSION_CREDENTIAL_CONFLICT = 1219;
}

record Credential(string Username, string Password, bool Save);