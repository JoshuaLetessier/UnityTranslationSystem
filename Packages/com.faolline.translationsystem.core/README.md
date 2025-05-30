# Translation System for Unity

A modular and extensible translation/localization system designed to be flexible, editable and easily integrated into any Unity project.

## ✨ Features

- 🔤 CSV-based translation input (one file per UI domain or feature)
- 📦 Auto-generation of JSON files and translation database
- 🧠 Key grouping and optional hierarchical structure
- 🎛️ Custom Unity inspector for easy integration with TextMeshPro
- 🌍 LanguageManager singleton for dynamic switching
- 💾 UPM-ready format for package management

## 🗂 Folder structure

- `Assets/Translations/CSV/` – CSV files containing translations
- `Assets/Translations/Generated/` – Auto-generated JSON and key database

## 🧩 Usage

1. Create your translations as `.csv` in `Assets/Translations/CSV/`.
2. Go to `Tools > Translation > Generate JSON From CSV`.
3. Assign a `TranslateObjectText` to your TextMeshPro object.
4. Select a translation key from the dropdown — done ✅


## Instalation
https://github.com/JoshuaLetessier/UnityTranslationSystem.git?path=Packages/com.faolline.translationsystem.core