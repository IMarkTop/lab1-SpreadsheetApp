namespace Spreadsheet.ViewModels.Text;

/// <summary>Усі тексти інтерфейсу українською (у XAML доступні через <c>x:Static</c>).</summary>
public static class UiText
{
    // Вікно та меню
    public const string AppTitle = "Електронна таблиця";
    public const string DefaultDocumentName = "Без назви";
    public const string MenuFile = "_Файл";
    public const string MenuNew = "_Створити";
    public const string MenuOpen = "_Відкрити…";
    public const string MenuOpenFromDrive = "Відкрити з Google _Drive…";
    public const string MenuSave = "_Зберегти";
    public const string MenuSaveAs = "Зберегти _як…";
    public const string MenuSaveToDrive = "Зберегти на Google _Drive…";
    public const string MenuExit = "_Вихід";
    public const string MenuTable = "_Таблиця";
    public const string MenuInsertRowAbove = "Додати рядок _вище";
    public const string MenuInsertRowBelow = "Додати рядок _нижче";
    public const string MenuDeleteRow = "Видалити _рядок";
    public const string MenuInsertColumnLeft = "Додати стовпець _ліворуч";
    public const string MenuInsertColumnRight = "Додати стовпець _праворуч";
    public const string MenuDeleteColumn = "Видалити _стовпець";
    public const string MenuHelp = "_Довідка";
    public const string MenuHelpItem = "_Довідка";
    public const string MenuAbout = "Про _програму";
    public const string ShortcutNew = "Ctrl+N";
    public const string ShortcutOpen = "Ctrl+O";
    public const string ShortcutSave = "Ctrl+S";
    public const string ShortcutSaveAs = "Ctrl+Shift+S";
    public const string ShortcutHelp = "F1";

    // Панель інструментів і рядок формул
    public const string ModeExpression = "ВИРАЗ";
    public const string ModeValue = "ЗНАЧЕННЯ";
    public const string ModeToggleToolTip = "Перемкнути режим відображення: ВИРАЗ / ЗНАЧЕННЯ";
    public const string FormulaBarWatermark = "Введіть число, true/false або вираз";
    public const string AddressBoxToolTip = "Адреса виділеної клітинки";

    // Відображення клітинок
    public const string ErrorValue = "#ПОМИЛКА";
    public const string TrueText = "true";
    public const string FalseText = "false";

    // Формат повідомлення про помилку: {0} — позиція (з 1), {1} — довжина, {2} — текст.
    public const string ErrorTemplate = "Помилка (позиція {0}, довжина {1}): {2}";

    // Формат фрагмента тексту: {0} — фрагмент.
    public const string FragmentTemplate = "Фрагмент: «{0}»";

    // Підтвердження: {0} — номер рядка або літери стовпця.
    public const string ConfirmNew = "Поточну таблицю буде замінено новою, незбережені дані буде втрачено. Продовжити?";
    public const string ConfirmDeleteRow = "Рядок {0} містить дані. Видалити його?";
    public const string ConfirmDeleteColumn = "Стовпець {0} містить дані. Видалити його?";
    public const string ConfirmSaveChanges = "Зберегти зміни у файлі \"{0}\" перед закриттям?";

    // Рядок стану
    public const string StatusReady = "Готово";
    public const string StatusNewCreated = "Створено нову порожню таблицю";
    public const string StatusCellUpdated = "Клітинку {0} оновлено";
    public const string StatusCellCleared = "Клітинку {0} очищено";
    public const string StatusRowInserted = "Додано рядок {0}";
    public const string StatusRowDeleted = "Видалено рядок {0}";
    public const string StatusColumnInserted = "Додано стовпець {0}";
    public const string StatusColumnDeleted = "Видалено стовпець {0}";
    public const string StatusModeExpression = "Режим: ВИРАЗ";
    public const string StatusModeValue = "Режим: ЗНАЧЕННЯ";
    public const string StatusSelectCell = "Спочатку виділіть клітинку";
    public const string StatusCellOutOfTable = "Клітинка лежить поза межами таблиці";
    public const string StatusLastRow = "Не можна видалити останній рядок";
    public const string StatusLastColumn = "Не можна видалити останній стовпець";
    public const string StatusMaxRows = "Досягнуто максимальної кількості рядків ({0})";
    public const string StatusMaxColumns = "Досягнуто максимальної кількості стовпців ({0})";
    public const string StatusRowFailed = "Не вдалося змінити кількість рядків";
    public const string StatusColumnFailed = "Не вдалося змінити кількість стовпців";
    public const string StatusFileSaved = "Таблицю збережено у {0}";
    public const string StatusFileLoaded = "Таблицю завантажено з {0}";
    public const string StatusSaveCancelled = "Збереження скасовано";
    public const string StatusOpenCancelled = "Відкриття скасовано";

    // Діалоги
    public const string ErrorDialogTitle = "Помилка";
    public const string ConfirmDialogTitle = "Підтвердження";
    public const string SaveChangesTitle = "Незбережені зміни";
    public const string SaveDialogTitle = "Зберегти таблицю";
    public const string OpenDialogTitle = "Відкрити таблицю";
    public const string FileTypeFilterJson = "Файли таблиць (*.json)";
    public const string ErrorInvalidPath = "Не вдалося отримати локальний шлях до вибраного файлу.";
    public const string OkButton = "OK";
    public const string YesButton = "Так";
    public const string NoButton = "Ні";
    public const string SaveButton = "Зберегти";
    public const string DiscardButton = "Не зберігати";
    public const string CancelButton = "Скасувати";
    public const string CloseButton = "Закрити";

    // Довідка
    public const string HelpTitle = "Довідка";

    public const string HelpText = """
        ЕЛЕКТРОННА ТАБЛИЦЯ — ДОВІДКА

        Клітинка містить число, логічне значення (true / false) або вираз. Вираз може
        містити числа, імена клітинок, круглі дужки та операції. Пробіли та табуляції
        ігноруються, регістр літер не має значення (a1 і A1 — те саме, AND і and — те саме).

        ЧИСЛА
        Цілі числа довільної довжини, наприклад 99999999999999999999.

        ПОСИЛАННЯ НА КЛІТИНКИ
        Як в Excel: літери стовпця й номер рядка, наприклад A3, B12, AA10.
        Стовпці: A, B, …, Z, AA, …, ZZ; рядки нумеруються з 1.

        ОПЕРАЦІЇ (від найнижчого пріоритету до найвищого)
        1. or — логічне «або»
        2. and — логічне «і»
        3. not — логічне заперечення
        4. =  <  > — порівняння (не можна записувати ланцюжком: 1 < 2 < 3 — помилка)
        5. +  - — додавання й віднімання
        6. *  /  div  mod — множення, точне ділення, цілочисельне ділення, остача

        Операції одного пріоритету виконуються зліва направо. Дужки змінюють порядок.
        div відкидає дробову частину до нуля; mod має знак діленого.
        Операція / — точне ділення: якщо остача не нуль, це помилка.
        Операції = працює з двома числами або з двома логічними значеннями.
        and та or завжди обчислюють обидва операнди.

        ПРИКЛАДИ
        5
        true
        A1 + B1 * 2
        A1 > 5 and not (C1 = 3)
        (A1 + 7) mod 3 = 1

        РЕЖИМИ
        ВИРАЗ — у клітинках показується введений текст.
        ЗНАЧЕННЯ — у клітинках показується обчислене значення (число або true/false).
        Рядок формул завжди показує введений текст виділеної клітинки.
        Enter у рядку формул зберігає зміни, Esc — скасовує. Delete очищає клітинку.

        ПОМИЛКИ
        Помилки показуються червоним; у режимі ЗНАЧЕННЯ — як #ПОМИЛКА. Повне
        повідомлення з позицією (рахується з 1) видно в підказці клітинки та під рядком формул.
        — Синтаксична помилка: неправильно записаний вираз.
        — Помилка типів: наприклад, true + 1, 1 and true, not 5, 1 = true.
        — Ділення на нуль: /, div або mod на нуль.
        — Неточне ділення: 7 / 2.
        — Порожня клітинка: у виразі є посилання на порожню клітинку.
        — Клітинка з помилкою: у виразі є посилання на клітинку, що містить помилку.
        — Циклічне посилання: клітинки посилаються одна на одну.
        — Посилання поза таблицею: клітинка лежить за межами таблиці.

        ЗМІНА РОЗМІРУ
        Рядки й стовпці додаються та видаляються через меню «Таблиця». Розмір: від 1×1 до
        1000 рядків і 702 стовпців. Клітинки зсуваються разом із текстом; тексти формул
        автоматично не переписуються (A3 лишається A3), після зміни розміру таблиця
        перераховується.
        """;

    // Про програму
    public const string AboutTitle = "Про програму";
    public const string AboutCourse = "Лабораторна робота № 1, варіант 31";
    public const string AboutAuthor = "Автор: Борода Марко Костянтинович, група К-28";
    public const string AboutDescription =
        "Електронна таблиця з обчисленням виразів у клітинках: цілі числа довільної довжини, "
        + "логічні значення, посилання на клітинки, залежності, виявлення циклів, "
        + "режими ВИРАЗ / ЗНАЧЕННЯ та локалізація помилок.";
    public const string AboutVersionLabel = "Версія";

    // Діалоги Google Drive
    public const string DrivePickerTitle = "Відкрити з Google Drive";
    public const string DriveSaveTitle = "Зберегти на Google Drive";
    public const string DriveSavePrompt = "Введіть назву файлу:";
    public const string DriveLoading = "Завантаження списку файлів…";
    public const string DriveNoFiles = "На Google Drive не знайдено файлів таблиць (.json)";
    public const string DriveOpenButton = "Відкрити";
    public const string DriveSaveButton = "Зберегти";
    public const string DriveCancelButton = "Скасувати";
    public const string DriveRetryButton = "Повторити";
}
