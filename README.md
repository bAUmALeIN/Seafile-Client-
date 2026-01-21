# Seafile Client - BBS Me Hannover (Unofficial)

<div align="center">
  <img src="Resources/app_logo.png" alt="Seafile Client Logo" width="150">
  <br><br>
  
  <img src="https://img.shields.io/badge/Status-Beta%20v1.5.0-orange?style=flat-square" alt="Status Beta">
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows" alt="Platform Windows">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 8.0">
  <br><br>

  <b>Der inoffizielle Power-Client für die Cloud der BBS Me Hannover.</b>
  <br>
  <i>Entwickelt, um den Schulalltag effizienter, schneller und augenfreundlicher zu machen.</i>
</div>

<br>

> [!IMPORTANT]
> **Haftungsausschluss & Kontext**
> Dieses Projekt ist eine **private Eigenentwicklung** zu Lernzwecken. Es steht in **keiner offiziellen Verbindung** zur BBS Me Hannover oder den Betreibern des LARA-Portals.  
> Nutzung auf eigene Verantwortung. Bitte geht verantwortungsvoll mit der Infrastruktur der Schule um!

---

## 👋 Moin! Worum geht's hier?

Wer die Abendschule oder Ausbildung an der BBS Me besucht, kennt das LARA-Portal und die Seafile-Integration. Der offizielle Weg über den Browser funktioniert, aber als Software-Entwickler wollte ich mehr: **Mehr Speed, weniger Klicks und einen echten Dark Mode (für längere Abendeinsätze 😉 ).**

Dieser Client ist ein nativer Windows-Wrapper, der die Brücke zwischen dem komplexen LARA-Login und der Seafile-API schlägt. Er automatisiert das Anmeldeprozedere und bietet Funktionen, die im Webinterface fehlen oder umständlich sind.

## ✨ Warum diesen Client nutzen?

### 🔐 Zero-Friction Login (SSO)
Das nervige Durchklicken durch die LARA-Anmeldemasken entfällt.
* **Wie es funktioniert:** Ein integrierter Browser (WebView2) übernimmt die Navigation im Hintergrund.
* **Das Ergebnis:** Du gibst deine Daten einmal ein, der `AuthManager` extrahiert sicher den Session-Cookie (`seahub_auth`) und du bist sofort in deinen Dateien.

### 🚀 Performance & Multithreading
Warum warten, wenn es auch parallel geht?
* **Paralleler Download:** Der Client nutzt `SemaphoreSlim`, um bis zu **5 Dateien gleichzeitig** zu laden. Gerade bei Ordnern mit vielen kleinen Skripten oder PDFs spart das enorm Zeit.
* **Smart-ZIP:** Markiere mehrere Ordner oder Dateien – der Client entscheidet intelligent, wie diese am besten gepackt und als **ein einziges Archiv** geladen werden.

### 🛡️ Robuster Upload-Core
Uploads im Web brechen gerne mal ab oder werfen kryptische Fehler.
* Der `SeafileClient` nutzt einen eigens geschriebenen `ManualMultipartContent`-Wrapper. Das umgeht bekannte "400 Bad Request"-Probleme der Standard-Bibliotheken und sorgt dafür, dass deine Hausaufgaben auch wirklich ankommen.

### 🎨 Eye-Candy (UI/UX)
* **True Dark Mode:** Basierend auf der `ReaLTaiizor` Library habe ich eine Oberfläche gebaut, die auch spät abends die Augen schont.
* **Responsiv:** Keine generischen Listen – Icons, Header und Statusanzeigen werden via Custom-Drawing gerendert.

---

## 🛠️ Unter der Haube (Tech Stack)

Für die Techniker hier die Architektur:

| Modul | Technologie & Pattern |
| :--- | :--- |
| **Core Framework** | .NET 8 (Windows Forms) |
| **Netzwerk** | `HttpClient` mit Custom Handlers & Async/Await Pattern |
| **Authentifizierung** | WebView2 (Edge Chromium) Injection & Cookie Interception |
| **Datenhaltung** | SQLite (`Microsoft.Data.Sqlite`) für Settings & Tokens |
| **UI Framework** | WinForms mit Custom Controls & ReaLTaiizor Themes |

---

## 🔮 Roadmap & Vision

Ich will nicht nur Dateien schubsen – das Ziel ist ein zentrales Dashboard für den Schulalltag.

- [ ] **Moodle Integration** 📚
  * *Plan:* Direkter Zugriff auf Kursmaterialien und Uploads ohne Browser-Wechsel.
  * *Status:* Machbarkeitsanalyse positiv.

- [ ] **E-Mail Integration** 📧
  * *Plan:* Einbindung des Schul-Postfachs direkt in die App.
  * *Status:* Konzeptphase.

- [ ] **WebUntis (Stundenplan)** 📅
  * *Plan:* Der aktuelle Stundenplan auf einen Blick.
  * *Status:* **Evaluierung.** Es wird aktuell geprüft, ob dies stabil via API oder Parsing umsetzbar ist.

---

## 🚀 Installation & Start

### Voraussetzungen
* **Betriebssystem:** Windows 10 oder 11 (64-Bit)
* **Runtime:** [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* **Zugang:** Ein gültiges Konto für das BBS Me LARA Portal.

### Los geht's
1.  **Repository klonen:**
    ```bash
    git clone [https://github.com/bAUmALeIN/Seafile-Client-.git](https://github.com/bAUmALeIN/Seafile-Client-.git)
    ```
2.  **Kompilieren:**
    Öffne die `WinFormsApp3.sln` in Visual Studio 2022 und starte den Build.
3.  **Login:**
    Beim ersten Start öffnet sich das Login-Fenster. Nach erfolgreicher Anmeldung speichert der Client den Token verschlüsselt lokal.

---

## 🐛 Feedback & Bugs

Das Projekt ist "Work in Progress" und entsteht neben der Schule/Arbeit.
Du hast einen Bug gefunden oder eine Idee für ein cooles Feature?

1.  Schau in die **Issues**, ob es schon gemeldet wurde.
2.  Erstelle ein neues Issue mit:
    * Was wolltest du machen?
    * Was ist passiert? (Screenshots helfen!)
    * Log-Auszug (falls vorhanden).

Pull Requests sind natürlich auch gerne gesehen!

---

<div align="center">
  <i>Entwickelt mit ❤️ und C# für die Community der BBS</i>
</div>