-- Długa historia transakcji dla użytkownika "dunder" (id=1)
-- Portfel: "mój portfel" (id=1) - używam tego jako "Gotówka"
-- Okres: wrzesień 2025 - luty 2026
-- Realistyczne wydatki używające różnych kategorii i subkategorii

-- UWAGA: Przed uruchomieniem upewnij się, że:
-- 1. Użytkownik dunder istnieje (id=1)
-- 2. Portfel id=1 istnieje
-- 3. Kategorie i subkategorie są dodane

-- WRZESIEŃ 2025
INSERT INTO transakcje (id_portfela, data_transakcji, nazwa, id_kategorii, id_subkategorii, kwota, checkedTag) VALUES
-- Podstawowe - Produkty spożywcze
(1, '2025-09-01', 'JMP S.A. BIEDRONKA 4182 Miasto: BYDGOSZCZ', 1, 1, -45.23, 0),
(1, '2025-09-03', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -78.50, 0),
(1, '2025-09-05', 'CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ', 1, 1, -125.80, 0),
(1, '2025-09-07', 'DINO TRZECIEWNICA Miasto: TRZECIEWNICA', 1, 1, -32.15, 0),
(1, '2025-09-10', 'ZABKA ZE564 Miasto: BYDGOSZCZ', 1, 1, -12.50, 0),
(1, '2025-09-12', 'JMP S.A. BIEDRONKA 5306 Miasto: BYDGOSZCZ', 1, 1, -67.90, 0),
(1, '2025-09-15', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -89.45, 0),
(1, '2025-09-18', 'CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ', 1, 1, -156.20, 0),
(1, '2025-09-20', 'JMP S.A. BIEDRONKA 4182 Miasto: BYDGOSZCZ', 1, 1, -34.67, 0),
(1, '2025-09-22', 'DINO TRZECIEWNICA Miasto: TRZECIEWNICA', 1, 1, -56.30, 0),
(1, '2025-09-25', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -98.75, 0),
(1, '2025-09-28', 'ZABKA ZE564 Miasto: BYDGOSZCZ', 1, 1, -8.99, 0),
(1, '2025-09-30', 'JMP S.A. BIEDRONKA 5306 Miasto: BYDGOSZCZ', 1, 1, -42.15, 0),

-- Podstawowe - Chemia i środki czystości
(1, '2025-09-02', 'ROSSMANN Miasto: BYDGOSZCZ', 1, 6, -45.80, 0),
(1, '2025-09-16', 'HEBE Miasto: BYDGOSZCZ', 1, 6, -38.50, 0),
(1, '2025-09-29', 'ROSSMANN Miasto: BYDGOSZCZ', 1, 6, -52.30, 0),

-- Podstawowe - Transport publiczny
(1, '2025-09-01', 'www.kupbilecik.pl', 1, 7, -45.00, 0),
(1, '2025-09-15', 'www.kupbilecik.pl', 1, 7, -45.00, 0),
(1, '2025-09-30', 'www.kupbilecik.pl', 1, 7, -45.00, 0),

-- Podstawowe - Paliwo
(1, '2025-09-04', 'ORLEN Miasto: BYDGOSZCZ', 1, 8, -180.50, 0),
(1, '2025-09-18', 'BP Miasto: BYDGOSZCZ', 1, 8, -195.00, 0),
(1, '2025-09-27', 'ORLEN Miasto: BYDGOSZCZ', 1, 8, -175.75, 0),

-- Podstawowe - Restauracje i fast food
(1, '2025-09-06', 'BD KING KEBAB Miasto: BYDGOSZCZ', 1, 9, -28.50, 0),
(1, '2025-09-11', 'MC DONALDS Miasto: BYDGOSZCZ', 1, 9, -35.90, 0),
(1, '2025-09-14', 'KFC Miasto: BYDGOSZCZ', 1, 9, -42.00, 0),
(1, '2025-09-19', 'PIZZA HUT Miasto: BYDGOSZCZ', 1, 9, -68.50, 0),
(1, '2025-09-24', 'BD KING KEBAB Miasto: BYDGOSZCZ', 1, 9, -32.00, 0),
(1, '2025-09-26', 'SUBWAY Miasto: BYDGOSZCZ', 1, 9, -28.75, 0),

-- Podstawowe - Apteka
(1, '2025-09-08', 'APTEKA POD ORLEM Miasto: BYDGOSZCZ', 1, 10, -45.60, 0),
(1, '2025-09-21', 'APTEKA POD ORLEM Miasto: BYDGOSZCZ', 1, 10, -23.40, 0),

-- Podstawowe - Komunikacja
(1, '2025-09-01', 'ORANGE - Abonament', 1, 12, -49.99, 0),
(1, '2025-09-01', 'PLAY - Internet', 1, 12, -79.00, 0),

-- Podstawowe - Media
(1, '2025-09-05', 'ENERGA - Prąd', 1, 13, -245.50, 0),
(1, '2025-09-10', 'PGNiG - Gaz', 1, 13, -180.30, 0),
(1, '2025-09-15', 'MPWiK - Woda', 1, 13, -95.20, 0),

-- Podstawowe - Czynsz
(1, '2025-09-01', 'Czynsz za mieszkanie', 1, 14, -1200.00, 0),

-- Osobiste - Rozrywka
(1, '2025-09-02', 'CINEMA CITY Miasto: BYDGOSZCZ', 2, 16, -45.00, 0),
(1, '2025-09-09', 'MULTIKINO Miasto: BYDGOSZCZ', 2, 16, -38.00, 0),
(1, '2025-09-23', 'CINEMA CITY Miasto: BYDGOSZCZ', 2, 16, -42.50, 0),

-- Osobiste - Hobby
(1, '2025-09-05', 'EMPIK Miasto: BYDGOSZCZ', 2, 17, -89.90, 0),
(1, '2025-09-13', 'SKLEP MODELARSKI Miasto: BYDGOSZCZ', 2, 17, -125.50, 0),
(1, '2025-09-25', 'EMPIK Miasto: BYDGOSZCZ', 2, 17, -67.30, 0),

-- Osobiste - Sport i fitness
(1, '2025-09-01', 'FITNESS CLUB - Karnet', 2, 18, -150.00, 0),
(1, '2025-09-08', 'DECATHLON Miasto: BYDGOSZCZ', 2, 18, -234.90, 0),
(1, '2025-09-20', 'DECATHLON Miasto: BYDGOSZCZ', 2, 18, -89.50, 0),

-- Osobiste - Książki i prasa
(1, '2025-09-07', 'EMPIK - Książki', 2, 19, -45.90, 0),
(1, '2025-09-19', 'Kiosk Ruch', 2, 19, -12.50, 0),

-- Osobiste - Filmy i seriale
(1, '2025-09-01', 'NETFLIX', 2, 20, -49.99, 0),
(1, '2025-09-01', 'HBO MAX', 2, 20, -29.99, 0),

-- Osobiste - Gry komputerowe
(1, '2025-09-10', 'STEAM - Gra', 2, 21, -89.99, 0),
(1, '2025-09-17', 'PLAYSTATION STORE', 2, 21, -159.99, 0),
(1, '2025-09-28', 'STEAM - Gra', 2, 21, -45.00, 0),

-- Osobiste - Ubrania i moda
(1, '2025-09-04', 'RESERVED Miasto: BYDGOSZCZ', 2, 22, -189.90, 0),
(1, '2025-09-12', 'H&M Miasto: BYDGOSZCZ', 2, 22, -145.50, 0),
(1, '2025-09-26', 'ZARA Miasto: BYDGOSZCZ', 2, 22, -298.00, 0),

-- Osobiste - Kosmetyki i perfumy
(1, '2025-09-06', 'SEPHORA Miasto: BYDGOSZCZ', 2, 23, -89.90, 0),
(1, '2025-09-22', 'DOUGLAS Miasto: BYDGOSZCZ', 2, 23, -125.00, 0),

-- Osobiste - Fryzjer
(1, '2025-09-11', 'SALON FRYZJERSKI Miasto: BYDGOSZCZ', 2, 24, -60.00, 0),
(1, '2025-09-27', 'SALON FRYZJERSKI Miasto: BYDGOSZCZ', 2, 24, -60.00, 0),

-- Osobiste - Elektronika
(1, '2025-09-14', 'MEDIA MARKT Miasto: BYDGOSZCZ', 2, 25, -89.90, 0),
(1, '2025-09-21', 'RTV EURO AGD Miasto: BYDGOSZCZ', 2, 25, -199.99, 0),

-- Osobiste - Restauracje (osobiste)
(1, '2025-09-03', 'RESTAURACJA STAROPOLSKA', 2, 29, -125.50, 0),
(1, '2025-09-15', 'RESTAURACJA WŁOSKA', 2, 29, -189.00, 0),
(1, '2025-09-24', 'RESTAURACJA AZJATYCKA', 2, 29, -145.75, 0),

-- Przychody
(1, '2025-09-01', 'Wypłata', 1, 1, 3500.00, 0),
(1, '2025-09-15', 'Premia', 1, 1, 500.00, 0);

-- PAŹDZIERNIK 2025
INSERT INTO transakcje (id_portfela, data_transakcji, nazwa, id_kategorii, id_subkategorii, kwota, checkedTag) VALUES
-- Podstawowe - Produkty spożywcze
(1, '2025-10-02', 'JMP S.A. BIEDRONKA 4182 Miasto: BYDGOSZCZ', 1, 1, -52.30, 0),
(1, '2025-10-04', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -87.45, 0),
(1, '2025-10-06', 'CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ', 1, 1, -134.20, 0),
(1, '2025-10-08', 'DINO TRZECIEWNICA Miasto: TRZECIEWNICA', 1, 1, -38.90, 0),
(1, '2025-10-11', 'ZABKA ZE564 Miasto: BYDGOSZCZ', 1, 1, -15.50, 0),
(1, '2025-10-13', 'JMP S.A. BIEDRONKA 5306 Miasto: BYDGOSZCZ', 1, 1, -71.20, 0),
(1, '2025-10-16', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -92.80, 0),
(1, '2025-10-19', 'CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ', 1, 1, -148.50, 0),
(1, '2025-10-21', 'JMP S.A. BIEDRONKA 4182 Miasto: BYDGOSZCZ', 1, 1, -41.75, 0),
(1, '2025-10-23', 'DINO TRZECIEWNICA Miasto: TRZECIEWNICA', 1, 1, -59.40, 0),
(1, '2025-10-26', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -105.30, 0),
(1, '2025-10-28', 'ZABKA ZE564 Miasto: BYDGOSZCZ', 1, 1, -9.99, 0),
(1, '2025-10-31', 'JMP S.A. BIEDRONKA 5306 Miasto: BYDGOSZCZ', 1, 1, -48.60, 0),

-- Podstawowe - Chemia
(1, '2025-10-03', 'ROSSMANN Miasto: BYDGOSZCZ', 1, 6, -42.90, 0),
(1, '2025-10-17', 'HEBE Miasto: BYDGOSZCZ', 1, 6, -51.20, 0),
(1, '2025-10-30', 'ROSSMANN Miasto: BYDGOSZCZ', 1, 6, -38.75, 0),

-- Podstawowe - Transport
(1, '2025-10-01', 'www.kupbilecik.pl', 1, 7, -45.00, 0),
(1, '2025-10-16', 'www.kupbilecik.pl', 1, 7, -45.00, 0),

-- Podstawowe - Paliwo
(1, '2025-10-05', 'ORLEN Miasto: BYDGOSZCZ', 1, 8, -188.50, 0),
(1, '2025-10-20', 'BP Miasto: BYDGOSZCZ', 1, 8, -192.00, 0),
(1, '2025-10-29', 'ORLEN Miasto: BYDGOSZCZ', 1, 8, -178.25, 0),

-- Podstawowe - Restauracje
(1, '2025-10-07', 'BD KING KEBAB Miasto: BYDGOSZCZ', 1, 9, -30.00, 0),
(1, '2025-10-12', 'MC DONALDS Miasto: BYDGOSZCZ', 1, 9, -38.50, 0),
(1, '2025-10-15', 'KFC Miasto: BYDGOSZCZ', 1, 9, -44.50, 0),
(1, '2025-10-22', 'PIZZA HUT Miasto: BYDGOSZCZ', 1, 9, -72.00, 0),
(1, '2025-10-25', 'BD KING KEBAB Miasto: BYDGOSZCZ', 1, 9, -28.50, 0),

-- Podstawowe - Apteka
(1, '2025-10-09', 'APTEKA POD ORLEM Miasto: BYDGOSZCZ', 1, 10, -38.90, 0),
(1, '2025-10-24', 'APTEKA POD ORLEM Miasto: BYDGOSZCZ', 1, 10, -27.60, 0),

-- Podstawowe - Komunikacja
(1, '2025-10-01', 'ORANGE - Abonament', 1, 12, -49.99, 0),
(1, '2025-10-01', 'PLAY - Internet', 1, 12, -79.00, 0),

-- Podstawowe - Media
(1, '2025-10-05', 'ENERGA - Prąd', 1, 13, -238.40, 0),
(1, '2025-10-10', 'PGNiG - Gaz', 1, 13, -175.80, 0),
(1, '2025-10-15', 'MPWiK - Woda', 1, 13, -98.50, 0),

-- Podstawowe - Czynsz
(1, '2025-10-01', 'Czynsz za mieszkanie', 1, 14, -1200.00, 0),

-- Osobiste - Rozrywka
(1, '2025-10-03', 'CINEMA CITY Miasto: BYDGOSZCZ', 2, 16, -42.00, 0),
(1, '2025-10-14', 'MULTIKINO Miasto: BYDGOSZCZ', 2, 16, -40.00, 0),
(1, '2025-10-27', 'CINEMA CITY Miasto: BYDGOSZCZ', 2, 16, -45.50, 0),

-- Osobiste - Hobby
(1, '2025-10-06', 'EMPIK Miasto: BYDGOSZCZ', 2, 17, -95.90, 0),
(1, '2025-10-18', 'SKLEP MODELARSKI Miasto: BYDGOSZCZ', 2, 17, -142.50, 0),

-- Osobiste - Sport
(1, '2025-10-01', 'FITNESS CLUB - Karnet', 2, 18, -150.00, 0),
(1, '2025-10-14', 'DECATHLON Miasto: BYDGOSZCZ', 2, 18, -198.90, 0),

-- Osobiste - Filmy i seriale
(1, '2025-10-01', 'NETFLIX', 2, 20, -49.99, 0),
(1, '2025-10-01', 'HBO MAX', 2, 20, -29.99, 0),

-- Osobiste - Gry
(1, '2025-10-11', 'STEAM - Gra', 2, 21, -125.99, 0),
(1, '2025-10-25', 'PLAYSTATION STORE', 2, 21, -179.99, 0),

-- Osobiste - Ubrania
(1, '2025-10-05', 'RESERVED Miasto: BYDGOSZCZ', 2, 22, -165.90, 0),
(1, '2025-10-19', 'H&M Miasto: BYDGOSZCZ', 2, 22, -158.50, 0),

-- Osobiste - Kosmetyki
(1, '2025-10-08', 'SEPHORA Miasto: BYDGOSZCZ', 2, 23, -95.90, 0),

-- Osobiste - Fryzjer
(1, '2025-10-12', 'SALON FRYZJERSKI Miasto: BYDGOSZCZ', 2, 24, -60.00, 0),

-- Osobiste - Elektronika
(1, '2025-10-16', 'MEDIA MARKT Miasto: BYDGOSZCZ', 2, 25, -129.90, 0),

-- Osobiste - Restauracje
(1, '2025-10-04', 'RESTAURACJA STAROPOLSKA', 2, 29, -135.50, 0),
(1, '2025-10-18', 'RESTAURACJA WŁOSKA', 2, 29, -195.00, 0),
(1, '2025-10-28', 'RESTAURACJA AZJATYCKA', 2, 29, -158.75, 0),

-- Przychody
(1, '2025-10-01', 'Wypłata', 1, 1, 3500.00, 0),
(1, '2025-10-15', 'Premia', 1, 1, 500.00, 0);

-- LISTOPAD 2025
INSERT INTO transakcje (id_portfela, data_transakcji, nazwa, id_kategorii, id_subkategorii, kwota, checkedTag) VALUES
-- Podstawowe - Produkty spożywcze
(1, '2025-11-01', 'JMP S.A. BIEDRONKA 4182 Miasto: BYDGOSZCZ', 1, 1, -48.90, 0),
(1, '2025-11-03', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -91.20, 0),
(1, '2025-11-05', 'CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ', 1, 1, -142.60, 0),
(1, '2025-11-07', 'DINO TRZECIEWNICA Miasto: TRZECIEWNICA', 1, 1, -35.40, 0),
(1, '2025-11-10', 'ZABKA ZE564 Miasto: BYDGOSZCZ', 1, 1, -13.99, 0),
(1, '2025-11-12', 'JMP S.A. BIEDRONKA 5306 Miasto: BYDGOSZCZ', 1, 1, -68.50, 0),
(1, '2025-11-15', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -96.80, 0),
(1, '2025-11-18', 'CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ', 1, 1, -152.30, 0),
(1, '2025-11-20', 'JMP S.A. BIEDRONKA 4182 Miasto: BYDGOSZCZ', 1, 1, -44.25, 0),
(1, '2025-11-22', 'DINO TRZECIEWNICA Miasto: TRZECIEWNICA', 1, 1, -61.90, 0),
(1, '2025-11-25', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -108.40, 0),
(1, '2025-11-27', 'ZABKA ZE564 Miasto: BYDGOSZCZ', 1, 1, -11.50, 0),
(1, '2025-11-30', 'JMP S.A. BIEDRONKA 5306 Miasto: BYDGOSZCZ', 1, 1, -51.80, 0),

-- Podstawowe - Chemia
(1, '2025-11-02', 'ROSSMANN Miasto: BYDGOSZCZ', 1, 6, -48.20, 0),
(1, '2025-11-16', 'HEBE Miasto: BYDGOSZCZ', 1, 6, -44.50, 0),
(1, '2025-11-29', 'ROSSMANN Miasto: BYDGOSZCZ', 1, 6, -41.30, 0),

-- Podstawowe - Transport
(1, '2025-11-01', 'www.kupbilecik.pl', 1, 7, -45.00, 0),
(1, '2025-11-16', 'www.kupbilecik.pl', 1, 7, -45.00, 0),

-- Podstawowe - Paliwo
(1, '2025-11-04', 'ORLEN Miasto: BYDGOSZCZ', 1, 8, -185.50, 0),
(1, '2025-11-19', 'BP Miasto: BYDGOSZCZ', 1, 8, -190.00, 0),
(1, '2025-11-28', 'ORLEN Miasto: BYDGOSZCZ', 1, 8, -182.75, 0),

-- Podstawowe - Restauracje
(1, '2025-11-06', 'BD KING KEBAB Miasto: BYDGOSZCZ', 1, 9, -32.50, 0),
(1, '2025-11-11', 'MC DONALDS Miasto: BYDGOSZCZ', 1, 9, -41.90, 0),
(1, '2025-11-14', 'KFC Miasto: BYDGOSZCZ', 1, 9, -46.00, 0),
(1, '2025-11-21', 'PIZZA HUT Miasto: BYDGOSZCZ', 1, 9, -75.50, 0),
(1, '2025-11-24', 'BD KING KEBAB Miasto: BYDGOSZCZ', 1, 9, -29.00, 0),
(1, '2025-11-26', 'SUBWAY Miasto: BYDGOSZCZ', 1, 9, -31.25, 0),

-- Podstawowe - Apteka
(1, '2025-11-08', 'APTEKA POD ORLEM Miasto: BYDGOSZCZ', 1, 10, -42.60, 0),
(1, '2025-11-23', 'APTEKA POD ORLEM Miasto: BYDGOSZCZ', 1, 10, -29.80, 0),

-- Podstawowe - Komunikacja
(1, '2025-11-01', 'ORANGE - Abonament', 1, 12, -49.99, 0),
(1, '2025-11-01', 'PLAY - Internet', 1, 12, -79.00, 0),

-- Podstawowe - Media
(1, '2025-11-05', 'ENERGA - Prąd', 1, 13, -252.60, 0),
(1, '2025-11-10', 'PGNiG - Gaz', 1, 13, -188.90, 0),
(1, '2025-11-15', 'MPWiK - Woda', 1, 13, -97.80, 0),

-- Podstawowe - Czynsz
(1, '2025-11-01', 'Czynsz za mieszkanie', 1, 14, -1200.00, 0),

-- Osobiste - Rozrywka
(1, '2025-11-02', 'CINEMA CITY Miasto: BYDGOSZCZ', 2, 16, -44.00, 0),
(1, '2025-11-13', 'MULTIKINO Miasto: BYDGOSZCZ', 2, 16, -38.50, 0),
(1, '2025-11-28', 'CINEMA CITY Miasto: BYDGOSZCZ', 2, 16, -43.00, 0),

-- Osobiste - Hobby
(1, '2025-11-05', 'EMPIK Miasto: BYDGOSZCZ', 2, 17, -102.90, 0),
(1, '2025-11-17', 'SKLEP MODELARSKI Miasto: BYDGOSZCZ', 2, 17, -138.50, 0),
(1, '2025-11-29', 'EMPIK Miasto: BYDGOSZCZ', 2, 17, -72.30, 0),

-- Osobiste - Sport
(1, '2025-11-01', 'FITNESS CLUB - Karnet', 2, 18, -150.00, 0),
(1, '2025-11-15', 'DECATHLON Miasto: BYDGOSZCZ', 2, 18, -215.90, 0),

-- Osobiste - Filmy i seriale
(1, '2025-11-01', 'NETFLIX', 2, 20, -49.99, 0),
(1, '2025-11-01', 'HBO MAX', 2, 20, -29.99, 0),

-- Osobiste - Gry
(1, '2025-11-09', 'STEAM - Gra', 2, 21, -95.99, 0),
(1, '2025-11-22', 'PLAYSTATION STORE', 2, 21, -199.99, 0),

-- Osobiste - Ubrania
(1, '2025-11-03', 'RESERVED Miasto: BYDGOSZCZ', 2, 22, -178.90, 0),
(1, '2025-11-18', 'H&M Miasto: BYDGOSZCZ', 2, 22, -162.50, 0),
(1, '2025-11-27', 'ZARA Miasto: BYDGOSZCZ', 2, 22, -312.00, 0),

-- Osobiste - Kosmetyki
(1, '2025-11-07', 'SEPHORA Miasto: BYDGOSZCZ', 2, 23, -98.90, 0),
(1, '2025-11-21', 'DOUGLAS Miasto: BYDGOSZCZ', 2, 23, -132.00, 0),

-- Osobiste - Fryzjer
(1, '2025-11-10', 'SALON FRYZJERSKI Miasto: BYDGOSZCZ', 2, 24, -60.00, 0),

-- Osobiste - Elektronika
(1, '2025-11-13', 'MEDIA MARKT Miasto: BYDGOSZCZ', 2, 25, -145.90, 0),
(1, '2025-11-25', 'RTV EURO AGD Miasto: BYDGOSZCZ', 2, 25, -225.99, 0),

-- Osobiste - Restauracje
(1, '2025-11-04', 'RESTAURACJA STAROPOLSKA', 2, 29, -142.50, 0),
(1, '2025-11-17', 'RESTAURACJA WŁOSKA', 2, 29, -201.00, 0),
(1, '2025-11-30', 'RESTAURACJA AZJATYCKA', 2, 29, -165.75, 0),

-- Przychody
(1, '2025-11-01', 'Wypłata', 1, 1, 3500.00, 0),
(1, '2025-11-15', 'Premia', 1, 1, 500.00, 0);

-- GRUDZIEŃ 2025
INSERT INTO transakcje (id_portfela, data_transakcji, nazwa, id_kategorii, id_subkategorii, kwota, checkedTag) VALUES
-- Podstawowe - Produkty spożywcze
(1, '2025-12-01', 'JMP S.A. BIEDRONKA 4182 Miasto: BYDGOSZCZ', 1, 1, -55.40, 0),
(1, '2025-12-03', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -98.60, 0),
(1, '2025-12-05', 'CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ', 1, 1, -168.90, 0),
(1, '2025-12-07', 'DINO TRZECIEWNICA Miasto: TRZECIEWNICA', 1, 1, -42.80, 0),
(1, '2025-12-10', 'ZABKA ZE564 Miasto: BYDGOSZCZ', 1, 1, -16.50, 0),
(1, '2025-12-12', 'JMP S.A. BIEDRONKA 5306 Miasto: BYDGOSZCZ', 1, 1, -75.20, 0),
(1, '2025-12-15', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -112.40, 0),
(1, '2025-12-18', 'CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ', 1, 1, -189.50, 0),
(1, '2025-12-20', 'JMP S.A. BIEDRONKA 4182 Miasto: BYDGOSZCZ', 1, 1, -52.90, 0),
(1, '2025-12-22', 'DINO TRZECIEWNICA Miasto: TRZECIEWNICA', 1, 1, -68.30, 0),
(1, '2025-12-23', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -145.80, 0), -- Wigilia
(1, '2025-12-27', 'ZABKA ZE564 Miasto: BYDGOSZCZ', 1, 1, -12.99, 0),
(1, '2025-12-30', 'JMP S.A. BIEDRONKA 5306 Miasto: BYDGOSZCZ', 1, 1, -58.40, 0),

-- Podstawowe - Chemia
(1, '2025-12-02', 'ROSSMANN Miasto: BYDGOSZCZ', 1, 6, -52.40, 0),
(1, '2025-12-16', 'HEBE Miasto: BYDGOSZCZ', 1, 6, -48.90, 0),
(1, '2025-12-29', 'ROSSMANN Miasto: BYDGOSZCZ', 1, 6, -45.60, 0),

-- Podstawowe - Transport
(1, '2025-12-01', 'www.kupbilecik.pl', 1, 7, -45.00, 0),
(1, '2025-12-16', 'www.kupbilecik.pl', 1, 7, -45.00, 0),

-- Podstawowe - Paliwo
(1, '2025-12-04', 'ORLEN Miasto: BYDGOSZCZ', 1, 8, -192.50, 0),
(1, '2025-12-19', 'BP Miasto: BYDGOSZCZ', 1, 8, -198.00, 0),
(1, '2025-12-28', 'ORLEN Miasto: BYDGOSZCZ', 1, 8, -185.25, 0),

-- Podstawowe - Restauracje
(1, '2025-12-06', 'BD KING KEBAB Miasto: BYDGOSZCZ', 1, 9, -35.00, 0),
(1, '2025-12-11', 'MC DONALDS Miasto: BYDGOSZCZ', 1, 9, -44.90, 0),
(1, '2025-12-14', 'KFC Miasto: BYDGOSZCZ', 1, 9, -48.50, 0),
(1, '2025-12-21', 'PIZZA HUT Miasto: BYDGOSZCZ', 1, 9, -82.00, 0),
(1, '2025-12-24', 'RESTAURACJA ŚWIĄTECZNA', 1, 9, -250.00, 0), -- Wigilia
(1, '2025-12-26', 'BD KING KEBAB Miasto: BYDGOSZCZ', 1, 9, -31.50, 0),

-- Podstawowe - Apteka
(1, '2025-12-08', 'APTEKA POD ORLEM Miasto: BYDGOSZCZ', 1, 10, -48.20, 0),
(1, '2025-12-23', 'APTEKA POD ORLEM Miasto: BYDGOSZCZ', 1, 10, -35.40, 0),

-- Podstawowe - Komunikacja
(1, '2025-12-01', 'ORANGE - Abonament', 1, 12, -49.99, 0),
(1, '2025-12-01', 'PLAY - Internet', 1, 12, -79.00, 0),

-- Podstawowe - Media
(1, '2025-12-05', 'ENERGA - Prąd', 1, 13, -268.40, 0),
(1, '2025-12-10', 'PGNiG - Gaz', 1, 13, -195.60, 0),
(1, '2025-12-15', 'MPWiK - Woda', 1, 13, -99.20, 0),

-- Podstawowe - Czynsz
(1, '2025-12-01', 'Czynsz za mieszkanie', 1, 14, -1200.00, 0),

-- Osobiste - Rozrywka
(1, '2025-12-02', 'CINEMA CITY Miasto: BYDGOSZCZ', 2, 16, -46.00, 0),
(1, '2025-12-13', 'MULTIKINO Miasto: BYDGOSZCZ', 2, 16, -42.00, 0),
(1, '2025-12-28', 'CINEMA CITY Miasto: BYDGOSZCZ', 2, 16, -44.50, 0),

-- Osobiste - Hobby
(1, '2025-12-05', 'EMPIK Miasto: BYDGOSZCZ', 2, 17, -115.90, 0),
(1, '2025-12-17', 'SKLEP MODELARSKI Miasto: BYDGOSZCZ', 2, 17, -152.50, 0),
(1, '2025-12-22', 'EMPIK - Prezenty', 2, 17, -245.30, 0), -- Prezenty świąteczne

-- Osobiste - Sport
(1, '2025-12-01', 'FITNESS CLUB - Karnet', 2, 18, -150.00, 0),
(1, '2025-12-15', 'DECATHLON Miasto: BYDGOSZCZ', 2, 18, -198.90, 0),

-- Osobiste - Filmy i seriale
(1, '2025-12-01', 'NETFLIX', 2, 20, -49.99, 0),
(1, '2025-12-01', 'HBO MAX', 2, 20, -29.99, 0),

-- Osobiste - Gry
(1, '2025-12-09', 'STEAM - Gra', 2, 21, -125.99, 0),
(1, '2025-12-22', 'PLAYSTATION STORE - Prezent', 2, 21, -299.99, 0), -- Prezent

-- Osobiste - Ubrania
(1, '2025-12-03', 'RESERVED Miasto: BYDGOSZCZ', 2, 22, -189.90, 0),
(1, '2025-12-18', 'H&M Miasto: BYDGOSZCZ', 2, 22, -178.50, 0),
(1, '2025-12-21', 'ZARA Miasto: BYDGOSZCZ', 2, 22, -425.00, 0), -- Prezenty

-- Osobiste - Kosmetyki
(1, '2025-12-07', 'SEPHORA Miasto: BYDGOSZCZ', 2, 23, -105.90, 0),
(1, '2025-12-20', 'DOUGLAS Miasto: BYDGOSZCZ', 2, 23, -158.00, 0), -- Prezenty

-- Osobiste - Fryzjer
(1, '2025-12-11', 'SALON FRYZJERSKI Miasto: BYDGOSZCZ', 2, 24, -60.00, 0),

-- Osobiste - Elektronika
(1, '2025-12-13', 'MEDIA MARKT Miasto: BYDGOSZCZ', 2, 25, -189.90, 0),
(1, '2025-12-24', 'RTV EURO AGD - Prezent', 2, 25, -599.99, 0), -- Prezent świąteczny

-- Osobiste - Prezenty
(1, '2025-12-22', 'Prezenty świąteczne', 2, 26, -450.00, 0),
(1, '2025-12-23', 'Prezenty świąteczne', 2, 26, -320.00, 0),

-- Osobiste - Restauracje
(1, '2025-12-04', 'RESTAURACJA STAROPOLSKA', 2, 29, -155.50, 0),
(1, '2025-12-17', 'RESTAURACJA WŁOSKA', 2, 29, -215.00, 0),
(1, '2025-12-31', 'RESTAURACJA NOWOROCZNA', 2, 29, -280.00, 0), -- Sylwester

-- Przychody
(1, '2025-12-01', 'Wypłata', 1, 1, 3500.00, 0),
(1, '2025-12-15', 'Premia świąteczna', 1, 1, 1000.00, 0);

-- STYCZEŃ 2026
INSERT INTO transakcje (id_portfela, data_transakcji, nazwa, id_kategorii, id_subkategorii, kwota, checkedTag) VALUES
-- Podstawowe - Produkty spożywcze
(1, '2026-01-02', 'JMP S.A. BIEDRONKA 4182 Miasto: BYDGOSZCZ', 1, 1, -51.20, 0),
(1, '2026-01-04', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -94.80, 0),
(1, '2026-01-06', 'CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ', 1, 1, -158.40, 0),
(1, '2026-01-08', 'DINO TRZECIEWNICA Miasto: TRZECIEWNICA', 1, 1, -40.60, 0),
(1, '2026-01-11', 'ZABKA ZE564 Miasto: BYDGOSZCZ', 1, 1, -14.99, 0),
(1, '2026-01-13', 'JMP S.A. BIEDRONKA 5306 Miasto: BYDGOSZCZ', 1, 1, -72.80, 0),
(1, '2026-01-16', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -108.20, 0),
(1, '2026-01-18', 'CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ', 1, 1, -175.60, 0),
(1, '2026-01-20', 'JMP S.A. BIEDRONKA 4182 Miasto: BYDGOSZCZ', 1, 1, -49.50, 0),
(1, '2026-01-22', 'DINO TRZECIEWNICA Miasto: TRZECIEWNICA', 1, 1, -65.40, 0),
(1, '2026-01-25', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -132.60, 0),
(1, '2026-01-27', 'ZABKA ZE564 Miasto: BYDGOSZCZ', 1, 1, -11.50, 0),
(1, '2026-01-30', 'JMP S.A. BIEDRONKA 5306 Miasto: BYDGOSZCZ', 1, 1, -55.90, 0),

-- Podstawowe - Chemia
(1, '2026-01-03', 'ROSSMANN Miasto: BYDGOSZCZ', 1, 6, -50.60, 0),
(1, '2026-01-17', 'HEBE Miasto: BYDGOSZCZ', 1, 6, -47.20, 0),
(1, '2026-01-29', 'ROSSMANN Miasto: BYDGOSZCZ', 1, 6, -43.80, 0),

-- Podstawowe - Transport
(1, '2026-01-01', 'www.kupbilecik.pl', 1, 7, -45.00, 0),
(1, '2026-01-16', 'www.kupbilecik.pl', 1, 7, -45.00, 0),

-- Podstawowe - Paliwo
(1, '2026-01-05', 'ORLEN Miasto: BYDGOSZCZ', 1, 8, -190.50, 0),
(1, '2026-01-19', 'BP Miasto: BYDGOSZCZ', 1, 8, -196.00, 0),
(1, '2026-01-28', 'ORLEN Miasto: BYDGOSZCZ', 1, 8, -184.25, 0),

-- Podstawowe - Restauracje
(1, '2026-01-07', 'BD KING KEBAB Miasto: BYDGOSZCZ', 1, 9, -33.50, 0),
(1, '2026-01-12', 'MC DONALDS Miasto: BYDGOSZCZ', 1, 9, -43.90, 0),
(1, '2026-01-15', 'KFC Miasto: BYDGOSZCZ', 1, 9, -47.50, 0),
(1, '2026-01-21', 'PIZZA HUT Miasto: BYDGOSZCZ', 1, 9, -78.00, 0),
(1, '2026-01-24', 'BD KING KEBAB Miasto: BYDGOSZCZ', 1, 9, -30.00, 0),
(1, '2026-01-26', 'SUBWAY Miasto: BYDGOSZCZ', 1, 9, -32.75, 0),

-- Podstawowe - Apteka
(1, '2026-01-09', 'APTEKA POD ORLEM Miasto: BYDGOSZCZ', 1, 10, -46.80, 0),
(1, '2026-01-23', 'APTEKA POD ORLEM Miasto: BYDGOSZCZ', 1, 10, -33.20, 0),

-- Podstawowe - Komunikacja
(1, '2026-01-01', 'ORANGE - Abonament', 1, 12, -49.99, 0),
(1, '2026-01-01', 'PLAY - Internet', 1, 12, -79.00, 0),

-- Podstawowe - Media
(1, '2026-01-05', 'ENERGA - Prąd', 1, 13, -262.40, 0),
(1, '2026-01-10', 'PGNiG - Gaz', 1, 13, -192.60, 0),
(1, '2026-01-15', 'MPWiK - Woda', 1, 13, -98.20, 0),

-- Podstawowe - Czynsz
(1, '2026-01-01', 'Czynsz za mieszkanie', 1, 14, -1200.00, 0),

-- Osobiste - Rozrywka
(1, '2026-01-03', 'CINEMA CITY Miasto: BYDGOSZCZ', 2, 16, -45.00, 0),
(1, '2026-01-14', 'MULTIKINO Miasto: BYDGOSZCZ', 2, 16, -41.00, 0),
(1, '2026-01-28', 'CINEMA CITY Miasto: BYDGOSZCZ', 2, 16, -43.50, 0),

-- Osobiste - Hobby
(1, '2026-01-06', 'EMPIK Miasto: BYDGOSZCZ', 2, 17, -108.90, 0),
(1, '2026-01-18', 'SKLEP MODELARSKI Miasto: BYDGOSZCZ', 2, 17, -145.50, 0),
(1, '2026-01-30', 'EMPIK Miasto: BYDGOSZCZ', 2, 17, -78.30, 0),

-- Osobiste - Sport
(1, '2026-01-01', 'FITNESS CLUB - Karnet', 2, 18, -150.00, 0),
(1, '2026-01-15', 'DECATHLON Miasto: BYDGOSZCZ', 2, 18, -205.90, 0),

-- Osobiste - Filmy i seriale
(1, '2026-01-01', 'NETFLIX', 2, 20, -49.99, 0),
(1, '2026-01-01', 'HBO MAX', 2, 20, -29.99, 0),

-- Osobiste - Gry
(1, '2026-01-10', 'STEAM - Gra', 2, 21, -115.99, 0),
(1, '2026-01-23', 'PLAYSTATION STORE', 2, 21, -189.99, 0),

-- Osobiste - Ubrania
(1, '2026-01-04', 'RESERVED Miasto: BYDGOSZCZ', 2, 22, -175.90, 0),
(1, '2026-01-19', 'H&M Miasto: BYDGOSZCZ', 2, 22, -168.50, 0),
(1, '2026-01-29', 'ZARA Miasto: BYDGOSZCZ', 2, 22, -298.00, 0),

-- Osobiste - Kosmetyki
(1, '2026-01-08', 'SEPHORA Miasto: BYDGOSZCZ', 2, 23, -102.90, 0),
(1, '2026-01-22', 'DOUGLAS Miasto: BYDGOSZCZ', 2, 23, -142.00, 0),

-- Osobiste - Fryzjer
(1, '2026-01-12', 'SALON FRYZJERSKI Miasto: BYDGOSZCZ', 2, 24, -60.00, 0),

-- Osobiste - Elektronika
(1, '2026-01-14', 'MEDIA MARKT Miasto: BYDGOSZCZ', 2, 25, -165.90, 0),
(1, '2026-01-26', 'RTV EURO AGD Miasto: BYDGOSZCZ', 2, 25, -245.99, 0),

-- Osobiste - Restauracje
(1, '2026-01-05', 'RESTAURACJA STAROPOLSKA', 2, 29, -148.50, 0),
(1, '2026-01-18', 'RESTAURACJA WŁOSKA', 2, 29, -208.00, 0),
(1, '2026-01-31', 'RESTAURACJA AZJATYCKA', 2, 29, -172.75, 0),

-- Przychody
(1, '2026-01-01', 'Wypłata', 1, 1, 3500.00, 0),
(1, '2026-01-15', 'Premia', 1, 1, 500.00, 0);

-- LUTY 2026 (kilka transakcji)
INSERT INTO transakcje (id_portfela, data_transakcji, nazwa, id_kategorii, id_subkategorii, kwota, checkedTag) VALUES
-- Podstawowe - Produkty spożywcze
(1, '2026-02-01', 'JMP S.A. BIEDRONKA 4182 Miasto: BYDGOSZCZ', 1, 1, -53.40, 0),
(1, '2026-02-03', 'LIDL POMORSKA Miasto: BYDGOSZCZ', 1, 1, -96.20, 0),
(1, '2026-02-05', 'CARREFOUR SUPERMARKET Miasto: BYDGOSZCZ', 1, 1, -162.80, 0),

-- Podstawowe - Restauracje
(1, '2026-02-02', 'BD KING KEBAB Miasto: BYDGOSZCZ', 1, 9, -34.50, 0),
(1, '2026-02-04', 'MC DONALDS Miasto: BYDGOSZCZ', 1, 9, -45.90, 0),

-- Podstawowe - Komunikacja
(1, '2026-02-01', 'ORANGE - Abonament', 1, 12, -49.99, 0),
(1, '2026-02-01', 'PLAY - Internet', 1, 12, -79.00, 0),

-- Podstawowe - Media
(1, '2026-02-05', 'ENERGA - Prąd', 1, 13, -258.40, 0),

-- Podstawowe - Czynsz
(1, '2026-02-01', 'Czynsz za mieszkanie', 1, 14, -1200.00, 0),

-- Osobiste - Rozrywka
(1, '2026-02-03', 'CINEMA CITY Miasto: BYDGOSZCZ', 2, 16, -46.00, 0),

-- Osobiste - Filmy i seriale
(1, '2026-02-01', 'NETFLIX', 2, 20, -49.99, 0),
(1, '2026-02-01', 'HBO MAX', 2, 20, -29.99, 0),

-- Osobiste - Sport
(1, '2026-02-01', 'FITNESS CLUB - Karnet', 2, 18, -150.00, 0),

-- Przychody
(1, '2026-02-01', 'Wypłata', 1, 1, 3500.00, 0);

-- Sprawdź ile transakcji zostało dodanych
-- SELECT COUNT(*) as liczba_transakcji, 
--        MIN(data_transakcji) as najstarsza, 
--        MAX(data_transakcji) as najnowsza,
--        SUM(kwota) as suma_kwot
-- FROM transakcje 
-- WHERE id_portfela = 1;
