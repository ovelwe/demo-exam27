USE DemoExam2027;
GO

-- Резервный вариант импорта именно для приложенного Заказчики.json.
-- На экзамене предпочтительнее использовать кнопку "Импорт JSON" в приложении,
-- потому что она работает с любым файлом той же структуры.

MERGE dbo.Counterparties AS target
USING (VALUES
(N'000000001', N'ООО "Поставка"', NULL, N'г.Пятигорск', N'+79198634592', N'Поставщик'),
(N'000000002', N'ООО "Кинотеатр Квант"', N'26320045123', N'г. Железноводск, ул. Мира, 123', N'+79884581555', N'Покупатель'),
(N'000000008', N'ООО "Новый JDTO"', N'26320045111', N'г. Железноводсу', N'+79884581555', N'Покупатель'),
(N'000000003', N'ООО "Ромашка"', N'4140784214', N'г. Омск, ул. Строителей, 294', N'+79882584546', N'Поставщик'),
(N'000000009', N'ООО "Ипподром"', N'5874045632', N'г. Уфа, ул. Набережная,  37', N'+79627486389', N'Поставщик'),
(N'000000010', N'ООО "Ассоль"', N'2629011278', N'г. Калуга, ул. Пушкина, 94', N'+79184572398', N'Покупатель')
) AS src(ExternalId, Name, Inn, Address, Phone, CounterpartyType)
ON target.ExternalId = src.ExternalId
WHEN MATCHED THEN UPDATE SET
    Name=src.Name, Inn=src.Inn, Address=src.Address, Phone=src.Phone, CounterpartyType=src.CounterpartyType
WHEN NOT MATCHED THEN INSERT(ExternalId, Name, Inn, Address, Phone, CounterpartyType)
VALUES(src.ExternalId, src.Name, src.Inn, src.Address, src.Phone, src.CounterpartyType);
GO
