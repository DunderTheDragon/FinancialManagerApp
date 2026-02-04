-- Zapytanie SQL do zmiany id_portfela z 1 na 3 TYLKO dla transakcji wstawionych z insert_transactions_history.sql
-- Uruchom to zapytanie w phpMyAdmin lub innym kliencie MySQL

-- Sprawdź ile transakcji zostanie zmienionych (opcjonalnie - przed zmianą)
-- SELECT COUNT(*) as liczba_transakcji 
-- FROM transakcje 
-- WHERE id_portfela = 1 
--   AND data_transakcji >= '2025-09-01' 
--   AND data_transakcji <= '2026-02-28';

-- Zmień id_portfela z 1 na 3 TYLKO dla transakcji z okresu wrzesień 2025 - luty 2026
-- (to są transakcje wstawione z insert_transactions_history.sql)
UPDATE transakcje 
SET id_portfela = 3 
WHERE id_portfela = 1 
  AND data_transakcji >= '2025-09-01' 
  AND data_transakcji <= '2026-02-28';

-- Sprawdź czy zmiana się powiodła
-- SELECT COUNT(*) as liczba_transakcji, 
--        MIN(data_transakcji) as najstarsza, 
--        MAX(data_transakcji) as najnowsza
-- FROM transakcje 
-- WHERE id_portfela = 3
--   AND data_transakcji >= '2025-09-01' 
--   AND data_transakcji <= '2026-02-28';

-- UWAGA: Jeśli portfel o id=3 nie istnieje, musisz go najpierw utworzyć:
-- INSERT INTO portfele (id, id_uzytkownika, nazwa, typ, opis, saldo) 
-- VALUES (3, 1, 'Gotówka', 'manualny', 'Portfel gotówkowy', 0.00);
