# Seafile Client - BBS Me Hannover (Unofficial)

<div align="center">
  <img src="Ressources/app_logo.png" alt="Seafile Client Logo" width="150">
  <br>
  <b>Ein angepasster Seafile-Desktop-Client für die BBS Me Hannover.</b>
  <br>
  <i>Ermöglicht den Zugriff via LARA Portal Single Sign-On (SSO).</i>
</div>

<br>

## 📖 Über das Projekt

Dies ist ein spezialisierter Windows-Desktop-Client für die Seafile-Infrastruktur der **Berufsbildenden Schule Metalltechnik • Elektrotechnik (BBS Me) Hannover**.

Im Gegensatz zum Standard-Client ist diese Anwendung speziell für den Authentifizierungsprozess über das **LARA Portal** optimiert. Sie ermöglicht Schülern und Lehrkräften den direkten Zugriff auf ihre Schul-Dateien ohne Browser.

Das Projekt basiert auf C# und Windows Forms (.NET).

## ✨ Funktionen

* **LARA Portal Integration:** Native Unterstützung für den Single Sign-On (SSO) Login der Schule.
* **Schul-Cloud Zugriff:** Direkte Verbindung zur Seafile-Instanz der BBS Me.
* **Transfer-Manager:** Überwachung von Uploads und Downloads (`FrmTransferDetail`).
* **Benutzerfreundliche UI:** Einfache Oberfläche zur Verwaltung von Unterrichtsmaterialien und Dokumenten.

## 🛠️ Technologien

* **Sprache:** C#
* **Framework:** .NET (Windows Forms)
* **Authentifizierung:** OAuth / SSO (LARA Portal)

## 🚀 Installation & Einrichtung

Voraussetzung: Ein gültiger Account im LARA Portal der BBS Me Hannover.

1.  **Repository klonen:**
    ```bash
    git clone [https://github.com/bAUmALeIN/Seafile-Client-.git](https://github.com/bAUmALeIN/Seafile-Client-.git)
    ```

2.  **Projekt öffnen:**
    Lade die Solution `WinFormsApp3.sln` in Visual Studio.

3.  **Bauen & Starten:**
    Kompiliere das Projekt und starte die Anwendung. Beim ersten Start wirst du zum LARA Portal Login weitergeleitet.

## 📂 Wichtige Komponenten

* `FrmLogin.cs` - Handhabt den SSO-Login-Prozess via LARA Portal.
* `FrmTransferDetail.cs` - Anzeige des Synchronisationsstatus.
* `UIHelper.cs` - Anpassungen für das schulspezifische Design.

## ⚠️ Wichtiger Hinweis

Dies ist ein **inoffizielles Projekt** von Schülern/Entwicklern und keine offizielle Software der BBS Me Hannover oder der Region Hannover. Die Nutzung erfolgt auf eigene Verantwortung.

---
*Entwickelt für die Community der BBS Me Hannover.*
