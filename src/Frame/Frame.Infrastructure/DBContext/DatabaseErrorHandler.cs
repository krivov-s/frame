using Frame.Shared;
using Microsoft.EntityFrameworkCore;

namespace Frame.Infrastructure.DBContext;

public class DatabaseErrorHandler
{
    public static string GetUserFriendlyErrorMessage(Exception ex)
    {
        if (ex is DbUpdateConcurrencyException concurrencyException)
        {
            return "Ошибка совместного доступа к данным: кто-то уже изменил текущую запись. " +
                   "Необходимо перезачитать данные и повторить изменение и запись.";
        }
        
        if (ex is DbUpdateException dbUpdateEx)
        {
            if (dbUpdateEx.InnerException is Npgsql.PostgresException pgEx)
            {
                // PostgreSQL error codes: https://www.postgresql.org/docs/current/errcodes-appendix.html
                return pgEx.SqlState switch
                {
                    // Class 23 — Integrity Constraint Violation
                    "23503" => "Невозможно удалить запись, так как она используется в других разделах", // foreign_key_violation
                    "23505" => "Запись с такими данными уже существует",  // unique_violation
                    "23502" => "Отсутствуют обязательные данные", // not_null_violation
                    "23514" => "Нарушение правил проверки данных", // check_violation
                    
                    // Class 22 — Data Exception
                    "22003" => "Значение выходит за допустимые пределы", // numeric_value_out_of_range
                    "22001" => "Строка слишком длинная", // string_data_right_truncation
                    
                    // Class 42 — Syntax Error or Access Rule Violation
                    "42P01" => "Таблица не существует", // undefined_table
                    "42703" => "Колонка не существует", // undefined_column
                    
                    _ => $"Ошибка при работе с базой данных (код: {pgEx.SqlState})"
                };
            }
        }
        
        if (ex is FrameSecurityException frameEx)
        {
            return frameEx.Message;
        }

        return "Ошибка при работе с БД";
    }
}