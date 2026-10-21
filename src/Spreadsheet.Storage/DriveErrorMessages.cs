namespace Spreadsheet.Storage;

/// <summary>
/// Тексти помилок підсистеми Google Drive українською мовою.
/// </summary>
internal static class DriveErrorMessages
{
    public const string Unauthorized = "Помилка авторизації Google Drive. Термін дії сесії закінчився або відмовлено в доступі.";
    public const string Forbidden = "Доступ до операції або файлу в Google Drive заборонено.";
    public const string NotFound = "Файл не знайдено на Google Drive.";
    public const string RateLimitExceeded = "Перевищено ліміт запитів до Google Drive. Спробуйте пізніше.";
    public const string NetworkError = "Помилка мережі при взаємодії з Google Drive. Перевірте з'єднання з інтернетом.";
    public const string AuthTimeout = "Час очікування авторизації в Google Drive вичерпано (таймаут 2 хвилини).";
    public const string UserCancelledAuth = "Користувач скасував авторизацію в Google Drive або відхилив запит доступу.";
    public const string SecretNotFound = "Файл конфігурації 'client_secret.json' не знайдено в корені проєкту.";
    public const string InvalidDriveLocation = "Некоректне розташування Google Drive: очікується 'gdrive:<fileId>' або 'gdrive:new:<fileName>'.";
    public const string InvalidFileName = "Некоректне ім'я файлу для створення на Google Drive.";
    public const string DownloadFailed = "Не вдалося завантажити файл з Google Drive.";
    public const string UploadFailed = "Не вдалося передати файл на Google Drive.";
    public const string CreateFailed = "Не вдалося створити новий файл на Google Drive.";
    public const string GeneralDriveError = "Помилка сервісу Google Drive: {0}";
}
