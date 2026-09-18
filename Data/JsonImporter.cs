using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DemoExam.Models;
using Microsoft.Data.SqlClient;

namespace DemoExam.Data
{
    public static class JsonImporter
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static int ImportCustomers(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Не указан путь к JSON-файлу.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("JSON-файл не найден.", filePath);

            var json = File.ReadAllText(filePath);
            var customers = JsonSerializer.Deserialize<List<CustomerJson>>(json, JsonOptions)
                            ?? new List<CustomerJson>();

            var imported = 0;

            foreach (var item in customers)
            {
                const string sql = @"
IF EXISTS (SELECT 1 FROM Counterparties WHERE ExternalId = @ExternalId)
BEGIN
    UPDATE Counterparties
       SET Name = @Name,
           Inn = NULLIF(@Inn, ''),
           Address = @Address,
           Phone = @Phone,
           CounterpartyType = @Type
     WHERE ExternalId = @ExternalId;
END
ELSE
BEGIN
    INSERT INTO Counterparties(ExternalId, Name, Inn, Address, Phone, CounterpartyType)
    VALUES(@ExternalId, @Name, NULLIF(@Inn, ''), @Address, @Phone, @Type);
END";

                Database.Execute(sql,
                    new SqlParameter("@ExternalId", item.Id ?? string.Empty),
                    new SqlParameter("@Name", item.Name ?? string.Empty),
                    new SqlParameter("@Inn", item.Inn ?? string.Empty),
                    new SqlParameter("@Address", item.Address ?? string.Empty),
                    new SqlParameter("@Phone", item.Phone ?? string.Empty),
                    new SqlParameter("@Type", item.Type ?? string.Empty));

                imported++;
            }

            return imported;
        }
    }
}
