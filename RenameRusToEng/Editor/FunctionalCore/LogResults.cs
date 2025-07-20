using System.Text.RegularExpressions;

namespace RenameRusToEng
{

    /// <summary>
    /// Класс для сохранения информации о проделанных над текстом действиях.
    /// </summary>
    class LogResults
    {

        public PreparedDictElement OriginalMatchElement; // Ссылка на оригинальный элемент, который соответствует замене.
        public string FoundMatch;
        public string CorrespondedSubstitution;
        public string FoundMatch_extended; // Если подпоследовательность найдена в середине текста и нужен дополнительный контекст слева и справа от неё.
        public string CorrespondedSubstitution_extended;
        public int lineNumber = -1; // В случае замены эти координаты соответствуют началу заменённой строки (могут сбиваться, т.к. изменения вносятся поэтапно). Если же алгоритм не меняет файлы, то эти значения указывают на местоположение оригинального совпадения.
        public int columnNumber = -1;

        public LogResults() { }

        public LogResults(PreparedDictElement dict_element, Match match, string input, string output, string replacement, int changed_char_index, int line, int extended_symbols)
        {
            OriginalMatchElement = dict_element;
            FoundMatch = match.ToString();
            CorrespondedSubstitution = replacement;
            int extended_start;
            int extended_length;
            string addition_left = "...";
            string addition_right = "...";

            extended_start = match.Index - extended_symbols;
            extended_length = FoundMatch.Length + extended_symbols * 2;
            if (extended_start < 0)
            {
                addition_left = "";
                extended_length += extended_start;
                extended_start = 0;
            }
            int input_length = input.Length;
            if (extended_start + extended_length > input_length)
            {
                addition_right = "";
                extended_length = input_length - extended_start;
            }

            FoundMatch_extended = addition_left + input.Substring(extended_start, extended_length) + addition_right;

            extended_start = changed_char_index - extended_symbols;
            extended_length = FoundMatch.Length + extended_symbols * 2;
            if (extended_start < 0)
            {
                extended_length += extended_start;
                extended_start = 0;
            }
            int output_length = output.Length;
            if (extended_start + extended_length > output_length)
            {
                extended_length = output_length - extended_start;
            }
            CorrespondedSubstitution_extended = addition_left + output.Substring(extended_start, extended_length) + addition_right;

            columnNumber = line;
        }
    }
}