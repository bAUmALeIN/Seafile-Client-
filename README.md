# Seafile Client - BBS Me Hannover (Unofficial)

<div align="center">
  <img src="Resources/app_logo.png" alt="Seafile Client Logo" width="150">
  <br><br>
  
  <img src="https://img.shields.io/badge/Version-v1.5.0%20-Blue?style=flat-square" alt="Version 1.5.0 Beta">
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows" alt="Platform Windows">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 8.0">
  <br><br>

  <b>Inoffizieller Seafile-Client für die BBS Me Hannover.</b>
  <br>
  <i>Optimiert für das LARA Portal mit automatischem Single Sign-On (SSO).</i>
</div>

<br>

> [!NOTE]  
>* **Dieses Projekt dient ausschließlich zu Lern- und Analysezwecken im Kontext von Client-Server-Authentifizierung.
>*Es ist nicht für den produktiven Einsatz oder zur Umgehung administrativer Richtlinien gedacht**.**

>[!NOTE]
> **Status: Version 1.2.1 (Public Beta)** > Dies ist das erste Release (V1.x.x). Es können noch Bugs oder unerwartete Fehler auftreten.  
> Da ich dieses Projekt **neben der Abendschule** entwickle, bitte ich um etwas Geduld bei Fixes. Ich versuche aber, gemeldete Probleme so zeitnah wie möglich zu beheben!

<br>

## 📖 Über das Projekt

Dies ist ein Windows-Desktop-Client für die Cloud-Infrastruktur der **Berufsbildenden Schule Metalltechnik • Elektrotechnik (BBS Me) Hannover**.

## ✨ Features & Highlights

### 🔐 Intelligenter Login (SSO)
* **Auto-Pilot:** Der `AuthManager` nutzt ein injiziertes Skript, um automatisch durch die LARA-Anmeldeseiten zu navigieren und Buttons zu klicken.
* **Token-Extraction:** Erkennt automatisch den `seahub_auth` Cookie aus dem Browser-Kontext und speichert ihn sicher lokal in einer SQLite-Datenbank.

### 🚀 Performance & Transfer
* **Turbo-Download:** Der Client nutzt **Multithreading** (via `SemaphoreSlim`), um bis zu 5 Dateien gleichzeitig herunterzuladen. Das ist bei vielen kleinen Dateien deutlich schneller.
* **Smart-ZIP:** Du kannst mehrere Ordner oder Dateien markieren – der Client packt sie serverseitig oder lokal zusammen und lädt **ein einziges ZIP-Archiv** herunter.
* **Stabiler Upload:** Enthält einen eigenen `ManualMultipartContent`-Wrapper, um spezifische "400 Bad Request"-Fehler zu umgehen, die beim Standard-Upload oft auftreten.

### 🎨 Moderne Benutzeroberfläche
* **Dark Mode:** Komplett dunkles Design (basierend auf `ReaLTaiizor`) für angenehmes Arbeiten am Abend.
* **Responsive UI:** Custom ListView mit eigens gezeichneten Headern und Icons.
* **Status-Feedback:** Detaillierte Fortschrittsanzeige in Echtzeit.

## 🐛 Bugs & Feedback

Fehler gefunden? Hast du eine Idee für ein neues Feature?
Gerne einfach ein **Issue** hier auf GitHub erstellen!

Bitte gib dabei an:
1. Was hast du gemacht?
2. Was ist passiert (Fehlermeldung)?
3. Welches Betriebssystem nutzt du?

*Ich schaue mir die Reports an, sobald es die Zeit neben der Schule zulässt.*

## 🛠️ Technische Architektur

Das Projekt ist eine **Windows Forms** Anwendung basierend auf **.NET 8**.

| Komponente | Beschreibung |
| :--- | :--- |
| **AuthManager** | Steuert WebView2, injiziert JS für die LARA-Navigation und extrahiert Auth-Tokens. |
| **DownloadManager** | Verwaltet die asynchronen Queues, ZIP-Logik und Fehlerbehandlung. |
| **SeafileClient** | Eigener API-Wrapper mit manuellem HTTP-Request-Building für maximale Kompatibilität. |
| **UIHelper** | Zentrale Verwaltung für Styles, Dialoge und das Custom-Drawing der Listen. |
| **Datenbank** | SQLite (`Microsoft.Data.Sqlite`) zur lokalen Speicherung von Einstellungen. |

## 🚀 Installation

### Voraussetzungen
* Windows 10 oder 11 (64-Bit)
* [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* Gültiger Account für das BBS Me LARA Portal.

### Einrichtung (Source Code)
1.  **Repository klonen:**
    ```bash
    git clone [https://github.com/bAUmALeIN/Seafile-Client-.git]
    ```
2.  **In Visual Studio öffnen:**
    Lade die Solution `WinFormsApp3.sln`.
3.  **Starten:**
    Beim ersten Start öffnet sich das eingebettete Browser-Fenster für den LARA-Login.

---

## ⚠️ Disclaimer

Dies ist ein **inoffizielles Open-Source-Projekt** von Schülern.
* Es besteht **keine offizielle Verbindung** zur BBS Me Hannover oder den Betreibern des LARA Portals.
* Die Software nutzt Automatisierungstechniken für den Login. Änderungen am LARA-Portal könnten Updates am Client erfordern.
* Nutzung auf eigene Gefahr.

---
*Entwickelt mit ❤️ und C# für die Community der BBS Me.*
