using System.Text.RegularExpressions;

namespace RenameRusToEng
{

    /// <summary>
    /// ������� ������������, ���������������� ��� ������������� � ����� ����������.
    /// </summary>
    class PreparedDictElement
    {
        public string original_rus;
        public string original_eng;
        public Regex Regex_rus;
        public string Regex_eng;
    }
}
