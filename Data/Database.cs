using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;

namespace DemoExam.Data
{
    public static class Database
    {
        private static readonly Lazy<string> ConnectionStringValue =
            new Lazy<string>(LoadConnectionString);

        private static string ConnectionString => ConnectionStringValue.Value;

        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            var table = new DataTable();

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(sql, connection);
            using var adapter = new SqlDataAdapter(command);

            if (parameters is { Length: > 0 })
                command.Parameters.AddRange(parameters);

            connection.Open();
            adapter.Fill(table);

            return table;
        }

        public static int Execute(string sql, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(sql, connection);

            if (parameters is { Length: > 0 })
                command.Parameters.AddRange(parameters);

            connection.Open();
            return command.ExecuteNonQuery();
        }

        public static object Scalar(string sql, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(sql, connection);

            if (parameters is { Length: > 0 })
                command.Parameters.AddRange(parameters);

            connection.Open();
            return command.ExecuteScalar();
        }

        private static string LoadConnectionString()
        {
            var environmentValue = Environment.GetEnvironmentVariable("DEMOEXAM_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(environmentValue))
                return environmentValue;

            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "App.config"),
                Path.Combine(AppContext.BaseDirectory, "DemoExam.dll.config"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "App.config")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "App.config"))
            };

            foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(path))
                    continue;

                var document = XDocument.Load(path);
                var element = document.Root?
                    .Element("connectionStrings")?
                    .Elements("add")
                    .FirstOrDefault(x =>
                        string.Equals(
                            (string)x.Attribute("name"),
                            "ExamDb",
                            StringComparison.OrdinalIgnoreCase));

                var value = (string)element?.Attribute("connectionString");
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            throw new InvalidOperationException(
                "Не найдена строка подключения ExamDb. Проверь App.config рядом с проектом/приложением " +
                "или задай переменную среды DEMOEXAM_CONNECTION_STRING.");
        }
    }
}
