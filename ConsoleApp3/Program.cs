using System;
using System.Data;
using Npgsql;

class Program
{
    static void Main()
    {
        string connectionString = "Host=localhost;Port=5432;Database=university;Username=postgres;Password=postgres";

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();

            var dataTable = new DataTable("students");
            var adapter = new NpgsqlDataAdapter();

            adapter.SelectCommand = new NpgsqlCommand("SELECT id, name, age FROM students", connection);
            adapter.Fill(dataTable);

            Console.WriteLine("=== Исходные данные ===");
            DisplayTable(dataTable);

            dataTable.Rows[0]["age"] = 25;
            dataTable.Rows.Add(null, "Иванов Иван", 20);
            dataTable.Rows[2].Delete();

            Console.WriteLine("\n=== RowState перед сохранением ===");
            foreach (DataRow row in dataTable.Rows)
            {
                Console.WriteLine($"{row["name"]}: {row.RowState}");
            }

            var insertCommand = new NpgsqlCommand(
                "INSERT INTO students (name, age) VALUES (@name, @age)", connection);
            insertCommand.Parameters.Add("@name", NpgsqlTypes.NpgsqlDbType.Text, 100, "name");
            insertCommand.Parameters.Add("@age", NpgsqlTypes.NpgsqlDbType.Integer, 0, "age");
            adapter.InsertCommand = insertCommand;

            var updateCommand = new NpgsqlCommand(
                "UPDATE students SET name = @name, age = @age WHERE id = @id", connection);
            updateCommand.Parameters.Add("@name", NpgsqlTypes.NpgsqlDbType.Text, 100, "name");
            updateCommand.Parameters.Add("@age", NpgsqlTypes.NpgsqlDbType.Integer, 0, "age");
            updateCommand.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer, 0, "id");
            updateCommand.Parameters["@id"].SourceVersion = DataRowVersion.Original;
            adapter.UpdateCommand = updateCommand;

            var deleteCommand = new NpgsqlCommand(
                "DELETE FROM students WHERE id = @id", connection);
            deleteCommand.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer, 0, "id");
            deleteCommand.Parameters["@id"].SourceVersion = DataRowVersion.Original;
            adapter.DeleteCommand = deleteCommand;

            try
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        insertCommand.Transaction = transaction;
                        updateCommand.Transaction = transaction;
                        deleteCommand.Transaction = transaction;

                        adapter.Update(dataTable);
                        transaction.Commit();
                        Console.WriteLine("\n=== Изменения успешно сохранены ===");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"\n=== Ошибка сохранения: {ex.Message} ===");
                        Console.WriteLine("Транзакция откатана.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== Ошибка: {ex.Message} ===");
                Console.WriteLine("\nИзменения остались в памяти:");
                foreach (DataRow row in dataTable.Rows)
                {
                    if (row.RowState != DataRowState.Unchanged)
                    {
                        Console.WriteLine($"{row["name"]}: {row.RowState}");
                    }
                }
            }

            Console.WriteLine("\n=== RowState после сохранения ===");
            foreach (DataRow row in dataTable.Rows)
            {
                Console.WriteLine($"{row["name"]}: {row.RowState}");
            }

            var checkCommand = new NpgsqlCommand("SELECT id, name, age FROM students ORDER BY id", connection);
            using (var reader = checkCommand.ExecuteReader())
            {
                Console.WriteLine("\n=== Проверка из БД ===");
                while (reader.Read())
                {
                    Console.WriteLine($"ID: {reader["id"]}, Имя: {reader["name"]}, Возраст: {reader["age"]}");
                }
            }
        }
    }

    static void DisplayTable(DataTable table)
    {
        foreach (DataRow row in table.Rows)
        {
            Console.WriteLine($"ID: {row["id"]}, Имя: {row["name"]}, Возраст: {row["age"]}");
        }
    }
}