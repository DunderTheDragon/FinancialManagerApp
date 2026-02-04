# Dokumentacja Projektu
## System Zarządzania Budżetem "Finansense"

**Autorzy:**
- Kacper Jarzembowski (Backend)
- Remigiusz Chodowiec (Frontend, Import CSV, Integracja API Revolut)

**Data:** 2026

---

## Spis treści

1. [Wprowadzenie](#wprowadzenie)
2. [Architektura systemu](#architektura-systemu)
3. [Technologie i narzędzia](#technologie-i-narzędzia)
4. [Struktura bazy danych](#struktura-bazy-danych)
5. [Implementacja funkcjonalności](#implementacja-funkcjonalności)
6. [Problemy napotkane podczas implementacji](#problemy-napotkane-podczas-implementacji)
7. [Alternatywne rozwiązania](#alternatywne-rozwiązania)
8. [Podsumowanie](#podsumowanie)

---

## 1. Wprowadzenie

### 1.1. Cel projektu

Projekt "Finansense" to aplikacja desktopowa służąca do zarządzania budżetem osobistym. System umożliwia użytkownikom śledzenie wydatków i przychodów z różnych źródeł, kategoryzację transakcji, zarządzanie celami oszczędnościowymi oraz wizualizację danych finansowych za pomocą wykresów.

### 1.2. Zakres funkcjonalności

Zgodnie ze specyfikacją, system powinien oferować następujące funkcjonalności:

- **Autentykacja użytkowników** - logowanie i rejestracja
- **Zarządzanie portfelami** - tworzenie i zarządzanie portfelami gotówkowymi oraz integracja z kontami bankowymi (API)
- **Zarządzanie transakcjami** - dodawanie, edycja, usuwanie i kategoryzacja transakcji
- **Automatyczna kategoryzacja** - inteligentne przypisywanie kategorii na podstawie reguł użytkownika i systemowych
- **Cele oszczędnościowe** - tworzenie i zarządzanie skarbonkami z możliwością cyklicznych wpłat
- **Wizualizacja danych** - wykresy słupkowe i kołowe przedstawiające wydatki według kategorii
- **Import transakcji** - automatyczny import z kont bankowych (API) lub ręczny import z plików CSV

### 1.3. Status implementacji

Większość funkcjonalności została zaimplementowana zgodnie ze specyfikacją. Jednakże integracja z API Revolut napotkała poważne problemy techniczne, które uniemożliwiły pełne wdrożenie tej funkcjonalności. W zamian zaimplementowano alternatywne rozwiązanie w postaci importu transakcji z plików CSV.

---

## 2. Architektura systemu

### 2.1. Wzorzec architektoniczny

Aplikacja została zbudowana zgodnie z wzorcem **MVVM (Model-View-ViewModel)**, który zapewnia separację logiki biznesowej od warstwy prezentacji.

```
┌─────────────┐
│    View     │ (XAML - interfejs użytkownika)
│  (WPF UI)   │
└──────┬──────┘
       │ Data Binding
┌──────▼──────┐
│ ViewModel   │ (Logika prezentacji, komendy)
└──────┬──────┘
       │
┌──────▼──────┐
│   Model     │ (Dane, modele biznesowe)
└──────┬──────┘
       │
┌──────▼──────┐
│  Services   │ (Logika biznesowa, API, CSV)
└──────┬──────┘
       │
┌──────▼──────┐
│   Database  │ (MySQL)
└─────────────┘
```

### 2.2. Struktura projektu

```
FinancialManagerApp/
├── Core/                    # Klasy bazowe (ViewModelBase, RelayCommand)
├── Models/                  # Modele danych
│   ├── TransactionModel.cs
│   ├── WalletModel.cs
│   ├── CategoryModel.cs
│   ├── ImportedTransactionModel.cs
│   └── Revolut/            # Modele dla API Revolut
├── ViewModels/             # ViewModele dla każdego widoku
│   ├── MainViewModel.cs
│   ├── WalletViewModel.cs
│   ├── TransactionsViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── GoalsViewModel.cs
│   ├── SettingsViewModel.cs
│   └── ImportTransactionsViewModel.cs
├── Views/                  # Widoki XAML
│   ├── LoginView.xaml
│   ├── DashboardView.xaml
│   ├── WalletsView.xaml
│   ├── TransactionsView.xaml
│   ├── GoalsView.xaml
│   ├── SettingsView.xaml
│   └── ImportTransactionsView.xaml
├── Services/               # Serwisy biznesowe
│   ├── RevolutService.cs
│   ├── TransactionSyncService.cs
│   ├── CsvImportService.cs
│   └── CategoryAssignmentService.cs
└── Converters/            # Konwertery dla bindingów
```

### 2.3. Warstwy aplikacji

**Warstwa prezentacji (View):**
- WPF (Windows Presentation Foundation)
- XAML do definicji interfejsu użytkownika
- Code-behind tylko dla logiki specyficznej dla UI

**Warstwa logiki (ViewModel):**
- Implementacja wzorca MVVM
- Komendy (ICommand) do obsługi akcji użytkownika
- Powiadomienia o zmianach (INotifyPropertyChanged)

**Warstwa danych (Model):**
- Modele reprezentujące encje biznesowe
- Mapowanie na tabele bazy danych

**Warstwa serwisów:**
- Logika biznesowa
- Komunikacja z API zewnętrznymi
- Parsowanie plików CSV
- Automatyczna kategoryzacja transakcji

**Warstwa dostępu do danych:**
- Bezpośredni dostęp do MySQL przez MySql.Data
- Zapytania SQL w ViewModelach i serwisach

---

## 3. Technologie i narzędzia

### 3.1. Technologie wykorzystane

- **.NET 8.0** - platforma programistyczna
- **WPF (Windows Presentation Foundation)** - framework do budowy interfejsu użytkownika
- **C#** - język programowania
- **MySQL** - system zarządzania bazą danych
- **MySql.Data** - biblioteka do komunikacji z MySQL
- **Newtonsoft.Json** - serializacja/deserializacja JSON
- **System.IdentityModel.Tokens.Jwt** - generowanie i weryfikacja tokenów JWT
- **Microsoft.Web.WebView2** - komponent do wyświetlania stron internetowych (dla OAuth Revolut)
- **LiveCharts.Wpf** - biblioteka do tworzenia wykresów

### 3.2. Narzędzia deweloperskie

- **Visual Studio 2022** - środowisko programistyczne
- **MySQL Workbench** - zarządzanie bazą danych
- **Git** - kontrola wersji

---

## 4. Struktura bazy danych

### 4.1. Diagram relacji

Baza danych składa się z następujących tabel:

```
uzytkownicy
    ├── portfele (1:N)
    │   ├── transakcje (1:N)
    │   └── skarbonki (1:N)
    ├── skarbonki (1:N)
    │   └── skarbonki_wplaty (1:N)
    ├── reguly_uzytkownika (1:N)
    └── ustawienia_uzytkownika (1:1)

kategorie (1:N)
    └── subkategorie (1:N)
        ├── lista_podstawowa_tagow (N:1)
        └── reguly_uzytkownika (N:1)
```

### 4.2. Opis tabel

**uzytkownicy**
- Przechowuje dane logowania użytkowników
- Pola: `id`, `login`, `haslo`, `data_rejestracji`

**portfele**
- Reprezentuje źródła pieniędzy (gotówka, konta bankowe)
- Pola: `id`, `id_uzytkownika`, `nazwa`, `typ` (enum: 'manualny', 'api'), `opis`, `saldo`
- Dla portfeli API: `revolut_client_id`, `revolut_private_key`, `revolut_refresh_token`, `revolut_account_id`, `last_sync_date`

**transakcje**
- Przechowuje wszystkie transakcje finansowe
- Pola: `id`, `id_portfela`, `data_transakcji`, `nazwa`, `id_kategorii`, `id_subkategorii`, `kwota`, `checkedTag`, `zewnetrzne_id`

**kategorie**
- Główne kategorie wydatków
- Pola: `id`, `typ` (enum: 'podstawowe', 'osobiste', 'oszczednosci')

**subkategorie**
- Szczegółowe podkategorie
- Pola: `id`, `id_kategorii`, `nazwa`

**reguly_uzytkownika**
- Reguły automatycznej kategoryzacji tworzone przez użytkownika
- Pola: `id`, `id_uzytkownika`, `fraza`, `id_kategorii`, `id_subkategorii`

**lista_podstawowa_tagow**
- Systemowe reguły kategoryzacji
- Pola: `id`, `fraza`, `id_kategorii`, `id_subkategorii`

**skarbonki**
- Cele oszczędnościowe użytkownika
- Pola: `id`, `id_uzytkownika`, `nazwa`, `kwota_aktualna`, `kwota_docelowa`, `cykliczne`, `typ_pobrania`, `wartosc_pobrania`, `id_portfela_zrodlowego`

**ustawienia_uzytkownika**
- Ustawienia użytkownika dotyczące automatycznej kategoryzacji
- Pola: `id`, `id_uzytkownika`, `automatyczne_tagowanie`, `nadpisywanie_tagow`, `tolerancja_procent`

---

## 5. Implementacja funkcjonalności

### 5.1. Autentykacja użytkowników

**Implementacja:** ✅ Zrealizowane

- **Logowanie** (`LoginView.xaml`)
  - Weryfikacja danych w bazie MySQL
  - Hashowanie haseł (w przyszłości zalecane użycie bcrypt/Argon2)
  - Przekierowanie do panelu głównego po zalogowaniu

- **Rejestracja** (`RegistrationView.xaml`)
  - Walidacja unikalności loginu
  - Sprawdzanie zgodności haseł
  - Tworzenie konta w bazie danych

### 5.2. Panel główny (Dashboard)

**Implementacja:** ✅ Zrealizowane

Panel główny (`DashboardView.xaml`) zawiera:

- **Sumaryczne saldo** - suma sald wszystkich portfeli użytkownika
- **Wykres słupkowy** - wydatki według kategorii w ostatnich miesiącach
  - Możliwość filtrowania według portfela
  - Biblioteka LiveCharts.Wpf
- **Wykres kołowy** - udział wydatków w miesiącu
  - Przełączanie między kategoryzacją podstawową a zaawansowaną
  - Porównanie z planowanym budżetem (w przygotowaniu)
- **Ostatnie transakcje** - lista 5 ostatnich transakcji ze wszystkich portfeli

### 5.3. Zarządzanie portfelami

**Implementacja:** ✅ Zrealizowane

**Funkcjonalności:**
- **Dodawanie portfela** (`AddWalletView.xaml`)
  - Portfel manualny: nazwa, opis, saldo początkowe
  - Portfel API: próba integracji z Revolut (szczegóły w sekcji 6)
- **Edycja portfela** (`EditWalletView.xaml`)
  - Modyfikacja nazwy, opisu, salda (tylko dla portfeli manualnych)
- **Usuwanie portfela**
  - Potwierdzenie przez użytkownika
  - Kaskadowe usuwanie transakcji
- **Lista portfeli** (`WalletsView.xaml`)
  - Tabela z informacjami o portfelach
  - Przyciski akcji zależne od typu portfela

### 5.4. Zarządzanie transakcjami

**Implementacja:** ✅ Zrealizowane

**Funkcjonalności:**
- **Dodawanie transakcji** (`AddTransactionView.xaml`)
  - Wybór portfela (tylko manualne)
  - Określenie typu (wydatek/przychód)
  - Ustawienie kategorii i subkategorii
  - Automatyczne ustawienie `checkedTag = true` dla ręcznie dodanych transakcji

- **Lista transakcji** (`TransactionsView.xaml`)
  - Filtrowanie:
    - Według portfela (wszystkie/manualne/API/konkretny portfel)
    - Checkbox "Do kategoryzacji" - tylko transakcje z `checkedTag = false`
  - Edycja kategorii i subkategorii bezpośrednio w tabeli
  - Przycisk "Potwierdź" - ustawia `checkedTag = true`
  - Usuwanie transakcji (tylko dla portfeli manualnych)

- **Automatyczna kategoryzacja**
  - Implementacja w `CategoryAssignmentService.cs`
  - Hierarchia przypisywania:
    1. Reguły użytkownika (`reguly_uzytkownika`)
    2. Reguły systemowe (`lista_podstawowa_tagow`)
    3. Domyślna kategoria (podstawowe)

### 5.5. Cele oszczędnościowe (Skarbonki)

**Implementacja:** ✅ Zrealizowane

**Funkcjonalności:**
- **Tworzenie skarbonki** (`AddGoalView.xaml`)
  - Nazwa, kwota docelowa, kwota startowa
  - Wybór portfela źródłowego
  - Ustawienia wpłat cyklicznych:
    - Typ: kwota lub procent
    - Wartość pobrania
    - Portfel źródłowy
- **Lista skarbonek** (`GoalsView.xaml`)
  - Wyświetlanie aktualnej i docelowej kwoty
  - Przycisk edycji
  - Przycisk wpłaty (ręczna wpłata)
- **Edycja skarbonki** (`EditGoalView.xaml`)
  - Modyfikacja nazwy i kwoty docelowej
  - Możliwość ręcznej wpłaty

### 5.6. Panel ustawień

**Implementacja:** ✅ Częściowo zrealizowane

**Funkcjonalności:**
- **Ustawienia automatycznej kategoryzacji** (`SettingsView.xaml`)
  - Checkbox automatycznego tagowania
  - Checkbox nadpisywania tagów
  - Pole tolerancji przypisania (procent)
  - Lista reguł użytkownika (wyświetlanie - ✅, edycja/usuwanie - w przygotowaniu)

### 5.7. Import transakcji z pliku CSV

**Implementacja:** ✅ Zrealizowane (alternatywa dla API Revolut)

**Funkcjonalności:**
- **Przycisk importu** w `WalletsView.xaml` (tylko dla portfeli manualnych)
- **Dialog wyboru pliku** CSV
- **Parsowanie pliku** (`CsvImportService.cs`)
  - Obsługa formatu CSV z cudzysłowami
  - Ekstrakcja danych: data operacji, typ transakcji, kwota, waluta, opis
  - Wyodrębnianie nazwy sklepu z opisu (regex)
  - Wyodrębnianie lokalizacji (miasto, kraj)
- **Okno importu** (`ImportTransactionsView.xaml`)
  - DataGrid z wszystkimi transakcjami
  - Automatyczne przypisanie kategorii
  - Możliwość ręcznej edycji kategorii i subkategorii
  - Checkbox "Ustaw jako regułę" - tworzy regułę użytkownika
  - Przyciski "Prześlij" i "Anuluj"
- **Zapis transakcji**
  - Wstawienie do bazy danych
  - Aktualizacja salda portfela
  - Tworzenie reguł użytkownika (jeśli zaznaczono checkbox)

**Format pliku CSV:**
```
"Data operacji","Data waluty","Typ transakcji","Kwota","Waluta","Opis transakcji",...
"2026-02-01","2026-01-30","Płatność kartą","-59.31","PLN","Tytuł: ... Lokalizacja: Adres: CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ Kraj: POLSKA",...
```

---

## 6. Problemy napotkane podczas implementacji

### 6.1. Integracja z API Revolut - Niezrealizowane

#### 6.1.1. Zakres prac wykonanych

**Autor odpowiedzialny:** Remigiusz Chodowiec

Pomimo intensywnych prac, pełna integracja z API Revolut nie została zrealizowana. Oto szczegółowy opis wykonanych prac i napotkanych problemów:

**Zaimplementowane komponenty:**

1. **RevolutService.cs** - serwis do komunikacji z API Revolut
   - Generowanie tokenów JWT (Client Assertion)
   - Metoda `ExchangeAuthCodeForRefreshToken()` - wymiana kodu autoryzacyjnego na refresh token
   - Metoda `AuthenticateAsync()` - autentykacja używając refresh token
   - Metoda `GetAccountsAsync()` - pobieranie listy kont
   - Metoda `GetTransactionsForWalletAsync()` - pobieranie transakcji dla konta

2. **RevolutAuthWindow.xaml/cs** - okno autoryzacji OAuth
   - Komponent WebView2 do wyświetlania strony logowania Revolut
   - Przechwytywanie kodu autoryzacyjnego z redirect URI
   - Obsługa zdarzeń nawigacji (NavigationStarting, NavigationCompleted, SourceChanged)
   - Wstrzykiwanie JavaScript do monitorowania zmian URL
   - Timer fallback do okresowego sprawdzania URL
   - Szczegółowe logowanie do pliku

3. **AddWalletView.xaml/cs** - formularz dodawania portfela
   - Pola do wprowadzenia Client ID i Private Key
   - Przycisk "Login and Get Token" uruchamiający proces OAuth
   - Obsługa zwracanego refresh token

4. **TransactionSyncService.cs** - serwis synchronizacji transakcji
   - Automatyczna synchronizacja co 15 minut
   - Ręczne odświeżanie przez przycisk "Odśwież"

#### 6.1.2. Napotkane problemy

**Problem 1: Biały ekran w WebView2 po autoryzacji**

**Opis:**
Po kliknięciu "Autoryzuj" w oknie logowania Revolut, okno WebView2 stawało się całkowicie białe, a kod autoryzacyjny nie był przechwytywany.

**Próby rozwiązania:**
1. Dodanie obsługi zdarzenia `NavigationCompleted`
2. Implementacja `SourceChanged` event handler
3. Wstrzykiwanie JavaScript do monitorowania zmian `window.location.href`
4. Timer fallback sprawdzający URL co 500ms
5. Zmiana logiki - zamiast anulować nawigację (`e.Cancel = true`), pozwalano na jej zakończenie

**Wynik:** Problem częściowo rozwiązany - kod był przechwytywany, ale z opóźnieniem.

**Problem 2: Błąd "Authorisation code is invalid or has expired"**

**Opis:**
Po przechwyceniu kodu autoryzacyjnego z URL `sso-confirm`, próba wymiany na refresh token kończyła się błędem:
```json
{
  "error": "invalid_request",
  "error_description": "Authorisation code is invalid or has expired. Please generate a new one following the 'Consent to the application' section in the Revolut API docs."
}
```

**Analiza:**
- Kod był przechwytywany z URL `https://sandbox-business.revolut.com/sso-confirm?code=...`
- Zgodnie z dokumentacją Revolut, kod powinien być przekierowany na `redirect_uri` (w naszym przypadku `https://www.google.com/`)
- Kod z `sso-confirm` może być kodem pośrednim, który wymaga dalszego przekierowania
- Kod autoryzacyjny ma bardzo krótki czas życia (prawdopodobnie < 10 sekund)

**Próby rozwiązania:**
1. Natychmiastowa wymiana kodu po przechwyceniu (w `NavigationStarting`)
2. Oczekiwanie na przekierowanie do `redirect_uri` przed wymianą
3. Sprawdzanie zarówno `sso-confirm` jak i `redirect_uri` w logice przechwytywania
4. Weryfikacja zgodności z oficjalną dokumentacją API Revolut

**Wynik:** Problem nie został rozwiązany. Kod nadal wygasał przed wymianą lub był nieprawidłowy.

**Problem 3: Niekompletna dokumentacja API Revolut Sandbox**

**Opis:**
Dokumentacja API Revolut dla środowiska sandbox jest niekompletna i nieprecyzyjna w następujących obszarach:
- Dokładny flow OAuth 2.0 dla aplikacji desktopowych
- Wymagania dotyczące `redirect_uri` dla aplikacji desktopowych
- Czas życia kodów autoryzacyjnych
- Różnice między `sso-confirm` a finalnym `redirect_uri`
- Obsługa JavaScript-based redirects w WebView2

**Problem 4: Ograniczenia WebView2**

**Opis:**
WebView2, pomimo że jest najnowszym komponentem Microsoft, ma pewne ograniczenia:
- Opóźnienia w przechwytywaniu zdarzeń nawigacji
- Problemy z JavaScript-based redirects (SPA routing)
- Trudności w przechwytywaniu kodów z dynamicznie zmieniających się URL-i

#### 6.1.3. Do jakiego momentu udało się dojść

**Zrealizowane:**
1. ✅ Pełna implementacja serwisu komunikacji z API Revolut
2. ✅ Generowanie poprawnych tokenów JWT (Client Assertion)
3. ✅ Okno autoryzacji OAuth z WebView2
4. ✅ Przechwytywanie kodu autoryzacyjnego z URL (częściowo)
5. ✅ Logowanie szczegółowe do pliku dla debugowania
6. ✅ Obsługa błędów i komunikatów dla użytkownika

**Niezrealizowane:**
1. ❌ Stabilne przechwytywanie kodu autoryzacyjnego
2. ❌ Pomyślna wymiana kodu na refresh token
3. ❌ Pobieranie kont użytkownika
4. ❌ Synchronizacja transakcji z konta Revolut
5. ❌ Automatyczne odświeżanie transakcji

**Logi z ostatnich prób:**
```
[2026-02-03 20:54:08.255] Wymieniam kod na token natychmiast (mamy privateKey)
[2026-02-03 20:54:09.013] Błąd wymiany kodu: Błąd wymiany kodu na token: 
{"error":"invalid_request","error_description":"Authorisation code is invalid or has expired. 
Please generate a new one following the 'Consent to the application' section in the Revolut API docs."}
```

#### 6.1.4. Możliwe przyczyny niepowodzenia

1. **Czas życia kodu autoryzacyjnego** - Kod może wygasać w ciągu kilku sekund, a opóźnienia w WebView2 uniemożliwiają natychmiastową wymianę
2. **Nieprawidłowy redirect_uri** - Możliwe, że `https://www.google.com/` nie jest akceptowany przez sandbox Revolut dla aplikacji desktopowych
3. **Różnice między sandbox a production** - Sandbox może mieć inne wymagania niż production API
4. **Brak wsparcia dla desktop OAuth flow** - Revolut API może być zaprojektowane głównie dla aplikacji webowych
5. **Problemy z WebView2** - Komponent może nie obsługiwać poprawnie wszystkich aspektów OAuth flow Revolut

### 6.2. Inne problemy techniczne

#### 6.2.1. Parsowanie CSV z cudzysłowami

**Problem:** Pliki CSV bankowe zawierają pola z przecinkami i cudzysłowami, co wymaga specjalnego parsowania.

**Rozwiązanie:** Implementacja własnego parsera CSV w `CsvImportService.cs` z obsługą:
- Cudzysłowów otaczających pola
- Podwójnych cudzysłowów jako escape
- Przecinków wewnątrz pól

#### 6.2.2. Ekstrakcja nazwy sklepu z opisu

**Problem:** Opis transakcji w CSV zawiera wiele informacji w różnych formatach.

**Rozwiązanie:** Użycie wyrażeń regularnych do wyodrębniania:
- Nazwy sklepu z "Lokalizacja: Adres: [NAZWA]"
- Miasta z "Miasto: [NAZWA]"
- Kraju z "Kraj: [NAZWA]"

#### 6.2.3. Zarządzanie cyklem życia obiektów RSA

**Problem:** `ObjectDisposedException` przy generowaniu JWT dla Revolut.

**Rozwiązanie:** Usunięcie `using` bloku wokół `RSA.Create()`, pozwalając `RsaSecurityKey` zarządzać cyklem życia obiektu.

---

## 7. Alternatywne rozwiązania

### 7.1. Import transakcji z pliku CSV

**Autor odpowiedzialny:** Remigiusz Chodowiec

W odpowiedzi na problemy z integracją API Revolut, zaimplementowano kompleksowy system importu transakcji z plików CSV.

#### 7.1.1. Funkcjonalności

1. **Parsowanie pliku CSV**
   - Obsługa standardowego formatu CSV bankowego
   - Ekstrakcja: data operacji, data waluty, typ transakcji, kwota, waluta, opis
   - Wyodrębnianie nazwy sklepu i lokalizacji z opisu

2. **Automatyczna kategoryzacja**
   - Użycie istniejącego `CategoryAssignmentService`
   - Przypisywanie kategorii na podstawie reguł użytkownika, systemowych i domyślnej
   - Możliwość ręcznej korekty przed zapisem

3. **Interfejs użytkownika**
   - Okno przeglądu wszystkich transakcji przed importem
   - Edycja kategorii i subkategorii dla każdej transakcji
   - Checkbox do automatycznego tworzenia reguł użytkownika

4. **Zapis do bazy danych**
   - Wstawienie wszystkich transakcji w transakcji bazodanowej
   - Aktualizacja salda portfela
   - Tworzenie reguł użytkownika (jeśli zaznaczono)

#### 7.1.2. Zalety rozwiązania

- ✅ **Uniwersalność** - działa z plikami CSV z dowolnego banku
- ✅ **Niezależność** - nie wymaga integracji z zewnętrznymi API
- ✅ **Kontrola użytkownika** - możliwość weryfikacji i edycji przed zapisem
- ✅ **Automatyzacja** - inteligentne przypisywanie kategorii
- ✅ **Rozszerzalność** - łatwe dodanie obsługi innych formatów CSV

#### 7.1.3. Ograniczenia

- ⚠️ Wymaga ręcznego eksportu pliku CSV z banku
- ⚠️ Nie jest automatyczne (wymaga akcji użytkownika)
- ⚠️ Może wymagać dostosowania parsera dla różnych formatów CSV

### 7.2. Przyszłe możliwości rozszerzenia

1. **Obsługa wielu formatów CSV**
   - Wykrywanie formatu automatycznie
   - Konfigurowalne mapowania kolumn

2. **Import z innych banków**
   - Rozszerzenie parsera o formaty innych banków
   - Wykrywanie banku na podstawie struktury pliku

3. **Zaplanowane importy**
   - Automatyczne sprawdzanie folderu z plikami CSV
   - Import o określonych porach

---

## 8. Podsumowanie

### 8.1. Zrealizowane funkcjonalności

✅ **Autentykacja użytkowników**
- Logowanie i rejestracja
- Walidacja danych

✅ **Zarządzanie portfelami**
- Tworzenie, edycja, usuwanie portfeli manualnych
- Próba integracji z API Revolut (niezakończona)

✅ **Zarządzanie transakcjami**
- Dodawanie, edycja, usuwanie transakcji
- Filtrowanie i wyszukiwanie
- Kategoryzacja ręczna i automatyczna

✅ **Automatyczna kategoryzacja**
- Reguły użytkownika
- Reguły systemowe
- Hierarchia przypisywania kategorii

✅ **Cele oszczędnościowe**
- Tworzenie i zarządzanie skarbonkami
- Wpłaty cykliczne
- Ręczne wpłaty

✅ **Wizualizacja danych**
- Wykresy słupkowe wydatków według kategorii
- Wykresy kołowe udziału wydatków
- Filtrowanie według portfela

✅ **Import transakcji z CSV**
- Parsowanie plików CSV
- Automatyczna kategoryzacja
- Interfejs przeglądu i edycji
- Tworzenie reguł użytkownika

### 8.2. Częściowo zrealizowane

⚠️ **Integracja z API Revolut**
- Zaimplementowane komponenty: serwis, okno autoryzacji, synchronizacja
- Problem: niestabilne przechwytywanie kodu autoryzacyjnego i wymiana na token
- Status: wstrzymane, wymaga dalszych prac lub zmiany podejścia

⚠️ **Panel ustawień**
- Wyświetlanie reguł użytkownika - ✅
- Edycja/usuwanie reguł - w przygotowaniu
- Zapisywanie ustawień do bazy - częściowo

### 8.3. Niezrealizowane funkcjonalności

❌ **Pełna integracja z API Revolut**
- Automatyczna synchronizacja transakcji z konta Revolut
- Pobieranie salda w czasie rzeczywistym

❌ **Porównanie z planowanym budżetem**
- Wykres porównawczy aktualnych vs planowanych wydatków
- Alerty o przekroczeniu budżetu

❌ **Eksport danych**
- Eksport transakcji do CSV/Excel
- Raporty finansowe

### 8.4. Wnioski

Projekt "Finansense" został w większości zrealizowany zgodnie ze specyfikacją. Głównym wyzwaniem była integracja z API Revolut, która napotkała problemy techniczne związane z OAuth 2.0 flow w aplikacji desktopowej. 

Zamiast tego zaimplementowano alternatywne rozwiązanie w postaci importu transakcji z plików CSV, które oferuje podobną funkcjonalność, choć wymaga ręcznej akcji użytkownika.

Aplikacja jest w pełni funkcjonalna dla portfeli manualnych i oferuje wszystkie kluczowe funkcjonalności zarządzania budżetem: kategoryzację transakcji, wizualizację danych, zarządzanie celami oszczędnościowymi oraz inteligentne przypisywanie kategorii na podstawie reguł.

### 8.5. Rekomendacje na przyszłość

1. **Integracja z API Revolut:**
   - Rozważenie użycia PKCE (Proof Key for Code Exchange) dla bezpieczniejszego OAuth flow
   - Przetestowanie w środowisku production (może mieć inne wymagania niż sandbox)
   - Kontakt z supportem Revolut w sprawie dokumentacji dla aplikacji desktopowych
   - Rozważenie alternatywnych bibliotek OAuth dla .NET

2. **Rozszerzenie importu CSV:**
   - Obsługa wielu formatów CSV różnych banków
   - Automatyczne wykrywanie formatu
   - Zaplanowane importy z folderu

3. **Dodatkowe funkcjonalności:**
   - Eksport danych do różnych formatów
   - Raporty finansowe
   - Porównanie z planowanym budżetem
   - Integracja z innymi bankami/API

4. **Usprawnienia techniczne:**
   - Implementacja warstwy abstrakcji dla dostępu do danych (Repository pattern)
   - Unit testy dla kluczowych komponentów
   - Lepsze zarządzanie błędami i logowanie
   - Hashowanie haseł (bcrypt/Argon2)

---

**Autorzy dokumentacji:**
- Kacper Jarzembowski
- Remigiusz Chodowiec

**Data zakończenia projektu:** Luty 2026
