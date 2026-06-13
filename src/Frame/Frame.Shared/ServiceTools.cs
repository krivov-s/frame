using System.Globalization;
using System.Reflection;

namespace Frame.Shared
{
    public static class ServiceTools
    {
        public static DateOnly ConvertToDateOnly(object? oVal)
        {
            DateOnly date = DateOnly.MinValue;
            if (oVal != null && oVal.GetType() != typeof(DBNull))
            {
                string? strDate = oVal.ToString();
                if (string.IsNullOrEmpty(strDate))
                {
                    string err = $"Для конвертации в DateOnly был передан {oVal}, " +
                                 $"преобразовался в строку {strDate}";
                    throw new FrameException(err);
                }

                try
                {
                    if (strDate.Length < 10)
                    {
                        throw new FrameException($"Длина строки ({strDate.Length}) меньше 10 символов");
                    }

                    if (strDate.Length > 10)
                    {
                        strDate = strDate.Substring(0, 10);
                    }
                    
                    var cultureInfo = new CultureInfo("ru-RU");
                    strDate = strDate.Replace("/", ".");
                    DateTime dateTime = DateTime.ParseExact(strDate, "dd.MM.yyyy", cultureInfo);
                    date = DateOnly.FromDateTime(dateTime);
                }
                catch (Exception ex)
                {
                    string err = $"Для конвертации в DateOnly был передан {oVal}, " +
                                 $"преобразовался в строку {strDate}, в ходе конвертации возникла ошибка {ex.Message}";
                    throw new FrameException(err, ex);
                }
            }
            return date;
        }
        public static DateTime ConvertToDT(object? oVal)
        {
            DateTime dt = DateTime.MinValue;
            if (oVal != null && oVal.GetType() != typeof(DBNull))
            {
                dt = Convert.ToDateTime(oVal);
            }
            return dt;
        }
        public static DateTime? ConvertToDT(object? oVal, bool bNoNullValues)
        {
            DateTime? dt = null; 
            if(bNoNullValues)
            {
                dt = DateTime.MinValue;
            }
            if (oVal != null && oVal.GetType() != typeof(DBNull))
            {
                dt = Convert.ToDateTime(oVal);
            }
            return dt;
        }
        public static decimal ConvertToDecimal(object? oVal, decimal valueIfNull = 0)
        {
            if (oVal == null)
                return valueIfNull;
            if(oVal is DBNull)
                return valueIfNull;
                
            string s = oVal.ToString() ?? "";
            s = s.Replace(".", ",").Replace(" ", "");
            return Convert.ToDecimal(s);
        }
        /// <summary>
        /// Преобразование строки в int. 
        /// </summary>
        /// <param name="oVal"></param>
        /// <param name="iValueIfNull"></param>
        /// <returns></returns>
        public static int ConvertToInt(object? oVal, int iValueIfNull = 0)
        {
            int val;
            if (oVal != null && oVal.GetType() != typeof(DBNull))
            {
                string? sVal = oVal.ToString();
                // Избавляемся от всех нецифровых символов
                //sVal = Regex.Replace(sVal, @"[^\d]+", "");
                val = Convert.ToInt32(sVal);
            }
            else 
            {
                val = iValueIfNull;
            }
            return val;
        }
        public static bool ConvertToBool(object oVal)
        {
            bool val = false;
            if (oVal != null && oVal.GetType() != typeof(DBNull))
            {
                val = Convert.ToBoolean(oVal);
            }
            return val;
        }

        /// <summary>
        /// Конвертация переданного значения в требуемый тип T
        /// </summary>
        /// <param name="value">Значение для конвертации</param>
        /// <param name="defaultValue">Значение по-умолчанию при невозможности конвертации</param>
        /// <typeparam name="T">Требуемый тип, в который нужно сконвертировать</typeparam>
        public static T? ConvertValue<T>(object value, T? defaultValue)
        {
            var tTarget = typeof(T);
            var tResult = value.GetType();
            
            if (tTarget == tResult)
            {
                return (T?) value;
            }
            
            try
            {
                if (tTarget == typeof(DateOnly))
                {
                    return (T) (object) ConvertToDateOnly(value);
                }

                if (tTarget == typeof(DateTime))
                {
                    return (T) (object) ConvertToDT(value);
                }

                if (tTarget == typeof(decimal))
                {
                    object? tmp = defaultValue;
                    decimal defValue = (tmp != null) ? (decimal) tmp : 0;
                    return (T) (object) ConvertToDecimal(value, defValue);
                }

                if (tTarget == typeof(int))
                {
                    object? tmp = defaultValue;
                    int defValue = (tmp != null) ? (int) tmp : 0;
                    return (T) (object) ConvertToInt(value, defValue);
                }

                if (tTarget == typeof(bool))
                {
                    return (T) (object) ConvertToBool(value);
                }

                // Прямое изменение типа параметра
                var resultConverted = Convert.ChangeType(value, tTarget);
                return (T)resultConverted;
            }
            catch
            {
                // Сюда попадаем если не сработала конвертация
                return defaultValue;
            }
        }


        public static DateTime GetQuarterDateBegin(DateTime dDate)
        {
            int quarter = Convert.ToInt32(Math.Floor((dDate.Month - 1) / 3.0));
            return new DateTime(dDate.Year, (quarter + 1) * 3 - 2, 1);
        }

        // public static DateTime GetQuarterDateEnd(DateTime dDate)
        // {
        //     int quarter = Convert.ToInt32(Math.Floor((dDate.Month - 1) / 3.0));
        //     return GetQuarterDateBegin(dDate).AddMonths(3).AddDays(-1);
        // }
        //
        public static IEnumerable<string> ReadFilesFromDir(string sDir)
        {
            foreach (string f in Directory.GetFiles(sDir))
            {
                using StreamReader reader = new(f);
                string strMenu = reader.ReadToEnd();
                yield return strMenu;
            }
        }

        public static decimal Round(decimal value, int digits = 2)
        {
            return Math.Round(value, digits);
        }
        
        /// <summary>
        /// Принудительная загрузка сборок, чтобы стала доступна информация обо всех типах.
        /// </summary>
        public static void LoadAssembliesToApplication(string fileMask)
        {
            // Получаем путь к директории приложения
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Загружаем все сборки из директории приложения
            var assemblies = Directory
                .GetFiles(baseDir, fileMask)
                .Select(Assembly.LoadFrom)
                .ToList();

            // Добавляем сборки в контекст приложения
            var appDomain = AppDomain.CurrentDomain;
            foreach (var assembly in assemblies)
            {
                if (!appDomain.GetAssemblies().Contains(assembly))
                {
                    appDomain.Load(assembly.GetName());
                }
            }
        }

        /// <summary>
        /// Возвращает список загруженных в приложение сборок, в имени которых содержится <paramref name="nameContainsThis"/>
        /// </summary>
        /// <param name="nameContainsThis"></param>
        /// <returns></returns>
        public static List<Assembly> GetAssemblies(string nameContainsThis)
        {
            return  AppDomain.CurrentDomain.GetAssemblies().Where(
                a => a.GetName().FullName.Contains(nameContainsThis)).ToList();
        }

        public static string GetDefaultFormat<TValue>() => GetDefaultFormat(typeof(TValue));

        public static string GetDefaultFormat(Type t)
        {
            if (t == typeof(int))
            {
                return "N0";
            }
        
            if (t == typeof(bool))
            {
                return "B";
            }

            if (t == typeof(decimal))
            {
                return "N2";
            }

            if (t == typeof(DateTime))
            {
                return "dd.MM.yyyy HH:mm:ss";
            }

            if (t == typeof(DateOnly))
            {
                return "dd.MM.yyyy";
            }

            return "";
        }
        
    }

    public static class Extensions
    {
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
            {
                action(element);
            }
            return source;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable != null)
            {
                return !enumerable.Any();
            }

            return true;
        }
        
        public static bool IsNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }        
    };

}
