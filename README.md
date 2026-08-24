# AuthService

AuthService to aplikacja full-stack do obslugi uwierzytelniania, autoryzacji oraz zarzadzania uzytkownikami. Projekt sklada sie z backendu REST API w ASP.NET Core oraz panelu administracyjnego w Angularze z mozliwoscia uruchomienia jako aplikacja desktopowa przez Electron.

Celem projektu jest zbudowanie kompletnego modulu autoryzacyjnego, ktory pokazuje praktyczne uzycie JWT, refresh tokenow, rol, uprawnien, zabezpieczonych endpointow oraz testow jednostkowych i integracyjnych.

## Najwazniejsze funkcje

- Rejestracja i logowanie uzytkownikow
- Logowanie administratora
- Obsluga JWT access tokenow oraz refresh tokenow
- Przechowywanie tokenow w bezpiecznych ciasteczkach HTTP-only
- Odswiezanie sesji uzytkownika
- Wylogowanie z uniewaznieniem sesji
- Zmiana hasla uzytkownika
- Role i uprawnienia uzytkownikow
- Panel administracyjny do zarzadzania uzytkownikami, rolami i uprawnieniami
- Ochrona endpointow przez polityki autoryzacyjne
- Automatyczne seedowanie danych startowych
- Dokumentacja API przez Swagger/OpenAPI
- Testy jednostkowe i integracyjne backendu

## Technologie

### Backend

- C# / ASP.NET Core
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- BCrypt do hashowania hasel
- Swagger / OpenAPI
- xUnit

### Frontend

- Angular
- Angular Material
- TypeScript
- RxJS
- Electron
- Vitest

## Architektura

Projekt jest podzielony na dwie glowne czesci:

```text
Backend/
  src/AuthServer/        REST API, logika autoryzacji, baza danych
  tests/AuthServerTests/ testy jednostkowe i integracyjne

Frontend/
  src/                   aplikacja Angular + konfiguracja Electron
```

Backend korzysta z repozytoriow dla operacji na danych oraz osobnych serwisow odpowiedzialnych za generowanie JWT i obsluge refresh tokenow. Dostep administracyjny jest zabezpieczony polityka wymagajaca uprawnienia `Full`.

Frontend udostepnia ekran logowania oraz panel administracyjny, komunikujac sie z API przez dedykowane serwisy Angulara i interceptor autoryzacyjny.

## Bezpieczenstwo

W projekcie zastosowano kilka mechanizmow bezpieczenstwa:

- Hasla przechowywane jako hash BCrypt
- Access tokeny JWT z walidacja issuer, audience, lifetime i signing key
- Refresh tokeny zapisywane po stronie serwera
- Ciasteczka `HttpOnly`, `SameSite=Strict` oraz `Secure` poza srodowiskiem developerskim
- Uniewaznianie sesji przez `SessionVersion`
- Blokada usuwania wbudowanego administratora oraz kluczowych uprawnien

## Testy

Backend posiada testy jednostkowe dla kontrolerow oraz testy integracyjne sprawdzajace zachowanie endpointow, w tym scenariusze nieautoryzowanego dostepu.

Przykladowe uruchomienie testow backendu:

```bash
dotnet test
```

Testy frontendu:

```bash
npm test
```

## Uruchomienie projektu

### Backend

W katalogu backendu nalezy skonfigurowac connection string do SQL Server oraz ustawienia JWT, np. w `appsettings.Development.json` albo przez zmienne srodowiskowe.

Wymagane ustawienia JWT:

```text
Jwt:AuthServiceKey
Jwt:Issuer
Jwt:Audience
```

Uruchomienie API:

```bash
dotnet run --project Backend/src/AuthServer/AuthServer.csproj
```

W srodowisku developerskim dostepny jest Swagger UI.

### Frontend

Instalacja zaleznosci:

```bash
cd Frontend/src
npm install
```

Uruchomienie aplikacji webowej:

```bash
npm start
```

Uruchomienie wersji Electron w trybie developerskim:

```bash
npm run electron:dev
```
