using System.Globalization;

namespace EmilsWork.EmilsCMS
{
    internal class Helpers
    {
        public static bool TryAskUsers<T>(string Question, out T response, bool UseChar = false, bool Erase = false, bool inline = true)
        {
            try
            {
                response = AskUsers<T>(Question, UseChar, Erase, inline);
                return true;
            }
            catch
            {
                response = default;
                return false;
            }
        }

        public static T AskUsers<T>(string Question, bool UseChar = false, bool Erase = false, bool inline = true)
        {
            if (inline)
                Console.Write(Question);
            else
                Console.WriteLine(Question);

            string? text = UseChar ? Console.ReadKey(Erase).KeyChar.ToString() : Console.ReadLine();

            if (text == null)
                return default;

            try
            {
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (string.IsNullOrWhiteSpace(text))
                {
                    if (Nullable.GetUnderlyingType(typeof(T)) != null)
                        return default;

                    throw new InvalidCastException("Valeur requise.");
                }

                if (targetType == typeof(DateTime))
                {
                    if (DateTime.TryParseExact(text, ["yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                        || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt)
                        || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        return (T)(object)dt;
                    }

                    throw new InvalidCastException("Format de date invalide. Utilisez yyyy-MM-dd.");
                }

                if (targetType == typeof(float) || targetType == typeof(double) || targetType == typeof(decimal))
                {
                    text = text.Replace('.', ',');
                }

                var converted = Convert.ChangeType(text, targetType, CultureInfo.CurrentCulture);
                return (T)converted;
            }
            catch (Exception e)
            {
                // Wrap and preserve original exception (keeps stack trace in InnerException)
                throw new InvalidCastException("Unsupported return type or invalid input in AskUsers.", e);
            }
        }

    }
}
