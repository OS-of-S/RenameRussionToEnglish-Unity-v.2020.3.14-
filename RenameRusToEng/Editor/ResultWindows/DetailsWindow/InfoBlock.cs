using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace RenameRusToEng
{
    /// <summary>
    /// Элемент UI, отображающий информацию об отдельном шаге алгоритма в окне подробностей.
    /// </summary>
    class InfoBlock : VisualElement
    {
        const int gap = DetailsWindow.gap;
        static Color outline_color = DetailsWindow.outline_color;

        NumberLine StepLine;
        SomeTextBox FoundMatch;
        SomeTextBox CorrespondedSubstitution;
        SomeTextBox original_rus;
        SomeTextBox original_eng;
        SomeTextBox FoundMatch_extended;
        SomeTextBox CorrespondedSubstitution_extended;
        NumberLine _lineNumber;
        NumberLine _columnNumber;
        int line;
        int column;

        public InfoBlock(LogResults exemple_info, objectLogsRepresentation parent_block)
        {
            VisualElement vis_el = new VisualElement() { style = { borderTopWidth = gap, borderBottomWidth = gap, borderRightWidth = gap, borderLeftWidth = gap } };

            Box obvodka = new Box()
            {
                style =
                {
                    //backgroundColor = new Color(42, 42, 42, 1),
                    borderTopColor = outline_color,
                    borderBottomColor = outline_color,
                    borderLeftColor = outline_color,
                    borderRightColor = outline_color,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                }
            };

            StepLine = new NumberLine("Шаг", 0);
            obvodka.Add(StepLine);
            FoundMatch = new SomeTextBox("Вхождение", exemple_info.FoundMatch);
            obvodka.Add(FoundMatch);
            if (exemple_info.OriginalMatchElement != null)
            {
                CorrespondedSubstitution = new SomeTextBox("Замена", exemple_info.CorrespondedSubstitution);
                obvodka.Add(CorrespondedSubstitution);
                original_rus = new SomeTextBox("Оригинальный шаблон вхождения", exemple_info.OriginalMatchElement.original_rus);
                obvodka.Add(original_rus);
                original_eng = new SomeTextBox("Оригинальный шаблон замены", exemple_info.OriginalMatchElement.original_eng);
                obvodka.Add(original_eng);
            }
            FoundMatch_extended = new SomeTextBox("Контекст вхождения", exemple_info.FoundMatch_extended);
            obvodka.Add(FoundMatch_extended);
            if (exemple_info.OriginalMatchElement != null)
            {
                CorrespondedSubstitution_extended = new SomeTextBox("Контекст замены", exemple_info.CorrespondedSubstitution_extended);
                obvodka.Add(CorrespondedSubstitution_extended);
            }
            if (exemple_info.lineNumber > -1)
            {
                line = exemple_info.lineNumber + 1;
                _lineNumber = new NumberLine("Строка", line);
                obvodka.Add(_lineNumber);
            }
            else line = 1;
            column = exemple_info.columnNumber + 1;
            _columnNumber = new NumberLine("Символ", column);
            obvodka.Add(_columnNumber);
            vis_el.Add(obvodka);
            Add(vis_el);

            Clickable FocusOnSubstitution = new Clickable(() =>
            {
                if (parent_block.info.type == ObjectSubstitutionInfo.ObjectType.TEXT)
                {
                    UnityEngine.Object ass_to_focus = parent_block.GetAssetToFocus();
                    if (!AssetDatabase.OpenAsset(ass_to_focus, line, column)) Debug.LogError("Не удалось открыть файл " + ass_to_focus);
                }
                else parent_block.FocusOnObject();
            });
            FocusOnSubstitution.activators.Clear();
            FocusOnSubstitution.activators.Add(new ManipulatorActivationFilter { clickCount = 2, button = MouseButton.LeftMouse });
            this.AddManipulator(FocusOnSubstitution);
        }

        public void SetInfo(int step, LogResults info)
        {
            StepLine.num = step;
            FoundMatch.text = info.FoundMatch;

            if (info.OriginalMatchElement != null)
            {
                CorrespondedSubstitution.text = info.CorrespondedSubstitution;
                original_rus.text = info.OriginalMatchElement.original_rus;
                original_eng.text = info.OriginalMatchElement.original_eng;
            }

            FoundMatch_extended.text = info.FoundMatch_extended;

            if (info.OriginalMatchElement != null)
            {
                CorrespondedSubstitution_extended.text = info.CorrespondedSubstitution_extended;
            }

            if (info.lineNumber > -1)
            {
                line = info.lineNumber + 1;
                _lineNumber.num = line;
            }
            else line = 1;

            column = info.columnNumber + 1;
            _columnNumber.num = column;

        }
    }
}