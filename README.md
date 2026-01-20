# Seafile Client – BBS ME Hannover (inoffiziell)

<div align="center">
  <img src="Resources/app_logo.png" alt="Seafile Client Logo" width="150">
  <br><br>
<<<<<<< Updated upstream
  
  <img src="https://img.shields.io/badge/Version-v1.0%20(Beta)-orange?style=flat-square" alt="Version 1.0 Beta">
=======

  <img src="https://img.shields.io/badge/Version-v1.2.1%20(Beta)-orange?style=flat-square" alt="Version 1.2.1 Beta">
>>>>>>> Stashed changes
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows" alt="Platform Windows">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 8.0">
  <br><br>

  <b>Inoffizieller Windows-Client für den Seafile-Zugriff der BBS ME Hannover</b><br>
  <i>Mit integriertem LARA-Login und optimierter Dateiverwaltung</i>
</div>

---
> [!NOTE]  
>* **Dieses Projekt dient ausschließlich zu Lern- und Analysezwecken im Kontext von Client-Server-Authentifizierung.
>*Es ist nicht für den produktiven Einsatz oder zur Umgehung administrativer Richtlinien gedacht**.**

>[!NOTE]
> **Status: Version 1.0 (Public Beta)** > Dies ist das erste Release. Es können noch Bugs oder unerwartete Fehler auftreten.  
> Da ich dieses Projekt **neben der Abendschule** entwickle, bitte ich um etwas Geduld bei Fixes. Ich versuche aber, gemeldete Probleme so zeitnah wie möglich zu beheben!


## 📌 Projektübersicht

Dieses Projekt ist ein **Windows-Desktop-Client (WinForms, .NET 8)** zur Nutzung der Seafile-Cloud der  
**Berufsbildenden Schule Metalltechnik · Elektrotechnik (BBS ME) Hannover**.

Der Fokus liegt auf:
- einer **nahtlosen Anmeldung über das LARA-Portal**
- einer **übersichtlichen Datei- und Bibliotheksverwaltung**
- stabilen **Up- und Download-Prozessen**
- einer modernen, dunklen Benutzeroberfläche

Das Projekt entstand im Rahmen von Lern- und Entwicklungszwecken und dient gleichzeitig als praxisnahes Beispiel für:
- Client-Server-Kommunikation
- Authentifizierung über WebView
- asynchrone Dateiübertragung
- strukturierte WinForms-Architektur

---

## 🔐 Anmeldung & Authentifizierung

### LARA-SSO-Integration
Die Anmeldung erfolgt über eine integrierte **WebView2-Komponente**, welche den regulären Login-Prozess des LARA-Portals abbildet.

Der Ablauf:
1. Öffnen der LARA-Login-Seite in WebView2
2. Automatisierte Navigation durch den Login-Prozess
3. Erkennung und Extraktion des `seahub_auth` Cookies
4. Lokale, verschlüsselte Speicherung des Tokens (SQLite)

Die Authentifizierung wird vollständig im **`AuthManager`** gekapselt, sodass andere Komponenten ausschließlich mit gültigen Tokens arbeiten.

---

## 📂 Datei- & Bibliotheksverwaltung

### Bibliotheken
- Anzeige aller eigenen, geteilten und Gruppen-Bibliotheken
- Gruppierung nach Typ (Eigene / Freigegebene / Gruppen)
- Erstellung und Löschung von Bibliotheken

### Verzeichnisnavigation
- Klassische Ordnerstruktur mit `.. [Zurück]`
- Breadcrumb-Navigation
- Zwischenspeicherung von Verzeichnisinhalten (Cache)

### Dateitypen & Vorschau
- Dynamische Dateisymbole basierend auf Dateiendung
- Automatisches Laden von Vorschaubildern für Bilder (`jpg`, `png`, `gif`)
- Asynchrones Thumbnail-Loading mit Abbruchlogik

---

## 🔄 Download- & Upload-Funktionen

### Downloads
- Einzeldateien
- Ganze Ordner
- Komplette Bibliotheken
- Mehrfachauswahl als **ZIP-Archiv**

Der **`DownloadManager`** nutzt:
- parallele Downloads (Semaphore-basiert)
- Fortschritts- & Statusmeldungen
- Geschwindigkeitsberechnung

### Uploads
- Drag & Drop aus dem Windows Explorer
- Unterstützung für mehrere Dateien und Ordner
- Eigene Multipart-Implementierung zur Vermeidung von API-Problemen

---

## 📦 Verschieben & Organisation

- Drag & Drop innerhalb der Verzeichnisstruktur
- Verschieben von Dateien und Ordnern
- Fallback-Mechanismus:
  - Falls `Move` fehlschlägt → `Copy + Delete`
- Sicherheitsabfragen vor kritischen Aktionen

---

## 🔍 Globale Suche

- Bibliotheksübergreifende Dateisuche
- Parallele Durchsuchung aller Bibliotheken
- Gruppierte Suchergebnisse
- Direkter Sprung zum Fundort

---

## 🎨 Benutzeroberfläche

- Dark Mode (ReaLTaiizor)
- Eigene ListView-Zeichnung
- Kontextmenüs
- Statusanzeigen & Snackbars
- Responsive Spaltenanpassung

---

## 🧠 Technische Architektur

| Komponente | Aufgabe |
|----------|--------|
| **AuthManager** | Login, WebView-Steuerung, Token-Verwaltung |
| **SeafileClient** | API-Kommunikation, HTTP-Requests |
| **DownloadManager** | Downloads, Uploads, ZIP-Logik |
| **NavigationState** | Aktuelle Position & Pfadlogik |
| **CacheManager** | Zwischenspeicherung von API-Daten |
| **BreadcrumbManager** | Navigationsanzeige |
| **UiHelper** | Dialoge, Icons, Styling |

Lokale Daten werden mit **SQLite** gespeichert.

---

## 🚀 Installation

### Voraussetzungen
- Windows 10 oder 11 (64 Bit)
- .NET Desktop Runtime 8.0
- Gültiger LARA-Zugang der BBS ME Hannover


### ⚠️ Disclaimer

Dies ist ein **inoffizielles Open-Source-Projekt** von Schülern.
* Es besteht **keine offizielle Verbindung** zur BBS Me Hannover oder den Betreibern des LARA Portals.
* Die Software nutzt Automatisierungstechniken für den Login. Änderungen am LARA-Portal könnten Updates am Client erfordern.
* Nutzung auf eigene Gefahr.

---
*Entwickelt mit ❤️ und C# für die Community der BBS Me.*


