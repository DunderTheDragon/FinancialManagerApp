-- Dodanie kategorii "przychód" do bazy danych
-- Ta kategoria będzie używana tylko dla transakcji dodatnich (przychodów)
-- i nie będzie wyświetlana jako opcja przy transakcjach ujemnych

-- Sprawdź czy kategoria już istnieje
-- SELECT * FROM kategorie WHERE typ = 'przychód';

-- Dodaj kategorię "przychód" (id=4)
-- Uwaga: Jeśli id=4 jest już zajęte, zmień na inne dostępne id
INSERT INTO kategorie (id, typ) VALUES (4, 'przychód')
ON DUPLICATE KEY UPDATE typ = 'przychód';

-- Sprawdź czy dodano poprawnie
-- SELECT * FROM kategorie ORDER BY id;
