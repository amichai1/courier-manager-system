# dotNet5786_4661
# 📦 Wolt Delivery System – Admin & Courier Dashboard

[![Platform](https://img.shields.io/badge/platform-Windows%20WPF-blue)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/architecture-3--Tier%20(DAL%2FBL%2FPL)-orange)]()
[![Status](https://img.shields.io/badge/status-Educational%20Project-success)]()

<div dir="rtl">

מערכת ניהול משלוחים בסגנון “Wolt” הכוללת ממשק אדמין וממשק שליחים,
בנויה בשכבות DAL / BL / PL עם סימולטור זמן אסינכרוני.

---

## 🇮🇱 תיאור כללי (עברית)

### 🧱 שכבת DAL (Data Access Layer)

- **שדה סיסמה ביישויות הנתונים (Courier & Config)**  
  - לשכבת הנתונים יש שדה `Password` לישויות השליחים (`Courier`) וכן שדה `ManagerPassword` בקונפיגורציה.  

- **Parsing בטוח בקונסולה (BlTest / DalTest)**  
  - בפרויקטי הבדיקה (`BlTest`, `DalTest`) נעשה שימוש נרחב ב-`TryParse` (`int.TryParse`, `double.TryParse`, `Enum.TryParse` וכו') יחד עם בדיקת ערך החזרה.  
  - מונע קריסות בקלט מהמשתמש ומאפשר חוויית בדיקה יציבה יותר.

---

### 🧠 שכבת BL (Business Logic Layer)

- **ניהול סיסמאות – בדיקת חוזק (Password Strength Validation)**  
  - מחלקת עזר ייעודית מטפלת במדיניות סיסמאות חזקה (אורך מינימלי, אותיות גדולות/קטנות, ספרות ותווים מיוחדים).  
  - לפני עדכון סיסמת שליח, מתבצעת בדיקה מפורשת שהסיסמה עומדת בכללי האבטחה.

- **ניהול סיסמאות – סיסמא ראשונית (Initial password)**  
    - ביצירת שליח, ניתנת סיסמא ראשונית אוטומטית חזקה. לאחר מכן השליח יכול לעדכן אותה כרצונו.

- **חישוב שכר שליחים (Salary Calculation) 💰**  
  - קיימת פונקציה עסקית מלאה לחישוב שכר שליח בתקופת זמן נתונה.  
  - החישוב מתבסס על:  
    - **מספר המשלוחים שבוצעו בפועל** (כולל הבחנה בין בזמן / באיחור).  
    - **המרחק הכולל** שנצבר (ע״י חישוב מרחק מהחברה ליעד לכל משלוח).  
    - **סוג השליח** (רכב / אופנוע / אופניים / רגלי) שמכתיב שכר בסיס ובונוסים שונים.  
  - התוצאה נאגרת לאובייקט `CourierSalary` ומוצגת למנהל בממשק ה-UI.

---

### 🎨 שכבת PL (Presentation Layer – WPF UI)

#### ✅ ולידציה וחוויית משתמש

- **ולידציה חזותית (Input Validation with Visual Feedback)**  
  - `TextBox`‑ים משתמשים ב־`Validation.ErrorTemplate` ובטריגרים של `Validation.HasError` כדי להציג:  
    - מסגרת אדומה ורקע בהיר לשגיאה.  
    - אייקון “!” קטן עם `ToolTip` של הודעת השגיאה.  
  - נותן למשתמש פידבק מיידי וברור על שדות שגויים.

- **Converters ו־MultiValueConverters**  
  - שימוש נרחב ב־`IValueConverter` ו־`IMultiValueConverter` לצורך:  
    - עיצוב מספרים (משקל, נפח, מרחק).  
    - ולידציה לוגית (טלפון, אימייל, ת״ז וכו') לפני שמירה.  
    - בחירת צבעים / אייקונים לפי סטטוס הזמנה או סוג משלוח.  
    - בניית טקסטים מורכבים (כגון “כתובת + קואורדינטות” או “שליח (סוג משלוח)”).

- **טריגרים (Triggers) – Property, Data ו-Event**  
  - שימוש ב־`EventTrigger` לאנימציות Hover על כרטיסיות ו־Buttons.  
  - `DataTrigger`‑ים מדגישים כרטיסיות לפי מספר הזמנות פתוחות / בסיכון.  
  - טריגרים בתבניות בקרה משנים צבעים/מסגרות במצב Hover, Focus או שגיאה.

- **ControlTemplate מותאם אישית**  
  - `TextBox`, `PasswordBox`, `Button` ועוד – עם `ControlTemplate`ים מלאים ליצירת UI מודרני (פינות מעוגלות, אפקטי Shadow, אנימציות קליק).  
  - לחצני מחיקה ופעולה עוצבו עם תבניות ייעודיות המפרידות בין מצבי Hover/Pressed.

- **גרפיקה וצורות (Shapes)**  
  - שימוש ב־`Ellipse`, `Rectangle`, `Path` וכו' ליצירת לוגו ואייקונים מותאמים ב־XAML, ללא צורך בקבצי תמונה חיצוניים.

- **כפתור מחיקה דינמי (Dynamic Delete Button)**  
  - חלון ניהול שליח (`CourierWindow`) כולל כפתור מחיקה שה־Visibility שלו נשלט לוגית:  
    - אינו מופיע כלל לשליח חדש.  
    - אפשר למחוק רק שליח שאינו פעיל וללא משלוחים פעילים – אחרת BL זורק חריגה מתאימה.  
  - כך מתקיימת בדיקה כפולה: גם בצד ה־UI וגם בצד ה־BL.

- **שדה סיסמה מוסתר (Password Masking)**  
  - מסך הלוגין משתמש ב־`PasswordBox` עם Template מותאם כדי להסתיר את הסיסמה (כוכביות).  
  - שילוב של UI מודרני עם אבטחת קלט בסיסית.

- **קיצור דרך עם Enter (Enter Key Action)**  
  - מסך הלוגין: לחיצה על Enter בשדה ת״ז או סיסמה מפעילה Login באופן אוטומטי.  
  - קיימת גם תכונת attached כללית (`EnterKeyCommand`) שמאפשרת לחבר פקודה ללחיצת Enter על כל רכיב UI.

#### ⏳ חוויית משתמש בזמן טעינות

- **אינדיקטור התקדמות (Progress Indicator)**  
  - חלונות מסוימים (למשל ניהול הזמנה / בחירת הזמנות זמינות) מציגים Overlay שקוף חלקית עם `ProgressBar` במצב `IsIndeterminate="True"` והודעת סטטוס.  
  - מאפשר למשתמש להבין שהמערכת עובדת ברקע בזמן פעולות ממושכות (טעינת רשימות, חישובים וכו').

---

### ⏱️ סימולטור ו-Asynchronicity

- **UI אסינכרוני ובלתי חוסם (Async UI)**  
  - טעינת רשימות (כמו רשימת ההזמנות) מתבצעת ב־**Thread רקע** באמצעות `Task.Run(...)` ולאחר מכן חזרה ל־UI באמצעות `Dispatcher.BeginInvoke(...)`.  
  - שימוש ב־`ObserverMutex` ו־Observers בשכבת BL כדי לוודא שהעדכונים מ־Simulator / BL אינם חוסמים את ה־UI ואינם יוצרים מרוצי תהליכים.  
  - גישת “fire-and-forget” עם Task.Run ב־BL (למשל בעדכוני שעון וסימולציה) שומרת על תגובתיות הממשק גם בזמן סימולציה רציפה.

- **סימולטור זמן (Clock Simulator) – ריצה ברקע**  
  - Thread ייעודי מריץ “שעון מערכת” המתקדם במרווחים קבועים, קורא ל־BL לעדכוני סטטוסים תקופתיים (הזמנות/שליחים/משלוחים).  
  - פרמטרי סימולטור (כמו אינטרוול בדקות) ניתנים להגדרה מתוך ה־UI, תוך שמירה על thread-safety בעזרת מנעולים ו־AsyncMutex.
</div>
---

## 🇺🇸 Overview (English)

### 🧱 DAL – Data Access Layer

- **Password fields in data entities (Courier & Config)**  
  - The data layer defines a `Password` field for courier entities and a `ManagerPassword` field in configuration.  
  - This enables basic credential handling for both the system manager and couriers.

- **Safe parsing in console tools (BlTest / DalTest)**  
  - The console test projects (`BlTest`, `DalTest`) rely heavily on `TryParse` (`int.TryParse`, `double.TryParse`, `Enum.TryParse`, etc.) with proper checks on the boolean return value.  
  - This prevents crashes on invalid input and provides a more robust interactive testing experience.

---

### 🧠 BL – Business Logic Layer

- **Password management – strength validation**  
  - A dedicated helper class enforces a strong password policy (minimum length, mixed upper/lowercase letters, digits and special characters).  
  - Courier password updates are validated explicitly against these security rules before being accepted.

- **Courier salary calculation 💰**  
  - A full business operation calculates a courier’s salary for a given time period.  
  - The computation takes into account:  
    - **The number of completed deliveries**, including on-time vs late deliveries.  
    - **The total distance traveled**, using company–destination distances per order.  
    - **Courier type** (car/motorcycle/bicycle/on-foot) to determine different base hourly rates and per-delivery bonuses.  
  - Results are returned as a `CourierSalary` object and surfaced in the admin UI.

---

### 🎨 PL – Presentation Layer (WPF UI)

#### ✅ Validation & UX

- **Input validation with visual feedback**  
  - Text boxes use a custom `Validation.ErrorTemplate` and a `Validation.HasError` trigger to display:  
    - A red border and light background when invalid.  
    - A small “!” badge with a `ToolTip` describing the error.  
  - This gives users clear, immediate feedback on invalid fields.

- **Converters and multi-value converters**  
  - Extensive use of `IValueConverter` and `IMultiValueConverter` for:  
    - Formatting numeric values (weight, volume, distance).  
    - Pre-save validation logic (phone, email, ID, etc.).  
    - Status-based colors and icons for orders and delivery types.  
    - Building composite display texts (e.g., “address (lat, lon)” or “courier (delivery type)”).  

- **Triggers – Property, Data, and Event**  
  - `EventTrigger`s drive hover/click animations on cards and buttons.  
  - `DataTrigger`s highlight cards based on counts of open / at-risk orders.  
  - Template triggers adjust colors/borders for hover, focus, read-only, and validation error states.

- **Custom ControlTemplates**  
  - `TextBox`, `PasswordBox`, `Button` and others use fully custom `ControlTemplate`s to create a modern UI: rounded corners, shadows, and click animations.  
  - Delete/secondary/action buttons have dedicated templates to clearly distinguish actions.

- **Graphics & Shapes**  
  - The UI uses WPF `Ellipse`, `Rectangle`, `Path`, and other shapes to render a custom logo and icons directly in XAML, without relying on external image files.

- **Dynamic delete button**  
  - The courier management window exposes a delete button whose `Visibility` is driven by state:  
    - Hidden for new couriers.  
    - BL prevents deleting active couriers or those with pending deliveries, throwing a specific exception.  
  - This combines UI gating with strict BL enforcement.

- **Password masking**  
  - The login screen uses a `PasswordBox` with a custom template to mask password input (asterisks), blending modern styling with basic input security.  

- **Enter key as action shortcut**  
  - In the login window, pressing Enter in the ID or password field automatically triggers the login action.  
  - A generic attached property (`EnterKeyCommand`) is also available to bind any command to the Enter key on arbitrary controls.

#### ⏳ Long-running operation UX

- **Progress indicator (spinner/ProgressBar)**  
  - Certain windows (e.g., order management and available orders) show a semi-transparent overlay with an indeterminate `ProgressBar` and status message.  
  - This clearly signals that background work is in progress (list loading, calculations, etc.) and prevents the user from interacting with incomplete UI state.

---

### ⏱️ Simulator & Asynchronous Behavior

- **Async, non-blocking UI (Async UI)**  
  - List loading (such as the order list) happens on a **background thread** using `Task.Run(...)`, and results are marshaled back to the UI thread via `Dispatcher.BeginInvoke(...)`.  
  - `ObserverMutex` and BL observers ensure simulator-driven updates don’t block the UI or cause race conditions.  
  - A “fire-and-forget” pattern with `Task.Run` inside the BL (e.g., clock updates and simulation routines) keeps the WPF front-end responsive even during continuous simulation.

- **Background time simulator (Clock Simulator)**  
  - A dedicated thread advances the simulated system clock at a configurable interval and calls into BL periodic update methods for couriers, orders, and deliveries.  
  - Simulator parameters (like interval in minutes) are configurable from the UI, with thread safety enforced using locks and async-aware mutexes.

---
