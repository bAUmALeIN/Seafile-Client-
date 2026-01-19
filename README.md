# Seafile Client - BBS Me Hannover (Unofficial)

<div align="center">
  <img src="Resources/app_logo.png" alt="Seafile Client Logo" width="150">
  <br><br>
  
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows" alt="Platform Windows">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 8.0">
  <img src="https://img.shields.io/badge/UI-Material%20Design-009688?style=flat-square" alt="Material Design">
  <br><br>

  <b>Ein leistungsstarker, inoffizieller Seafile-Client für die BBS Me Hannover.</b>
  <br>
  <i>Optimiert für das LARA Portal mit automatischem Single Sign-On (SSO).</i>
</div>

<br>

## 📖 Über das Projekt

Dies ist ein spezialisierter Windows-Desktop-Client für die Cloud-Infrastruktur der **Berufsbildenden Schule Metalltechnik • Elektrotechnik (BBS Me) Hannover**.

Standard-Clients scheitern oft an komplexen SSO-Weiterleitungen. Dieser Client löst das Problem durch eine integrierte Browser-Engine (`WebView2`), die den Anmelde-Prozess über das **LARA Portal** automatisiert und den Zugriff auf Schuldateien nahtlos ermöglicht.

## ✨ Highlight-Funktionen

### 🔐 Intelligenter Login (SSO)
* **Auto-Pilot:** Der `AuthManager` navigiert automatisch durch die LARA-Anmeldeseiten.
* **Token-Extraction:** Erkennt automatisch den `seahub_auth` Token und speichert ihn sicher lokal (SQLite).

### 🚀 Performance & Transfer
* **Turbo-Download:** Lädt Ordnerinhalte parallel herunter (Multithreading), was deutlich schneller ist als der serielle Download.
* **Batch-ZIP:** Mehrere Dateien oder Ordner markieren und als **ein einziges ZIP-Archiv** herunterladen.
* **Resumable Uploads:** Stabilere Upload-Logik für große Dateien.

### 🎨 Moderne Benutzeroberfläche
* Basiert auf **Material Design** (via `ReaLTaiizor`).
* Dunkles Design (Dark Mode) für augenschonendes Arbeiten.
* Übersichtliche Statusanzeige für laufende Transfers.

## 🛠️ Technische Architektur

Das Projekt ist eine **Windows Forms** Anwendung basierend auf **.NET 8**.

| Komponente | Beschreibung |
| :--- | :--- |
| **AuthManager** | Steuert WebView2, injiziert JS-Helper für die Navigation und extrahiert Cookies. |
| **DownloadManager** | Kernstück für Dateitransfers. Verwaltet Queues, ZIP-Erstellung und Fehlerbehandlung. |
| **SeafileClient** | Eigener API-Wrapper. Behebt spezifische "400 Bad Request" Probleme durch manuellen Multipart-Upload. |
| **UIHelper** | Zentrale Verwaltung für Styles, Dialoge und das responsive Layout. |

## 🚀 Installation

### Voraussetzungen
* Windows 10 oder 11 (64-Bit)
* [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* Gültiger Account für das BBS Me LARA Portal.

### Einrichtung für Entwickler
1.  **Repository klonen:**
    ```bash
    git clone [https://github.com/bAUmALeIN/Seafile-Client-.git](https://github.com/bAUmALeIN/Seafile-Client-.git)
    ```
2.  **In Visual Studio öffnen:**
    Lade die Solution `WinFormsApp3.sln`.
3.  **NuGet Pakete:**
    Stelle sicher, dass `ReaLTaiizor`, `Microsoft.Web.WebView2` und `Microsoft.Data.Sqlite` geladen sind.
4.  **Starten:**
    Beim ersten Start öffnet sich das eingebettete Browser-Fenster für den LARA-Login.

## ⚠️ Disclaimer

Dies ist ein **Open-Source-Hobbyprojekt** von Schülern.
* Es besteht **keine offizielle Verbindung** zur BBS Me Hannover oder den Betreibern des LARA Portals.
* Die Software nutzt Web-Scraping/Automatisierungstechniken für den Login. Änderungen am LARA-Portal könnten Updates am Client erfordern.
* Nutzung auf eigene Gefahr.

---
*Entwickelt mit ❤️ und C# für die Community der BBS Me.*
