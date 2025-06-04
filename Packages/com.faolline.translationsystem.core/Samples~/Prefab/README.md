# 📦 Prefabs inclus dans TranslationSystem

Voici une description rapide des prefabs fournis pour faciliter l’utilisation de la librairie de traduction dans Unity.

---

## 🎛 TranslateDropdown.prefab
Un `TMP_Dropdown` avec le composant `TranslateObjectDropdown` déjà attaché. 

- ✅ Affiche une liste d'options traduites
- ✅ Se met à jour automatiquement avec la langue active

---

## 🌍 TranslateDropdownLanguageMenu.prefab
Un menu prêt à l’emploi permettant de **changer de langue à la volée**.

- ✅ Lié au `LanguageManager`
- ✅ Affiche toutes les langues activées dans `LanguageDataBase`

---

## ⚙️ TranslateManager.prefab
Une instance de `LanguageManager` préconfigurée.

- ✅ Lié à un `LanguageDataBase`
- ✅ Persiste automatiquement entre les scènes

---

## 📝 TranslateText(TMP).prefab
Un `TextMeshPro` avec le composant `TranslateObjectText` attaché.

- ✅ Se met automatiquement à jour selon la clé de traduction donnée
- ✅ Compatible avec le sélecteur de clé dans l’inspecteur

---

### 🖼 TranslateImage.prefab
Une `Image` avec le composant `TranslateObjectImage` attaché.

- ✅ Se met automatiquement à jour selon la clé de traduction donnée

---

## ✨ Comment utiliser ?

1. Glissez un prefab dans votre scène
2. Spécifiez les clés de traduction dans l’inspecteur
3. Appuyez sur Play pour voir la traduction active

---

> ⚠️ Vous pouvez personnaliser chaque prefab en les dupliquant dans votre projet.

---

**Auteur :** Faolline  
**Librairie :** Translation System Core
