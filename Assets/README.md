# 🏪 Shop Tycoon - Unity Game

משחק טייקון תלת-ממדי ב-Unity בהשראת משחקי Tycoon ברובלוקס.
נהל חנות, מלא מדפים, שדרג קופות, והרוויח כסף מלקוחות!

---

## 📁 מבנה הפרויקט

```
Assets/
└── Scripts/
    ├── Player/
    │   ├── PlayerController.cs    # תנועת שחקן (WASD)
    │   ├── TopDownCamera.cs       # מצלמה עילית
    │   └── PlayerInventory.cs     # מלאי שחקן (נשיאת סחורה)
    ├── Tycoon/
    │   ├── TycoonManager.cs       # כלכלה ושדרוגים (Singleton)
    │   ├── TycoonButton.cs        # כפתורי רצפה לקנייה/שדרוג
    │   └── TycoonUnlockable.cs    # אובייקטים שנפתחים בקנייה
    ├── Shop/
    │   ├── Shelf.cs               # מדפים (מילוי + מכירה)
    │   ├── SupplyCrate.cs         # ארגז אספקה (איסוף סחורה)
    │   └── RegisterQueue.cs       # קופה + תור לקוחות
    ├── NPC/
    │   ├── CustomerSpawner.cs     # מזמן לקוחות
    │   └── CustomerAI.cs          # AI לקוח (NavMesh)
    └── UI/
        └── GameUI.cs              # ממשק משתמש (כסף, מלאי)
```

---

## 🚀 הוראות הקמה (Setup) ביוניטי - שלב אחרי שלב

### שלב 1: צור פרויקט חדש ביוניטי
1. פתח **Unity Hub** > **New Project** > **3D (URP)** או **3D Core**.
2. תן שם לפרויקט (למשל "ShopTycoon").
3. העתק את תיקיית `Assets/Scripts/` לתוך תיקיית ה-Assets של הפרויקט.

### שלב 2: בנה את הסצנה (Scene Setup)

#### א. רצפת החנות
1. **GameObject > 3D Object > Plane** - הגדל אותו (Scale: 5,1,5) כדי ליצור רצפה גדולה.
2. תן לו חומר (Material) בצבע אפור בהיר או דמוי אריחי רצפה.

#### ב. השחקן (Player)
1. **GameObject > 3D Object > Capsule** - זו הדמות שלך.
2. הוסף **Rigidbody** (בתפריט Component > Physics > Rigidbody).
   - ✅ סמן **Freeze Rotation** על כל שלושת הצירים (X, Y, Z).
3. הוסף את הסקריפטים: **PlayerController** + **PlayerInventory**.
4. בלשונית Inspector, הגדר את ה-**Tag** של השחקן ל-**"Player"**.
   - אם ה-Tag "Player" לא קיים, צור אותו ב-**Edit > Project Settings > Tags and Layers**.
5. צור **Empty GameObject** כילד של השחקן, קרא לו "StackPoint" והזז אותו מעל הראש (Y = 1.5).
   - גרור אותו לשדה "Stack Parent" ב-PlayerInventory.

#### ג. המצלמה (Camera)
1. בחר את **Main Camera** שכבר קיימת בסצנה.
2. הוסף את הסקריפט **TopDownCamera**.
3. גרור את אובייקט השחקן (Capsule) לשדה **"Target"**.
4. ה-Inspector מאפשר לך לשנות גובה (Height), מרחק (Distance) וזווית (Angle) לפי הטעם שלך.

#### ד. TycoonManager
1. **GameObject > Create Empty** - קרא לו "TycoonManager".
2. הוסף את הסקריפט **TycoonManager**.
3. הגדר **Starting Money** ל-500 (או כמה שתרצה).

### שלב 3: בנה את החנות

#### א. ארגז אספקה (Supply Crate)
1. **GameObject > 3D Object > Cube** - הקטן אותו קצת, צבע אותו חום.
2. הוסף **BoxCollider** ✅ סמן **"Is Trigger"**.
3. הוסף את הסקריפט **SupplyCrate**.
4. מקם אותו ליד הכניסה לחנות.

#### ב. מדפים (Shelves)
1. **GameObject > 3D Object > Cube** - עצב אותו כמדף (Scale: 2, 1.5, 0.5).
2. הוסף **BoxCollider** ✅ סמן **"Is Trigger"** (הגדל אותו קצת מהמודל).
3. הוסף את הסקריפט **Shelf**.
4. שכפל ליצירת מדפים נוספים.

#### ג. קופה (Register)
1. **GameObject > 3D Object > Cube** - עצב כקופה.
2. צור 4-5 **Empty GameObjects** כילדים בשם "QueueSlot_0", "QueueSlot_1" וכו'.
   - מקם אותם בשורה לפני הקופה (אלו מיקומי התור).
3. הוסף **BoxCollider** ✅ סמן **"Is Trigger"**.
4. הוסף את הסקריפט **RegisterQueue**.
5. גרור את ה-QueueSlots לשדה **"Queue Slots"** (בסדר הנכון!).

### שלב 4: כפתורי טייקון (Tycoon Buttons)

1. **GameObject > 3D Object > Cube** - הקטן אותו והפוך לשטוח (Scale: 1.5, 0.1, 1.5).
2. הוסף **BoxCollider** ✅ סמן **"Is Trigger"**.
3. הוסף את הסקריפט **TycoonButton**.
4. הגדר:
   - **Cost**: מחיר הרכישה
   - **Display Name**: "Buy Shelf" / "Upgrade Register" וכו'
   - **Unlock Target**: גרור את האובייקט שאמור להופיע (מדף, קופה...)
   - **Upgrade Type**: בחר את סוג השדרוג (אם רלוונטי)
5. צבע את הכפתור בירוק זוהר!
6. מקם אותו על הרצפה ליד המיקום שבו השדרוג/מדף אמור להופיע.

### שלב 5: לקוחות (Customers)

#### א. צור Customer Prefab
1. **GameObject > 3D Object > Capsule** - הקטן קצת (Scale: 0.8, 0.8, 0.8).
2. הוסף **NavMeshAgent** (Component > Navigation > NavMesh Agent).
3. הוסף את הסקריפט **CustomerAI**.
4. צבע אותו בצבע שונה מהשחקן (כחול, אדום, וכו').
5. **גרור אותו לתיקיית Prefabs** כדי ליצור Prefab, ואז מחק אותו מהסצנה.

#### ב. אפה NavMesh
1. **Window > AI > Navigation** (אם לא רואה - **Package Manager > AI Navigation**).
2. בחר את הרצפה.
3. בלשונית **Bake** לחץ **Bake** - זה ייצור את "המפה" שעליה הלקוחות יכולים ללכת.

#### ג. הגדר את ה-Spawner
1. **GameObject > Create Empty** - קרא לו "CustomerSpawner".
2. הוסף את הסקריפט **CustomerSpawner**.
3. גרור את ה-Customer Prefab לשדה **"Customer Prefab"**.
4. צור Empty GameObject ליד הכניסה - זו נקודת ה-Spawn. גרור לשדה **"Spawn Points"**.
5. צור Empty GameObject ליד היציאה - זו נקודת ה-Exit. גרור לשדה **"Exit Point"**.
6. גרור את כל המדפים לשדה **"Available Shelves"**.
7. גרור את כל הקופות לשדה **"Available Registers"**.

### שלב 6: UI
1. **GameObject > UI > Canvas**.
2. צור **TextMeshPro - Text (UI)** עבור: כסף, מלאי, ולקוחות.
3. מקם אותם בפינה העליונה.
4. הוסף את הסקריפט **GameUI** ל-Canvas.
5. גרור את הטקסטים לשדות המתאימים.

### שלב 7: הפעל ובדוק! ▶️
1. לחץ **Play** ביוניטי.
2. זוז עם **WASD** לארגז האספקה - תאסוף סחורה.
3. לך למדף - תמלא אותו אוטומטית.
4. לקוחות יתחילו להיכנס, לקנות ולשלם!
5. דרוך על כפתורי טייקון כדי לקנות שדרוגים!

---

## 🔧 רשימת Tags נדרשים
- **"Player"** - חייב להיות על אובייקט השחקן

## 📦 חבילות נדרשות (Packages)
- **TextMeshPro** (מגיע מותקן כברירת מחדל)
- **AI Navigation** (לפעמים צריך להתקין דרך Package Manager)

---

## 🎮 תודלת המשחק (Game Loop)

```
שחקן אוסף סחורה מארגז
         ↓
שחקן ממלא מדפים
         ↓
לקוחות נכנסים → הולכים למדף → לוקחים מוצר → עומדים בתור → משלמים
         ↓
שחקן מרוויח כסף
         ↓
שחקן קונה שדרוגים (כפתורי רצפה):
  • עוד מדפים (מגוון + קיבולת)
  • קופות מהירות יותר
  • קיבולת נשיאה גדולה יותר
  • איכות ירקות טובה יותר (מחירים גבוהים יותר)
         ↓
חוזר חלילה! 🔄
```
