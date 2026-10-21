using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using FileList = Google.Apis.Drive.v3.Data.FileList;

namespace Spreadsheet.Storage;

/// <summary>
/// Реалізація IDriveClient для роботи з реальним Google Drive через DriveService.
/// </summary>
internal sealed class GoogleDriveClient : IDriveClient, IDisposable
{
    private static readonly string[] Scopes = [DriveService.Scope.DriveFile];
    private const string ApplicationName = "SpreadsheetApp";

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private DriveService? _driveService;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _driveService?.Dispose();
        _initLock.Dispose();
    }

    private async Task<DriveService> GetServiceAsync(CancellationToken cancellationToken)
    {
        if (_driveService is not null)
        {
            return _driveService;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_driveService is not null)
            {
                return _driveService;
            }

            UserCredential credential = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });

            return _driveService;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<UserCredential> AuthorizeAsync(CancellationToken cancellationToken)
    {
        string secretPath = GoogleClientSecretLocator.FindSecretPath();
        string tokenDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpreadsheetApp",
            "tokens");

        var dataStore = new FileDataStore(tokenDir, fullPath: true);

        return await AuthorizeWithRetryAsync(secretPath, dataStore, retryOnInvalidGrant: true, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<UserCredential> AuthorizeWithRetryAsync(
        string secretPath,
        FileDataStore dataStore,
        bool retryOnInvalidGrant,
        CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromMinutes(2));

        try
        {
            await using FileStream stream = File.OpenRead(secretPath);
            GoogleClientSecrets secrets = GoogleClientSecrets.FromStream(stream);

            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets.Secrets,
                Scopes,
                "user",
                linkedCts.Token,
                dataStore).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new StorageException(DriveErrorMessages.AuthTimeout);
        }
        catch (TokenResponseException ex)
        {
            if (string.Equals(ex.Error?.Error, "access_denied", StringComparison.OrdinalIgnoreCase) ||
                (ex.Error?.ErrorDescription?.Contains("denied", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                throw new StorageException(DriveErrorMessages.UserCancelledAuth, ex);
            }

            if (retryOnInvalidGrant &&
                string.Equals(ex.Error?.Error, "invalid_grant", StringComparison.OrdinalIgnoreCase))
            {
                await dataStore.ClearAsync().ConfigureAwait(false);
                return await AuthorizeWithRetryAsync(secretPath, dataStore, retryOnInvalidGrant: false, cancellationToken)
                    .ConfigureAwait(false);
            }

            throw new StorageException(
                string.Format(CultureInfo.InvariantCulture, DriveErrorMessages.GeneralDriveError, ex.Message),
                ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not StorageException)
        {
            throw MapException(ex);
        }
    }

    public async Task<IReadOnlyList<DriveFileInfo>> ListJsonFilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            DriveService service = await GetServiceAsync(cancellationToken).ConfigureAwait(false);
            var listRequest = service.Files.List();
            listRequest.Fields = "files(id, name, modifiedTime)";
            listRequest.OrderBy = "modifiedTime desc";
            listRequest.Q = "trashed = false and (mimeType = 'application/json' or name contains '.json')";
            listRequest.PageSize = 100;

            FileList result = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            var items = new List<DriveFileInfo>();
            if (result.Files is not null)
            {
                foreach (DriveFile file in result.Files)
                {
                    if (!string.IsNullOrEmpty(file.Id) && !string.IsNullOrEmpty(file.Name))
                    {
                        items.Add(new DriveFileInfo(file.Id, file.Name, file.ModifiedTimeDateTimeOffset));
                    }
                }
            }

            return items;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not StorageException)
        {
            throw MapException(ex);
        }
    }

    public async Task<string> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            throw new StorageException(DriveErrorMessages.InvalidDriveLocation);
        }

        try
        {
            DriveService service = await GetServiceAsync(cancellationToken).ConfigureAwait(false);
            var getRequest = service.Files.Get(fileId);
            using var stream = new MemoryStream();

            IDownloadProgress progress = await getRequest.DownloadAsync(stream, cancellationToken).ConfigureAwait(false);
            if (progress.Status == DownloadStatus.Failed)
            {
                if (progress.Exception is not null)
                {
                    throw MapException(progress.Exception);
                }

                throw new StorageException(DriveErrorMessages.DownloadFailed);
            }

            byte[] bytes = stream.ToArray();
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not StorageException)
        {
            throw MapException(ex);
        }
    }

    public async Task<string> CreateFileAsync(string name, string jsonContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new StorageException(DriveErrorMessages.InvalidFileName);
        }

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            throw new StorageException("Вміст файлу для створення порожній");
        }

        try
        {
            DriveService service = await GetServiceAsync(cancellationToken).ConfigureAwait(false);
            var metadata = new DriveFile
            {
                Name = name,
                MimeType = "application/json"
            };

            byte[] bytes = Encoding.UTF8.GetBytes(jsonContent);
            using var stream = new MemoryStream(bytes);
            stream.Position = 0;
            var createRequest = service.Files.Create(metadata, stream, "application/json");
            createRequest.Fields = "id, name";

            IUploadProgress progress = await createRequest.UploadAsync(cancellationToken).ConfigureAwait(false);
            if (progress.Status == UploadStatus.Failed)
            {
                if (progress.Exception is not null)
                {
                    throw MapException(progress.Exception);
                }

                throw new StorageException(DriveErrorMessages.CreateFailed);
            }

            if (createRequest.ResponseBody is null || string.IsNullOrWhiteSpace(createRequest.ResponseBody.Id))
            {
                throw new StorageException(DriveErrorMessages.CreateFailed);
            }

            return createRequest.ResponseBody.Id;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not StorageException)
        {
            throw MapException(ex);
        }
    }

    public async Task UpdateFileAsync(string fileId, string jsonContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            throw new StorageException(DriveErrorMessages.InvalidDriveLocation);
        }

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            throw new StorageException("Вміст файлу для оновлення порожній");
        }

        try
        {
            DriveService service = await GetServiceAsync(cancellationToken).ConfigureAwait(false);
            var metadata = new DriveFile();
            byte[] bytes = Encoding.UTF8.GetBytes(jsonContent);
            using var stream = new MemoryStream(bytes);
            stream.Position = 0;
            var updateRequest = service.Files.Update(metadata, fileId, stream, "application/json");

            IUploadProgress progress = await updateRequest.UploadAsync(cancellationToken).ConfigureAwait(false);
            if (progress.Status == UploadStatus.Failed)
            {
                if (progress.Exception is not null)
                {
                    throw MapException(progress.Exception);
                }

                throw new StorageException(DriveErrorMessages.UploadFailed);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not StorageException)
        {
            throw MapException(ex);
        }
    }

    private static Exception MapException(Exception ex)
    {
        if (ex is GoogleApiException apiEx)
        {
            return apiEx.HttpStatusCode switch
            {
                HttpStatusCode.Unauthorized => new StorageException(DriveErrorMessages.Unauthorized, apiEx),
                HttpStatusCode.Forbidden => new StorageException(DriveErrorMessages.Forbidden, apiEx),
                HttpStatusCode.NotFound => new StorageException(DriveErrorMessages.NotFound, apiEx),
                (HttpStatusCode)429 => new StorageException(DriveErrorMessages.RateLimitExceeded, apiEx),
                _ => new StorageException(
                    string.Format(CultureInfo.InvariantCulture, DriveErrorMessages.GeneralDriveError, apiEx.Message),
                    apiEx)
            };
        }

        if (ex is HttpRequestException httpEx)
        {
            return new StorageException(DriveErrorMessages.NetworkError, httpEx);
        }

        return new StorageException(
            string.Format(CultureInfo.InvariantCulture, DriveErrorMessages.GeneralDriveError, ex.Message),
            ex);
    }
}
