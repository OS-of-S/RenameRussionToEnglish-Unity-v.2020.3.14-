using UnityEngine.UIElements;

namespace RenameRusToEng
{

    /// <summary>
    /// Элемент UI, отображающий числовое значение и наименование.
    /// </summary>
    class NumberLine : TextElement
    {
        const int gap = DetailsWindow.gap;
        private string _fieldName;
        public int num
        {
            set
            {
                text = _fieldName + ": " + value.ToString();

            }
        }

        public NumberLine(string FieldName, int n)
        {
            _fieldName = FieldName;
            num = n;

            style.borderTopWidth = gap;
            style.borderBottomWidth = gap;
            style.borderRightWidth = gap;
            style.borderLeftWidth = gap;
        }
    }
}