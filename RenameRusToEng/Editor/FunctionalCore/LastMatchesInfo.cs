using System.Text.RegularExpressions;

namespace RenameRusToEng
{

    /// <summary>
    /// Вспомогательный класс для составления отчётных логов о заменах.
    /// </summary>
    class LastMatchesInfo
    {
        public PreparedDictElement dict_element;
        public Match match;
        public string replacement;

        public LastMatchesInfo(PreparedDictElement d_e, Match m, string r)
        {
            dict_element = d_e;
            match = m;
            replacement = r;
        }
    }
}